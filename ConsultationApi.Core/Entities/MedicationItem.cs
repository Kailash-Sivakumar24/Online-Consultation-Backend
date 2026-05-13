namespace ConsultationApi.Core.Entities;

public class MedicationItem : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public int FrequencyPerDay { get; set; }
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Prescription Prescription { get; set; } = null!;
}
