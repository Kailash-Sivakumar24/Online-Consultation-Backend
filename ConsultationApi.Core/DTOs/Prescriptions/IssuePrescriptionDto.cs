namespace ConsultationApi.Core.DTOs.Prescriptions;

public class IssuePrescriptionDto
{
    public Guid SessionId { get; set; }
    public string? Instructions { get; set; }
    public List<MedicationItemDto> MedicationItems { get; set; } = new();
}

public class MedicationItemDto
{
    public string DrugName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public int FrequencyPerDay { get; set; }
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }
}
