using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public class HistorialDbContext: DbContext
{
    public HistorialDbContext(DbContextOptions<HistorialDbContext> options): base(options) { }
    
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Parent> Parents { get; set; }
    public DbSet<MedicalHistory> PatientHistories { get; set; }
}