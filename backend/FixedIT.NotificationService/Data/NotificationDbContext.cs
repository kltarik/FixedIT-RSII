using FixedIT.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.NotificationService.Data;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options)
    : DbContext(options)
{
    public DbSet<ProcessedNotificationMessage> ProcessedNotificationMessages =>
        Set<ProcessedNotificationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedNotificationMessage>(entity =>
        {
            entity.ToTable("ProcessedNotificationMessages");
            entity.HasKey(message => message.MessageId);
            entity.Property(message => message.ReceivedAt).HasColumnType("datetime2");
            entity.Property(message => message.ProcessedAt).HasColumnType("datetime2");
        });
    }
}
