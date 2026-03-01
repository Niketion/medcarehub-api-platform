using MedCareHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MedCareHub.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Prestazione> Prestazioni => Set<Prestazione>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<Prestazione>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Slot>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.DoctorId, x.StartsAt, x.EndsAt });
            e.Property(x => x.Status).HasMaxLength(30);

            e.HasOne(x => x.Prestazione)
                .WithMany()
                .HasForeignKey(x => x.PrestazioneId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PatientSub, x.CreatedAt });

            // ONE active booking per slot (status != cancelled)
            e.HasIndex(x => x.SlotId)
                .IsUnique()
                .HasFilter($"\"Status\" <> '{BookingStatus.Cancelled}'");

            e.HasOne(x => x.Slot)
                .WithMany()
                .HasForeignKey(x => x.SlotId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.Status).HasMaxLength(30);
        });

        modelBuilder.Entity<Report>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PatientSub, x.CreatedAt });
            e.HasOne(x => x.Booking)
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.Bucket).HasMaxLength(100);
            e.Property(x => x.ObjectKey).HasMaxLength(512);
            e.Property(x => x.FileName).HasMaxLength(255);
            e.Property(x => x.ContentType).HasMaxLength(255);

            e.Property(x => x.ReportType).HasMaxLength(80);
            e.Property(x => x.AuthorSub).HasMaxLength(200);
            e.Property(x => x.AuthorRole).HasMaxLength(80);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Timestamp);
            e.Property(x => x.Event).HasMaxLength(120);
            e.Property(x => x.ActorSub).HasMaxLength(200);
            e.Property(x => x.Outcome).HasMaxLength(20);
            e.Property(x => x.ResourceType).HasMaxLength(80);
            e.Property(x => x.ResourceId).HasMaxLength(80);
        });

        base.OnModelCreating(modelBuilder);
    }
}