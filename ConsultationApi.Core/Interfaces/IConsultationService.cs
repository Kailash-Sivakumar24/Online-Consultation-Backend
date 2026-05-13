using ConsultationApi.Core.DTOs.Sessions;

namespace ConsultationApi.Core.Interfaces;

public interface IConsultationService
{
    Task<SessionDto> StartSessionAsync(Guid sessionId);
    Task<SessionDto> EndSessionAsync(Guid sessionId);
    Task<ChatMessageDto> AddMessageAsync(Guid sessionId, string content);
    Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(Guid sessionId);
}
