using AutoMapper;
using ConsultationApi.Core.DTOs.Common;
using ConsultationApi.Core.DTOs.Doctors;
using ConsultationApi.Core.Exceptions;
using ConsultationApi.Core.Interfaces;

namespace ConsultationApi.Infrastructure.Services;

public class DoctorService : IDoctorService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    private const string DoctorListCacheKey = "doctors:list";
    private static string DoctorCacheKey(Guid id) => $"doctors:{id}";

    public DoctorService(IUnitOfWork uow, IMapper mapper, ICacheService cache)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<PagedResult<DoctorSummaryDto>> GetDoctorsAsync(DoctorFilterDto filter, int page, int pageSize)
    {
        var allDoctors = await _cache.GetAsync<List<DoctorSummaryDto>>(DoctorListCacheKey);
        if (allDoctors == null)
        {
            var entities = await _uow.Doctors.GetAllAsync();
            allDoctors = entities.Select(d => _mapper.Map<DoctorSummaryDto>(d)).ToList();
            await _cache.SetAsync(DoctorListCacheKey, allDoctors, TimeSpan.FromMinutes(5));
        }

        IEnumerable<DoctorSummaryDto> doctors = allDoctors;

        if (!string.IsNullOrEmpty(filter.Specialization))
            doctors = doctors.Where(d => d.Specialization.Equals(filter.Specialization, StringComparison.OrdinalIgnoreCase));
        if (filter.MinRating.HasValue)
            doctors = doctors.Where(d => d.AverageRating >= filter.MinRating.Value);
        if (filter.MaxFee.HasValue)
            doctors = doctors.Where(d => d.ConsultationFee <= filter.MaxFee.Value);

        if (filter.AvailableDate.HasValue)
        {
            var date = filter.AvailableDate.Value.Date;
            var dayStart = date.AddHours(9);
            var dayEnd = date.AddHours(17);

            var bookedAppointments = await _uow.Appointments.FindAsync(a =>
                a.ScheduledAt >= dayStart && a.ScheduledAt < dayEnd &&
                (a.Status == Core.Enums.AppointmentStatus.Confirmed || a.Status == Core.Enums.AppointmentStatus.Pending));

            var bookedByDoctor = bookedAppointments
                .GroupBy(a => a.DoctorId)
                .ToDictionary(g => g.Key, g => g.ToList());

            doctors = doctors.Where(d =>
            {
                if (!bookedByDoctor.TryGetValue(d.Id, out var appts))
                    return true; // no bookings on that day — fully available

                // check if at least one 30-min slot is free
                var current = dayStart;
                while (current < dayEnd)
                {
                    var slotEnd = current.AddMinutes(30);
                    if (!appts.Any(a => a.ScheduledAt < slotEnd && a.ScheduledAt.AddMinutes(a.DurationMinutes) > current))
                        return true;
                    current = slotEnd;
                }
                return false;
            });
        }

        var filtered = doctors.ToList();
        var total = filtered.Count;
        var data = filtered.Skip((page - 1) * pageSize).Take(pageSize);

        return new PagedResult<DoctorSummaryDto>
        {
            Data = data,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DoctorDetailDto> GetDoctorByIdAsync(Guid id)
    {
        var cached = await _cache.GetAsync<DoctorDetailDto>(DoctorCacheKey(id));
        if (cached != null) return cached;

        var doctor = await _uow.Doctors.GetByIdAsync(id)
            ?? throw new NotFoundException("Doctor", id);

        var dto = _mapper.Map<DoctorDetailDto>(doctor);
        await _cache.SetAsync(DoctorCacheKey(id), dto, TimeSpan.FromMinutes(10));
        return dto;
    }

    public async Task<IEnumerable<TimeSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateTime from, DateTime to)
    {
        var doctor = await _uow.Doctors.GetByIdAsync(doctorId)
            ?? throw new NotFoundException("Doctor", doctorId);

        var appointments = await _uow.Appointments.FindAsync(a =>
            a.DoctorId == doctorId &&
            a.ScheduledAt >= from && a.ScheduledAt <= to &&
            (a.Status == Core.Enums.AppointmentStatus.Confirmed || a.Status == Core.Enums.AppointmentStatus.Pending));

        var slots = new List<TimeSlotDto>();
        var current = from.Date.AddHours(9);
        var end = to.Date.AddHours(17);

        while (current < end)
        {
            var slotEnd = current.AddMinutes(30);
            var isBooked = appointments.Any(a => a.ScheduledAt < slotEnd && a.ScheduledAt.AddMinutes(a.DurationMinutes) > current);

            slots.Add(new TimeSlotDto
            {
                StartTime = current,
                EndTime = slotEnd,
                IsAvailable = !isBooked
            });
            current = slotEnd;
        }

        return slots;
    }
}
