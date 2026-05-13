using ConsultationApi.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConsultationApi.Infrastructure.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.FullName).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Specialization).IsRequired().HasMaxLength(100);
        builder.Property(d => d.LicenseNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(d => d.LicenseNumber).IsUnique();
        builder.Property(d => d.ConsultationFee).HasColumnType("decimal(10,2)");
        builder.Property(d => d.AverageRating).HasColumnType("decimal(3,2)");
        builder.Property(d => d.CreatedAt).HasColumnType("timestamptz");
        builder.Property(d => d.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(d => d.DeletedAt).HasColumnType("timestamptz");
        builder.HasQueryFilter(d => d.DeletedAt == null);
    }
}
