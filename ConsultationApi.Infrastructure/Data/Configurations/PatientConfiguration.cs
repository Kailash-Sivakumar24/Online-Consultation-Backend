using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConsultationApi.Infrastructure.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.FullName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.DateOfBirth).HasColumnType("timestamptz");
        builder.Property(p => p.Gender).HasConversion<string>();
        builder.Property(p => p.BloodGroup).HasMaxLength(10);
        builder.Property(p => p.CreatedAt).HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(p => p.DeletedAt).HasColumnType("timestamptz");
        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
