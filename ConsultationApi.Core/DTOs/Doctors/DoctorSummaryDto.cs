namespace ConsultationApi.Core.DTOs.Doctors;

public class DoctorSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public decimal AverageRating { get; set; }
}
