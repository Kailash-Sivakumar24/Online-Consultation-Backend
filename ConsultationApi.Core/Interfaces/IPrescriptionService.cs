using ConsultationApi.Core.DTOs.Prescriptions;

namespace ConsultationApi.Core.Interfaces;

public interface IPrescriptionService
{
    Task<PrescriptionDto> IssuePrescriptionAsync(IssuePrescriptionDto request);
    Task<PrescriptionDto> GetByIdAsync(Guid id);
    Task<IEnumerable<PrescriptionDto>> GetMyPrescriptionsAsync();
}
