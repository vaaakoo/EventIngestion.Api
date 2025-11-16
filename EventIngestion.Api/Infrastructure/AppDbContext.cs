using EventIngestion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventIngestion.Api.Infrastructure;

public class AppDbContext : DbContext
{
    public DbSet<RawEvent> RawEvents => Set<RawEvent>();
    public DbSet<MappedEvent> MappedEvents => Set<MappedEvent>();
    public DbSet<MappingRule> MappingRules => Set<MappingRule>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    #region Fluent API Entity Configuration
    // Fluent API Entity Configuration
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RawEvent>(entity =>
        {
            entity.ToTable("RawEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RawPayload).IsRequired();
            entity.Property(e => e.ReceivedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.Status).HasDefaultValue(0);
        });

        modelBuilder.Entity<MappedEvent>(entity =>
        {
            entity.ToTable("MappedEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.RawEvent)
                  .WithMany()
                  .HasForeignKey(e => e.RawEventId);
        });

        modelBuilder.Entity<MappingRule>(entity =>
        {
            entity.ToTable("MappingRules");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExternalName).IsUnique();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
    #endregion
}
