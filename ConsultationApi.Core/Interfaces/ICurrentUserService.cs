using ConsultationApi.Core.Enums;

namespace ConsultationApi.Core.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
}
