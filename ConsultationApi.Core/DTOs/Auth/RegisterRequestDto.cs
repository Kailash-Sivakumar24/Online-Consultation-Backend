using ConsultationApi.Core.Enums;

namespace ConsultationApi.Core.DTOs.Auth;

public class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string FullName { get; set; } = string.Empty;

    // Doctor-specific
    public string? Specialization { get; set; }
    public string? LicenseNumber { get; set; }
    public decimal? ConsultationFee { get; set; }
    public string? Bio { get; set; }

    // Patient-specific
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? BloodGroup { get; set; }
    public string? MedicalHistorySummary { get; set; }
}
