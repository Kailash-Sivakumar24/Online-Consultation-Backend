using ConsultationApi.Core.DTOs.Appointments;
using ConsultationApi.Core.DTOs.Common;

namespace ConsultationApi.Core.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentDto> BookAsync(BookAppointmentDto request);
    Task<PagedResult<AppointmentDto>> GetMyAppointmentsAsync(int page, int pageSize);
    Task<AppointmentDto> GetByIdAsync(Guid id);
    Task<AppointmentDto> ConfirmAsync(Guid id);
    Task<AppointmentDto> CancelAsync(Guid id, string? reason);
}
