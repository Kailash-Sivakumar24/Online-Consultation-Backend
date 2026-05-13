using ConsultationApi.Core.DTOs.Common;
using ConsultationApi.Core.DTOs.Doctors;
using ConsultationApi.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsultationApi.Controllers;

/// <summary>Doctor management and availability endpoints</summary>
[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    /// <summary>List all doctors with optional filtering and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DoctorSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctors(
        [FromQuery] string? specialization,
        [FromQuery] decimal? minRating,
        [FromQuery] decimal? maxFee,
        [FromQuery] DateTime? availableDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var filter = new DoctorFilterDto
        {
            Specialization = specialization,
            MinRating = minRating,
            MaxFee = maxFee,
            AvailableDate = availableDate
        };
        var result = await _doctorService.GetDoctorsAsync(filter, page, pageSize);
        return Ok(result);
    }

    /// <summary>Get a doctor's profile by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DoctorDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDoctor(Guid id)
    {
        var result = await _doctorService.GetDoctorByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Get available booking slots for a doctor in a date range.</summary>
    [HttpGet("{id:guid}/slots")]
    [ProducesResponseType(typeof(IEnumerable<TimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSlots(
        Guid id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var result = await _doctorService.GetAvailableSlotsAsync(id, from, to);
        return Ok(result);
    }
}
