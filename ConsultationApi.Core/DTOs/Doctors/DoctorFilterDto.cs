namespace ConsultationApi.Core.DTOs.Doctors;

public class DoctorFilterDto
{
    public string? Specialization { get; set; }
    public decimal? MinRating { get; set; }
    public decimal? MaxFee { get; set; }
    public DateTime? AvailableDate { get; set; }
}
