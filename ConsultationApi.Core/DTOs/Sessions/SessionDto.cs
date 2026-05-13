using ConsultationApi.Core.Enums;

namespace ConsultationApi.Core.DTOs.Sessions;

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public SessionType SessionType { get; set; }
    public string? RoomUrl { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Notes { get; set; }
}
