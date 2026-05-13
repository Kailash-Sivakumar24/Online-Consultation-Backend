using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConsultationApi.Infrastructure.Data.Configurations;

public class ConsultationSessionConfiguration : IEntityTypeConfiguration<ConsultationSession>
{
    public void Configure(EntityTypeBuilder<ConsultationSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.SessionType).HasConversion<string>();
        builder.Property(s => s.StartedAt).HasColumnType("timestamptz");
        builder.Property(s => s.EndedAt).HasColumnType("timestamptz");
        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnType("timestamptz");
        builder.HasQueryFilter(s => s.DeletedAt == null);

        builder.HasOne(s => s.Prescription).WithOne(p => p.Session)
            .HasForeignKey<Prescription>(p => p.SessionId);
        builder.HasMany(s => s.Messages).WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId);
    }
}
