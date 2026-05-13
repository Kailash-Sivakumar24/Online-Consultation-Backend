using ConsultationApi.Core.Enums;

namespace ConsultationApi.Core.DTOs.Appointments;

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? CancellationReason { get; set; }
    public Guid? SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
