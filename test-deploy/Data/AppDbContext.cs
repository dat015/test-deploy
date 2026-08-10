using Microsoft.EntityFrameworkCore;
using test_deploy.Models;

namespace test_deploy.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Username)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.Property(u => u.PasswordHash)
                  .IsRequired();

            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.Property(u => u.UpdatedAt)
                  .HasDefaultValueSql("NOW()");

            // Unique constraints
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
        });
    }
}
