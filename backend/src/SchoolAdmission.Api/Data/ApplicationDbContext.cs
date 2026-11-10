using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models.Entities;

namespace SchoolAdmission.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<SeatHold> Holds => Set<SeatHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.ToTable("classes");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id").HasMaxLength(64);
            entity.Property(c => c.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(c => c.TotalSeats).HasColumnName("total_seats").IsRequired();
            entity.Property(c => c.CreatedAt).HasColumnName("created_at").HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<SeatHold>(entity =>
        {
            entity.ToTable("holds");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasColumnName("id").HasMaxLength(64);
            entity.Property(h => h.ClassId).HasColumnName("class_id").IsRequired().HasMaxLength(64);
            entity.Property(h => h.StudentId).HasColumnName("student_id").IsRequired().HasMaxLength(30);
            entity.Property(h => h.Status).HasColumnName("status").IsRequired().HasConversion<string>().HasMaxLength(20);

            entity.Property(h => h.ExpiresAt).HasColumnName("expires_at").HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            entity.Property(h => h.ConfirmedAt).HasColumnName("confirmed_at").HasConversion(
                v => v.HasValue ? v.Value.ToUniversalTime() : (DateTime?)null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

            entity.Property(h => h.CreatedAt).HasColumnName("created_at").HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            entity.HasOne(h => h.SchoolClass)
                .WithMany(c => c.Holds)
                .HasForeignKey(h => h.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt });
            entity.HasIndex(h => new { h.StudentId, h.ClassId });
        });
    }
}
