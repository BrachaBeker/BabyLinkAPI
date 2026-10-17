// Fluent API ל-ServiceRequest: שני קשרי 1:N ל-User (Parent - Cascade, Babysitter - Restrict),
// M:N ל-Tags, Version כ-Concurrency Token, ואינדקסים על (City, Street) ועל StartTimeUtc.
using BabyLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyLink.Infrastructure.Persistence.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.City).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Street).IsRequired().HasMaxLength(150);
        builder.Property(r => r.HouseNumber).IsRequired().HasMaxLength(20);
        builder.Property(r => r.AddressNotes).HasMaxLength(200);
        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.HourlyRate).HasPrecision(10, 2);

        builder.Property(r => r.Version).IsConcurrencyToken();

        builder.HasOne(r => r.Parent)
            .WithMany(u => u.CreatedRequests)
            .HasForeignKey(r => r.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.AssignedBabysitter)
            .WithMany(u => u.AssignedRequests)
            .HasForeignKey(r => r.AssignedBabysitterId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-Many: טבלת קישור ServiceRequestTags עם מפתח מורכב, בלי מחלקה ב-C#.
        builder.HasMany(r => r.Tags)
            .WithMany(t => t.ServiceRequests)
            .UsingEntity(
                "ServiceRequestTags",
                tag => tag.HasOne(typeof(Tag)).WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                request => request.HasOne(typeof(ServiceRequest)).WithMany().HasForeignKey("ServiceRequestId").OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey("ServiceRequestId", "TagId"));

        builder.HasIndex(r => new { r.City, r.Street });
        builder.HasIndex(r => r.StartTimeUtc);
    }
}
