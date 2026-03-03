using GsNetRobo.Models;
using Microsoft.EntityFrameworkCore;

namespace GsNetRobo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AutomationJob> Jobs => Set<AutomationJob>();
    public DbSet<JobDetail> JobDetails => Set<JobDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AutomationJob>()
            .HasMany(j => j.Detalhes)
            .WithOne(d => d.AutomationJob)
            .HasForeignKey(d => d.AutomationJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
