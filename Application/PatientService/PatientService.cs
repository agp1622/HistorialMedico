using Core.Entities;
using Infrastructure.Context;
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

    public async Task<Patient> GetPatient(int id)
    {
        var patient = await this._context.Patients.FindAsync(id);

        if (patient is null)
        {
            throw new KeyNotFoundException("No patient found with id: " + id);
        }
        
        return patient;
    }

    public void DeletePatient(int id)
    {
        var patient = this._context.Patients.Find(id);

        if (patient is null)
        {
            throw new KeyNotFoundException("No patient found with id: " + id);
        }
        
        this._context.Patients.Remove(patient);
        this._context.SaveChanges();
    }

    public async Task<Patient> CreatePatient(Patient patient)
    {
        patient.NumExpediente = await GenerateUniqueNumExpedienteAsync();
        
        await this._context.Patients.AddAsync(patient);
        await this._context.SaveChangesAsync();
        
        return patient;
    }

    public async Task<Patient> UpdatePatient(Patient patient)
    {
        var existingPatient = await this._context.Patients.FindAsync(patient.Id);

        if (existingPatient is null)
        {
            throw new KeyNotFoundException("No patient found with id: " + patient.Id);
        }
        
        this._context.Entry(existingPatient).CurrentValues.SetValues(patient);
        
        await this._context.SaveChangesAsync();
        
        return existingPatient;
    }
    
    private async Task<string> GenerateUniqueNumExpedienteAsync()
    {
        int currentYear = DateTime.Now.Year;

        var counter = await this._context.ExpedienteCounters
            .FirstOrDefaultAsync(ec => ec.Year == currentYear);

        if (counter == null)
        {
            counter = new ExpedienteCounter
            {
                Year = currentYear,
                Counter = 1
            };
            this._context.ExpedienteCounters.Add(counter);
        }
        else
        {
            counter.Counter++;
            this._context.ExpedienteCounters.Update(counter);
        }

        string newNum = $"{currentYear}-{counter.Counter}";

        bool exists = await this._context.Patients
            .AnyAsync(p => p.NumExpediente == newNum);

        if (exists)
        {
            // Retry logic for uniqueness
            for (int i = 0; i < 5; i++)
            {
                counter.Counter++;
                newNum = $"{currentYear}-{counter.Counter}";

                exists = await this._context.Patients.AnyAsync(p => p.NumExpediente == newNum);
                if (!exists)
                {
                    break;
                }
            }

            if (exists)
            {
                throw new Exception("No se pudo generar un número de expediente único.");
            }
        }

        await this._context.SaveChangesAsync();
        return newNum;
    }

    
}