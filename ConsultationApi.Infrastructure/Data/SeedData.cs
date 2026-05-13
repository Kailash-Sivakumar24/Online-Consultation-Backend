using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace ConsultationApi.Infrastructure.Data;

public static class SeedData
{
    private static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DoctorUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid PatientUserId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid DoctorId = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid PatientId = Guid.Parse("00000000-0000-0000-0000-000000000005");

    public static void Seed(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = AdminUserId,
                Email = "admin@consultation.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                Id = DoctorUserId,
                Email = "doctor@consultation.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Doctor@123"),
                Role = UserRole.Doctor,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                Id = PatientUserId,
                Email = "patient@consultation.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient@123"),
                Role = UserRole.Patient,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        modelBuilder.Entity<Doctor>().HasData(
            new Doctor
            {
                Id = DoctorId,
                UserId = DoctorUserId,
                FullName = "Dr. Jane Smith",
                Specialization = "General Medicine",
                LicenseNumber = "LIC-001",
                ConsultationFee = 100.00m,
                Bio = "Experienced general practitioner with 10 years of practice.",
                AverageRating = 0,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        modelBuilder.Entity<Patient>().HasData(
            new Patient
            {
                Id = PatientId,
                UserId = PatientUserId,
                FullName = "John Doe",
                DateOfBirth = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                Gender = Gender.Male,
                BloodGroup = "O+",
                CreatedAt = now,
                UpdatedAt = now
            }
        );
    }
}
