using ConsultationApi.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConsultationApi.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnType("timestamptz");
        builder.Property(r => r.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(r => r.DeletedAt).HasColumnType("timestamptz");
        builder.HasQueryFilter(r => r.DeletedAt == null);

        builder.HasOne(r => r.Patient).WithMany()
            .HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}
