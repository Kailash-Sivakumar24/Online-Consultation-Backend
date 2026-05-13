namespace ConsultationApi.Core.Entities;

public class Prescription : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public DateTime IssuedAt { get; set; }
    public string? Instructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ConsultationSession Session { get; set; } = null!;
    public ICollection<MedicationItem> MedicationItems { get; set; } = new List<MedicationItem>();
}
