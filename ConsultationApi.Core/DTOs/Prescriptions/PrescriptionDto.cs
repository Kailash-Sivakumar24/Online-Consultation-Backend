namespace ConsultationApi.Core.DTOs.Prescriptions;

public class PrescriptionDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public DateTime IssuedAt { get; set; }
    public string? Instructions { get; set; }
    public List<MedicationItemResponseDto> MedicationItems { get; set; } = new();
}

public class MedicationItemResponseDto
{
    public Guid Id { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public int FrequencyPerDay { get; set; }
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }
}
