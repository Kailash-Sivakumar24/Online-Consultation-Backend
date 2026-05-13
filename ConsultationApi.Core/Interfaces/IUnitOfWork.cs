using ConsultationApi.Core.Entities;

namespace ConsultationApi.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Doctor> Doctors { get; }
    IRepository<Patient> Patients { get; }
    IRepository<Appointment> Appointments { get; }
    IRepository<ConsultationSession> ConsultationSessions { get; }
    IRepository<ChatMessage> ChatMessages { get; }
    IRepository<Prescription> Prescriptions { get; }
    IRepository<MedicationItem> MedicationItems { get; }
    IRepository<Review> Reviews { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    Task<int> CommitAsync();
}
