namespace ConsultationApi.Core.DTOs.Reviews;

public class SubmitReviewDto
{
    public Guid AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
