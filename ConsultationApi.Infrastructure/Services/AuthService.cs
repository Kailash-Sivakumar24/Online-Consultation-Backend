using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ConsultationApi.Core.DTOs.Auth;
using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using ConsultationApi.Core.Exceptions;
using ConsultationApi.Core.Interfaces;
using ConsultationApi.Core.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ConsultationApi.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly JwtSettings _jwtSettings;

    public AuthService(IUnitOfWork uow, IOptions<JwtSettings> jwtSettings)
    {
        _uow = uow;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existing = await _uow.Users.FindAsync(u => u.Email == request.Email);
        if (existing.Any())
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true
        };
        await _uow.Users.AddAsync(user);

        if (request.Role == UserRole.Doctor)
        {
            if (string.IsNullOrWhiteSpace(request.Specialization) ||
                string.IsNullOrWhiteSpace(request.LicenseNumber) ||
                request.ConsultationFee == null)
                throw new BusinessRuleViolationException("Doctors must provide Specialization, LicenseNumber, and ConsultationFee.");

            var doctor = new Doctor
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FullName = request.FullName,
                Specialization = request.Specialization,
                LicenseNumber = request.LicenseNumber,
                ConsultationFee = request.ConsultationFee.Value,
                Bio = request.Bio
            };
            await _uow.Doctors.AddAsync(doctor);
        }
        else if (request.Role == UserRole.Patient)
        {
            if (request.DateOfBirth == null || request.Gender == null)
                throw new BusinessRuleViolationException("Patients must provide DateOfBirth and Gender.");

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FullName = request.FullName,
                DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc),
                Gender = request.Gender.Value,
                BloodGroup = request.BloodGroup,
                MedicalHistorySummary = request.MedicalHistorySummary
            };
            await _uow.Patients.AddAsync(patient);
        }

        await _uow.CommitAsync();
        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var users = await _uow.Users.FindAsync(u => u.Email == request.Email);
        var user = users.FirstOrDefault();
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new BusinessRuleViolationException("Invalid email or password.");

        if (!user.IsActive)
            throw new ForbiddenException("Account is deactivated.");

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var tokens = await _uow.RefreshTokens.FindAsync(t => t.Token == refreshToken && !t.IsRevoked);
        var token = tokens.FirstOrDefault();
        if (token == null || token.ExpiresAt < DateTime.UtcNow)
            throw new BusinessRuleViolationException("Invalid or expired refresh token.");

        token.IsRevoked = true;
        await _uow.RefreshTokens.UpdateAsync(token);

        var users = await _uow.Users.FindAsync(u => u.Id == token.UserId);
        var user = users.First();
        return await IssueTokensAsync(user);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var tokens = await _uow.RefreshTokens.FindAsync(t => t.Token == refreshToken && !t.IsRevoked);
        var token = tokens.FirstOrDefault();
        if (token != null)
        {
            token.IsRevoked = true;
            await _uow.RefreshTokens.UpdateAsync(token);
            await _uow.CommitAsync();
        }
    }

    private async Task<AuthResponseDto> IssueTokensAsync(User user)
    {
        var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);
        var jti = Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, jti)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        var refreshTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            IsRevoked = false
        };
        await _uow.RefreshTokens.AddAsync(refreshToken);
        await _uow.CommitAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = expiry,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }
}
