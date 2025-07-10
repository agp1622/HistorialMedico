using System.Text.RegularExpressions;
using Core;
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

    public async Task<PaginatedList<Patient>> GetPatients(
        int pageNumber, int pageSize, int maxPages,
        string? search = null, string? orderBy = null, string? order = "asc")
    {
        var query = this._context.Patients.AsQueryable();

        // Filtering
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(search) ||
                p.NumeroExpediente.ToLower().Contains(search) ||
                p.Diagnostico.ToLower().Contains(search));
        }

        // Sorting
        if (!string.IsNullOrWhiteSpace(orderBy))
        {
            var property = typeof(Patient).GetProperty(orderBy);
            if (property != null)
            {
                query = order == "desc"
                    ? query.OrderByDescending(e => EF.Property<object>(e, orderBy))
                    : query.OrderBy(e => EF.Property<object>(e, orderBy));
            }
        }

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        var pagesToDisplay = totalPages > maxPages ? maxPages : totalPages;

        var patients = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<Patient>
        {
            Items = patients,
            TotalRecords = totalRecords,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = pagesToDisplay
        };
    }


    public async Task<Patient> GetPatient(Guid id)
    {
        var patient = await this._context.Patients
            .Include(p => p.Historial)
            .Include(x => x.Attachments)
            .Include(c => c.AdditionalPhones)
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

    public async Task<Patient> CreatePatient(PatientDto patient)
    {
        patient.NumeroExpediente = await GenerateUniqueNumExpedienteAsync();
        patient.Id = Guid.NewGuid();

        var savedPatient = new Patient()
        {
            Id = patient.Id,
            NumeroExpediente = patient.NumeroExpediente,
            Nombre = patient.Nombre,
            Sexo = patient.Sexo,
            Edad = patient.Edad,
            Diagnostico = patient.Diagnostico,
            ReferidoPor = patient.ReferidoPor,
            FechaNacimiento = patient.FechaNacimiento,
            FechaConsulta = patient.FechaConsulta,
            SeguroMedico = patient.SeguroMedico,
            Alergias = patient.Alergias,
            Madre = patient.Madre,
            MadreTelefono = patient.MadreTelefono,
            Padre = patient.Padre,
            PadreTelefono = patient.PadreTelefono,
            PadreCorreo = patient.PadreCorreo,
            Gestacion = patient.Gestacion,
            Parto = patient.Parto,
            PesoAlNacer = patient.PesoAlNacer,
            PesoUnidad = patient.PesoUnidad,
            Historial = patient.Historial,
            Attachments = patient.Attachments,
            AdditionalPhones = patient.AdditionalPhones
        };
        
        await this._context.Patients.AddAsync(savedPatient);
        await this._context.SaveChangesAsync();
        
        return savedPatient;
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
        var patient = await this._context.Patients.FindAsync(patientId);
        if (patient == null)
        {
            throw new KeyNotFoundException($"Patient with ID {patientId} not found");
        }

        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file provided or file is empty");
        }

        const long maxFileSize = 50 * 1024 * 1024; // 50MB
        if (file.Length > maxFileSize)
        {
            throw new ArgumentException("File size exceeds 50MB limit");
        }

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

        var patientUploadsPath = Path.Combine(uploadsPath, "patients", patientId.ToString());
        Directory.CreateDirectory(patientUploadsPath);

        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var fullPath = Path.Combine(patientUploadsPath, uniqueFileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            Name = file.FileName,
            Path = fullPath,
            UploadDate = DateTime.UtcNow,
            Size = FormatFileSize(file.Length),
            PatientId = patientId
        };

        this._context.Attachments.Add(attachment);
        await this._context.SaveChangesAsync();


        return attachment;
    }

    public async Task<IEnumerable<Attachment>> GetPatientAttachmentsAsync(Guid patientId)
    {
        var patient = await this._context.Patients
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null)
        {
            throw new KeyNotFoundException($"Patient with ID {patientId} not found");
        }

        return patient.Attachments.OrderByDescending(a => a.UploadDate);
    }

    public async Task<Attachment?> GetAttachmentAsync(Guid patientId, Guid attachmentId)
    {
        return await this._context.Attachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.PatientId == patientId);
    }

    public async Task<bool> DeleteAttachmentAsync(Guid patientId, Guid attachmentId)
    {
        var attachment = await this._context.Attachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.PatientId == patientId);

        if (attachment == null)
        {
            return false;
        }

        if (File.Exists(attachment.Path))
        {
            File.Delete(attachment.Path);
        }

        this._context.Attachments.Remove(attachment);
        await this._context.SaveChangesAsync();
        
        return true;
    }

    public async Task<(byte[] fileData, string contentType, string fileName)?> GetAttachmentFileAsync(Guid patientId, Guid attachmentId)
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

    public async Task<AdditionalPhone> AddAdditionalPhone(AdditionalPhone additionalPhone, Guid patientId)
    {
        // First, validate that the patient exists
        var patientExists = await this._context.Patients
            .AnyAsync(p => p.Id == patientId);
    
      
        additionalPhone.Number = CleanPhoneNumber(additionalPhone.Number);
    
        var existingPhone = await this._context.AdditionalPhones
            .FirstOrDefaultAsync(x => x.Number == additionalPhone.Number && x.PatientId == patientId);
        
        additionalPhone.PatientId = patientId;
        await this._context.AdditionalPhones.AddAsync(additionalPhone);
        await this._context.SaveChangesAsync();
    
        return additionalPhone;
    }

    
    
    public static string CleanPhoneNumber(string phoneNumber, bool keepPlusSign = false)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return string.Empty;

        return Regex.Replace(phoneNumber, keepPlusSign ? @"[^\d+]" : @"[^\d]", "");
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
            await using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var counter = await _context.ExpedienteCounters
                    .Where(ec => ec.Year == currentYear)
                    .FirstOrDefaultAsync();

                if (counter == null)
                {
                    counter = new ExpedienteCounter
                    {
                        Year = currentYear,
                        Counter = 0
                    };
                    _context.ExpedienteCounters.Add(counter);
                    await _context.SaveChangesAsync();
                }

                counter.Counter++;
                _context.ExpedienteCounters.Update(counter);

                string newNumExpediente = $"{currentYear}-{counter.Counter}";

                bool exists = await _context.Patients
                    .AnyAsync(p => p.NumeroExpediente == newNumExpediente);

                if (exists)
                {
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

                await _context.SaveChangesAsync();
                
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
            if (attempt == maxRetries - 1)
                throw new Exception("No se pudo generar un número de expediente único después de varios intentos.");
            
            await Task.Delay(Random.Shared.Next(10, 50));
        }
    }
    
    throw new Exception("No se pudo generar un número de expediente único."); 
}
}