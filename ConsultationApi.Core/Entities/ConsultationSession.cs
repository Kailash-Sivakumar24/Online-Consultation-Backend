using ConsultationApi.Core.Enums;

namespace ConsultationApi.Core.Entities;

public class ConsultationSession : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public SessionType SessionType { get; set; }
    public string? RoomUrl { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Appointment Appointment { get; set; } = null!;
    public Prescription? Prescription { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
