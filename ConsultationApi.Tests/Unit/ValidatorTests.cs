using ConsultationApi.Core.DTOs.Auth;
using ConsultationApi.Core.DTOs.Appointments;
using ConsultationApi.Core.DTOs.Reviews;
using ConsultationApi.Core.Enums;
using ConsultationApi.Validators;
using FluentValidation.TestHelper;

namespace ConsultationApi.Tests.Unit;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Valid_DoctorRequest_PassesValidation()
    {
        var dto = new RegisterRequestDto
        {
            Email = "doctor@test.com",
            Password = "SecureP@ss1",
            FullName = "Dr. Smith",
            Role = UserRole.Doctor,
            Specialization = "Cardiology",
            LicenseNumber = "LIC-001",
            ConsultationFee = 150m
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_WeakPassword_FailsValidation()
    {
        var dto = new RegisterRequestDto
        {
            Email = "test@test.com",
            Password = "password",
            FullName = "Test",
            Role = UserRole.Patient,
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Gender = Gender.Male
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Invalid_BadEmail_FailsValidation()
    {
        var dto = new RegisterRequestDto
        {
            Email = "not-an-email",
            Password = "SecureP@ss1",
            FullName = "Test",
            Role = UserRole.Patient,
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Gender = Gender.Male
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Doctor_MissingFee_FailsValidation()
    {
        var dto = new RegisterRequestDto
        {
            Email = "doctor@test.com",
            Password = "SecureP@ss1",
            FullName = "Dr. Smith",
            Role = UserRole.Doctor,
            Specialization = "Cardiology",
            LicenseNumber = "LIC-001"
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ConsultationFee);
    }
}

public class BookAppointmentValidatorTests
{
    private readonly BookAppointmentValidator _validator = new();

    [Fact]
    public void Valid_Request_PassesValidation()
    {
        var dto = new BookAppointmentDto
        {
            DoctorId = Guid.NewGuid(),
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 30,
            SessionType = SessionType.Video
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PastDate_FailsValidation()
    {
        var dto = new BookAppointmentDto
        {
            DoctorId = Guid.NewGuid(),
            ScheduledAt = DateTime.UtcNow.AddDays(-1),
            DurationMinutes = 30,
            SessionType = SessionType.Video
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ScheduledAt);
    }
}

public class SubmitReviewValidatorTests
{
    private readonly SubmitReviewValidator _validator = new();

    [Fact]
    public void Valid_Review_PassesValidation()
    {
        var dto = new SubmitReviewDto { AppointmentId = Guid.NewGuid(), Rating = 5, Comment = "Great!" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RatingAbove5_FailsValidation()
    {
        var dto = new SubmitReviewDto { AppointmentId = Guid.NewGuid(), Rating = 6 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void RatingBelow1_FailsValidation()
    {
        var dto = new SubmitReviewDto { AppointmentId = Guid.NewGuid(), Rating = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }
}
