using ConsultationApi.Core.DTOs.Reviews;

namespace ConsultationApi.Core.Interfaces;

public interface IReviewService
{
    Task<ReviewDto> SubmitReviewAsync(SubmitReviewDto request);
}
