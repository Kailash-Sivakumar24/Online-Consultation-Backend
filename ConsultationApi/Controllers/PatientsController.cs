using ConsultationApi.Core.DTOs.Prescriptions;
using ConsultationApi.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultationApi.Controllers;

/// <summary>Patient-specific endpoints</summary>
[ApiController]
[Route("api/patients")]
[Authorize(Roles = "Patient")]
public class PatientsController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;

    public PatientsController(IPrescriptionService prescriptionService)
    {
        _prescriptionService = prescriptionService;
    }

    /// <summary>Get all prescriptions for the logged-in patient.</summary>
    [HttpGet("me/prescriptions")]
    [ProducesResponseType(typeof(IEnumerable<PrescriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPrescriptions()
    {
        var result = await _prescriptionService.GetMyPrescriptionsAsync();
        return Ok(result);
    }
}
