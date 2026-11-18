using GentleHealersApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GentleHealersApi.Data;

/// <summary>
/// EF Core mapping for the tables currently needed by role-related API queries.
/// This context does not create or modify the database schema.
/// </summary>
public sealed class GentleHealersDbContext(DbContextOptions<GentleHealersDbContext> options)
    : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role", "dbo");
            entity.HasKey(role => role.RoleId);
            entity.Property(role => role.RoleId).HasColumnName("RoleId");
            entity.Property(role => role.Name).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUser", "dbo");
            entity.HasKey(user => user.UserId);
            entity.Property(user => user.UserId).HasColumnName("UserId");
            entity.Property(user => user.RoleId).HasColumnName("RoleId");
            entity.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
