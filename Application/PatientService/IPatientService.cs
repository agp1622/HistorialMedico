using System.Security.Cryptography;
using Core.Entities;
using Microsoft.AspNetCore.Http;
using Presentation.Domain;

namespace Presentation.Services;

public interface IPatientService
{
    public Task<PaginatedList<Patient>> GetPatients(int pageNumber, int pageSize, int maxPages);
    public Task<Patient> GetPatient(Guid id);
    public Task<bool> DeletePatient(Guid id);
    public Task<Patient> CreatePatient(Patient patient);
    public Task<Patient> UpdatePatient(Patient patient, Guid id);
    public Task<MedicalHistory> AddMedicalHistory(MedicalHistory medicalHistory, Guid id);
    
    Task<Attachment> AddAttachmentAsync(Guid patientId, IFormFile file, string uploadsPath);
    Task<IEnumerable<Attachment>> GetPatientAttachmentsAsync(Guid patientId);
    Task<Attachment?> GetAttachmentAsync(Guid patientId, Guid attachmentId);
    Task<bool> DeleteAttachmentAsync(Guid patientId, Guid attachmentId);
    Task<(byte[] fileData, string contentType, string fileName)?> GetAttachmentFileAsync(Guid patientId, Guid attachmentId);

}