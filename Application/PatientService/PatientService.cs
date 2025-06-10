using Core.Entities;
using Infrastructure.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Presentation.Services;

namespace Application.PatientService;

public class PatientService: IPatientService
{
    private readonly HistorialDbContext _context;

    public PatientService(HistorialDbContext context)
    {
        this._context = context;
    }

    public async Task<PaginatedList<Patient>> GetPatients(int pageNumber, int pageSize, int maxPages)
    {
        var query = this._context.Patients.AsQueryable();
        
        var patients = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalRecords = await this._context.Patients.CountAsync();
        
        var totalPages = (int)Math.Ceiling((double)totalRecords / (double)pageSize);
        var pagesToDisplay = totalPages > maxPages ? maxPages : totalPages;

        return new PaginatedList<Patient>
        {
            Items = patients,
            TotalPages = pagesToDisplay,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            CurrentPage = pageNumber
        };
    }

    public async Task<Patient> GetPatient(Guid id)
    {
        var patient = await this._context.Patients
            .Include(p => p.Historial)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient is null)
        {
            throw new KeyNotFoundException("No patient found with id: " + id);
        }
        
        return patient;
    }

    public async Task<bool> DeletePatient(Guid id)
    {
        var patient = await _context.Patients.FindAsync(id);

        if (patient is null)
        {
            throw new KeyNotFoundException("No patient found with id: " + id);
        }
        
        this._context.Patients.Remove(patient);
        await this._context.SaveChangesAsync();
        
        return true;
    }

    public async Task<Patient> CreatePatient(Patient patient)
    {
        patient.NumeroExpediente = await GenerateUniqueNumExpedienteAsync();
        
        await this._context.Patients.AddAsync(patient);
        await this._context.SaveChangesAsync();
        
        return patient;
    }

    public async Task<Patient> UpdatePatient(Patient patient, Guid id)
    {
        var existingPatient = await this._context.Patients.FindAsync(id);

        if (existingPatient is null)
        {
            throw new KeyNotFoundException("No patient found with id: " + patient.Id);
        }
        
        this._context.Entry(existingPatient).CurrentValues.SetValues(patient);
        
        await this._context.SaveChangesAsync();
        
        return existingPatient;
    }

    public async Task<MedicalHistory> AddMedicalHistory(MedicalHistory medicalHistory, Guid id)
    {
        medicalHistory.PatientId = id;
        medicalHistory.UpdatedAt = DateTime.Now;
        medicalHistory.Fecha = DateTime.Now;
        medicalHistory.CreatedAt = DateTime.Now;
        
        this._context.MedicalHistories.Add(medicalHistory);
        
        await this._context.SaveChangesAsync();
        
        return medicalHistory;
    }

    public async Task<Attachment> AddAttachmentAsync(Guid patientId, IFormFile file, string uploadsPath)
    {
        try
        {
            // Validate patient exists
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                throw new KeyNotFoundException($"Patient with ID {patientId} not found");
            }

            // Validate file
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file provided or file is empty");
            }

            // Check file size (50MB limit)
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (file.Length > maxFileSize)
            {
                throw new ArgumentException("File size exceeds 50MB limit");
            }

            // Validate file types
            var allowedTypes = new[] { 
                "application/pdf", 
                "image/jpeg", 
                "image/png", 
                "image/gif",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "text/plain"
            };

            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                throw new ArgumentException($"File type '{file.ContentType}' is not allowed");
            }

            // Create patient-specific directory
            var patientUploadsPath = Path.Combine(uploadsPath, "patients", patientId.ToString());
            Directory.CreateDirectory(patientUploadsPath);

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var fullPath = Path.Combine(patientUploadsPath, uniqueFileName);

            // Save file to disk
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create attachment record
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                Name = file.FileName,
                Path = fullPath,
                UploadDate = DateTime.UtcNow,
                Size = FormatFileSize(file.Length),
                PatientId = patientId
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();


            return attachment;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<Attachment>> GetPatientAttachmentsAsync(Guid patientId)
    {
        try
        {
            // Validate patient exists
            var patient = await _context.Patients
                .Include(p => p.Attachments)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                throw new KeyNotFoundException($"Patient with ID {patientId} not found");
            }

            return patient.Attachments.OrderByDescending(a => a.UploadDate);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<Attachment?> GetAttachmentAsync(Guid patientId, Guid attachmentId)
    {
        try
        {
            return await _context.Attachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.PatientId == patientId);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> DeleteAttachmentAsync(Guid patientId, Guid attachmentId)
    {
        try
        {
            var attachment = await _context.Attachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.PatientId == patientId);

            if (attachment == null)
            {
                return false;
            }

            // Delete file from disk
            if (File.Exists(attachment.Path))
            {
                File.Delete(attachment.Path);
            }

            // Delete from database
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();


            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<(byte[] fileData, string contentType, string fileName)?> GetAttachmentFileAsync(Guid patientId, Guid attachmentId)
    {
        try
        {
            var attachment = await GetAttachmentAsync(patientId, attachmentId);

            if (attachment == null || !File.Exists(attachment.Path))
            {
                return null;
            }

            var fileData = await File.ReadAllBytesAsync(attachment.Path);
            var contentType = GetContentType(attachment.Path);

            return (fileData, contentType, attachment.Name);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    // Helper methods
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
    
private async Task<string> GenerateUniqueNumExpedienteAsync()
{
    int currentYear = DateTime.Now.Year;
    const int maxRetries = 10;
    
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            // Use a transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Lock the counter row for update to prevent race conditions
                var counter = await _context.ExpedienteCounters
                    .Where(ec => ec.Year == currentYear)
                    .FirstOrDefaultAsync();

                if (counter == null)
                {
                    // Create new counter for this year
                    counter = new ExpedienteCounter
                    {
                        Year = currentYear,
                        Counter = 0 // Start at 0, will be incremented to 1
                    };
                    _context.ExpedienteCounters.Add(counter);
                    await _context.SaveChangesAsync(); // Save to get the ID
                }

                // Increment counter
                counter.Counter++;
                _context.ExpedienteCounters.Update(counter);

                // Generate the expediente number
                string newNumExpediente = $"{currentYear}-{counter.Counter}";

                // Double-check uniqueness (should not be necessary with proper counter, but safety check)
                bool exists = await _context.Patients
                    .AnyAsync(p => p.NumeroExpediente == newNumExpediente);

                if (exists)
                {
                    // This should rarely happen with proper counter management
                    // But if it does, we need to find the next available number
                    var maxExistingNumber = await _context.Patients
                        .Where(p => p.NumeroExpediente.StartsWith($"{currentYear}-"))
                        .Select(p => p.NumeroExpediente)
                        .ToListAsync();

                    int maxCounter = 0;
                    foreach (var expediente in maxExistingNumber)
                    {
                        if (expediente.Split('-').Length == 2 && 
                            int.TryParse(expediente.Split('-')[1], out int number))
                        {
                            maxCounter = Math.Max(maxCounter, number);
                        }
                    }

                    counter.Counter = maxCounter + 1;
                    newNumExpediente = $"{currentYear}-{counter.Counter}";
                }

                // Save the counter change
                await _context.SaveChangesAsync();
                
                // Commit the transaction
                await transaction.CommitAsync();
                
                return newNumExpediente;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another thread updated the counter, retry
            if (attempt == maxRetries - 1)
                throw new Exception("No se pudo generar un número de expediente único después de varios intentos.");
            
            // Wait a small random time before retrying
            await Task.Delay(Random.Shared.Next(10, 50));
        }
    }
    
    throw new Exception("No se pudo generar un número de expediente único."); 
}
}