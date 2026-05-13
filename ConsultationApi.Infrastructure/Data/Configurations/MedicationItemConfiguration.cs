using ConsultationApi.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConsultationApi.Infrastructure.Data.Configurations;

public class MedicationItemConfiguration : IEntityTypeConfiguration<MedicationItem>
{
    public void Configure(EntityTypeBuilder<MedicationItem> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(m => m.DrugName).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Dosage).IsRequired().HasMaxLength(100);
        builder.Property(m => m.CreatedAt).HasColumnType("timestamptz");
        builder.Property(m => m.UpdatedAt).HasColumnType("timestamptz");

        builder.HasOne(m => m.Prescription).WithMany(p => p.MedicationItems)
            .HasForeignKey(m => m.PrescriptionId).OnDelete(DeleteBehavior.Cascade);

        // Match the soft-delete filter on Prescription so EF doesn't warn about filter mismatch
        builder.HasQueryFilter(m => m.Prescription.DeletedAt == null);
    }
}
