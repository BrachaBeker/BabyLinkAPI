// Fluent API ל-Notification: קשר N:1 ל-User (Cascade - משתמש שנמחק, ההודעות שלו נמחקות),
// Type נשמר כמחרוזת, ואינדקס על UserId (כל הקריאות הן "ההודעות שלי").
using BabyLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyLink.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.City).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Street).IsRequired().HasMaxLength(150);
        builder.Property(n => n.HouseNumber).IsRequired().HasMaxLength(20);
        builder.Property(n => n.ParentFullName).IsRequired().HasMaxLength(100);
        builder.Property(n => n.CreatedAtUtc).IsRequired();

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.UserId);
    }
}
