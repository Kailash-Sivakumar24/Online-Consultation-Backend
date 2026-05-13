using ConsultationApi.Core.DTOs.Appointments;
using ConsultationApi.Core.DTOs.Common;
using ConsultationApi.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultationApi.Controllers;

/// <summary>Appointment booking and management endpoints</summary>
[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>Book a new appointment (Patient only).</summary>
    [HttpPost]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book([FromBody] BookAppointmentDto request)
    {
        var result = await _appointmentService.BookAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>List the caller's own appointments (paginated).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAppointments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _appointmentService.GetMyAppointmentsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>Get appointment detail by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _appointmentService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Confirm an appointment (Doctor only).</summary>
    [HttpPut("{id:guid}/confirm")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _appointmentService.ConfirmAsync(id);
        return Ok(result);
    }

    /// <summary>Cancel an appointment (Patient or Admin).</summary>
    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = "Patient,Admin")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelAppointmentDto? request)
    {
        var result = await _appointmentService.CancelAsync(id, request?.Reason);
        return Ok(result);
    }
}
