using System.Security.Cryptography;
using Core.Entities;
using Presentation.Domain;

namespace Presentation.Services;

public interface IPatientService
{
    public Task<PaginatedList<Patient>> GetPatients(int pageNumber, int pageSize, int maxPages);
    public Task<Patient> GetPatient(int id);
    public void DeletePatient(int id);
    public Task<Patient> CreatePatient(Patient patient);
    public Task<Patient> UpdatePatient(Patient patient);
}