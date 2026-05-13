using ConsultationApi.Core.DTOs.Common;
using ConsultationApi.Core.DTOs.Doctors;

namespace ConsultationApi.Core.Interfaces;

public interface IDoctorService
{
    Task<PagedResult<DoctorSummaryDto>> GetDoctorsAsync(DoctorFilterDto filter, int page, int pageSize);
    Task<DoctorDetailDto> GetDoctorByIdAsync(Guid id);
    Task<IEnumerable<TimeSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateTime from, DateTime to);
}
