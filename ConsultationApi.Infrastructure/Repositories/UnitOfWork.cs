using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Interfaces;
using ConsultationApi.Infrastructure.Data;

namespace ConsultationApi.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new EfRepository<User>(context);
        Doctors = new EfRepository<Doctor>(context);
        Patients = new EfRepository<Patient>(context);
        Appointments = new EfRepository<Appointment>(context);
        ConsultationSessions = new EfRepository<ConsultationSession>(context);
        ChatMessages = new EfRepository<ChatMessage>(context);
        Prescriptions = new EfRepository<Prescription>(context);
        MedicationItems = new EfRepository<MedicationItem>(context);
        Reviews = new EfRepository<Review>(context);
        Notifications = new EfRepository<Notification>(context);
        RefreshTokens = new EfRepository<RefreshToken>(context);
    }

    public IRepository<User> Users { get; }
    public IRepository<Doctor> Doctors { get; }
    public IRepository<Patient> Patients { get; }
    public IRepository<Appointment> Appointments { get; }
    public IRepository<ConsultationSession> ConsultationSessions { get; }
    public IRepository<ChatMessage> ChatMessages { get; }
    public IRepository<Prescription> Prescriptions { get; }
    public IRepository<MedicationItem> MedicationItems { get; }
    public IRepository<Review> Reviews { get; }
    public IRepository<Notification> Notifications { get; }
    public IRepository<RefreshToken> RefreshTokens { get; }

    public Task<int> CommitAsync() => _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
