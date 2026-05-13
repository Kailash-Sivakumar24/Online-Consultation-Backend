using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConsultationApi.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.ScheduledAt).HasColumnType("timestamptz");
        builder.Property(a => a.Status).HasConversion<string>();
        builder.Property(a => a.CreatedAt).HasColumnType("timestamptz");
        builder.Property(a => a.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(a => a.DeletedAt).HasColumnType("timestamptz");
        builder.HasQueryFilter(a => a.DeletedAt == null);

        builder.HasIndex(a => new { a.DoctorId, a.ScheduledAt });

        builder.HasOne(a => a.Patient).WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Doctor).WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.ConsultationSession).WithOne(s => s.Appointment)
            .HasForeignKey<ConsultationSession>(s => s.AppointmentId);
        builder.HasOne(a => a.Review).WithOne(r => r.Appointment)
            .HasForeignKey<Review>(r => r.AppointmentId);
    }
}
