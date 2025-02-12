using HistorialMedico.Data;
using HistorialMedico.Services;
using Microsoft.EntityFrameworkCore;

namespace HistorialMedico.Domain.Services;

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
            .Skip((pageNumber))
            .Take(pageSize)
            .ToListAsync();

        var totalRecords = await _context.Patients.CountAsync();
        
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
        var patient = await _context.Patients.FindAsync(id);

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
        await this._context.Patients.AddAsync(patient);
        await this._context.SaveChangesAsync();
        
        return patient;
    }

    public async Task<Patient> UpdatePatient(Patient patient)
    {
        throw new NotImplementedException();
    }
}