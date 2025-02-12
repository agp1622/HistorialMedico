using System.Reflection.Metadata;
using HistorialMedico.Domain;
using Microsoft.EntityFrameworkCore;

namespace HistorialMedico.Data;

public class HistorialDbContext: DbContext
{
    public HistorialDbContext(DbContextOptions<HistorialDbContext> options): base(options) { }
    
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Parent> Parents { get; set; }
    public DbSet<MedicalHistory> PatientHistories { get; set; }
    public DbSet<User> Users { get; set; }
}