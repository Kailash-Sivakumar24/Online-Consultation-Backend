namespace ConsultationApi.Core.DTOs.Doctors;

public class DoctorDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public string? Bio { get; set; }
    public decimal AverageRating { get; set; }
    public string Email { get; set; } = string.Empty;
}
