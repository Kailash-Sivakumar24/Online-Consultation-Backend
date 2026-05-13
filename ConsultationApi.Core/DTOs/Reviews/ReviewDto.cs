namespace ConsultationApi.Core.DTOs.Reviews;

public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
