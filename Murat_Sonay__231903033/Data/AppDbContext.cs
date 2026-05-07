using LRP.Models;
using Microsoft.EntityFrameworkCore;

namespace LRP.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Lab> Labs => Set<Lab>();
    public DbSet<Computer> Computers => Set<Computer>();
    public DbSet<UserAccount> Users => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Computer>()
            .HasOne(c => c.AssignedUser)
            .WithOne(u => u.AssignedComputer)
            .HasForeignKey<Computer>(c => c.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserAccount>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<UserAccount>()
            .HasIndex(u => u.StudentNumber)
            .IsUnique();
    }
}
