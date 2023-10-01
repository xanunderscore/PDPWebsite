using Microsoft.EntityFrameworkCore;
using PDPWebsite.Models;

namespace PDPWebsite.Services;

public class DB : DbContext
{
    public DB(DbContextOptions<DB> options) : base(options)
    {

    }

    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<SignUp> Signups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Schedule>(options =>
        {
            options.HasKey(e => e.Id);

            options.HasMany(e => e.Signups)
                .WithOne(e => e.Schedule)
                .HasForeignKey(e => e.ScheduleId);
        });

        modelBuilder.Entity<SignUp>(options =>
        {
            options.HasKey(e => e.Id);
        });
    }
}
