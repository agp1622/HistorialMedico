using System.Security.Cryptography;
using HistorialMedico.Domain;

namespace HistorialMedico.Services;

public interface IPatientService
{
    public Task<PaginatedList<Patient>> GetPatients(int pageNumber, int pageSize, int maxPages);
    public Task<Patient> GetPatient(int id);
    public void DeletePatient(int id);
    public Task<Patient> CreatePatient(Patient patient);
    public Task<Patient> UpdatePatient(Patient patient);
}