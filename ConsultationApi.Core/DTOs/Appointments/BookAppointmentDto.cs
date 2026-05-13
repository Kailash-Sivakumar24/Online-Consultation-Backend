using ConsultationApi.Core.Enums;

namespace ConsultationApi.Core.DTOs.Appointments;

public class BookAppointmentDto
{
    public Guid DoctorId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public SessionType SessionType { get; set; }
}
