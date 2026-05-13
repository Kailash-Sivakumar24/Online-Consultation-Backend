using ConsultationApi.Core.DTOs.Sessions;
using ConsultationApi.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultationApi.Controllers;

/// <summary>Consultation session management endpoints</summary>
[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly IConsultationService _consultationService;

    public SessionsController(IConsultationService consultationService)
    {
        _consultationService = consultationService;
    }

    /// <summary>Start a consultation session (Doctor only).</summary>
    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Start(Guid id)
    {
        var result = await _consultationService.StartSessionAsync(id);
        return Ok(result);
    }

    /// <summary>End a consultation session (Doctor only).</summary>
    [HttpPost("{id:guid}/end")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> End(Guid id)
    {
        var result = await _consultationService.EndSessionAsync(id);
        return Ok(result);
    }

    /// <summary>Send a chat message in a session.</summary>
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageDto request)
    {
        var result = await _consultationService.AddMessageAsync(id, request.Content);
        return CreatedAtAction(nameof(GetMessages), new { id }, result);
    }

    /// <summary>Retrieve all messages in a session.</summary>
    [HttpGet("{id:guid}/messages")]
    [ProducesResponseType(typeof(IEnumerable<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid id)
    {
        var result = await _consultationService.GetMessagesAsync(id);
        return Ok(result);
    }
}
