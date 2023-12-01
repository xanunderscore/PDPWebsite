namespace PDPWebsite.Services;

public class Database : DbContext
{
    // initialized by dbcontext
#pragma warning disable CS8618
    public Database(DbContextOptions<Database> options) : base(options)
    {
    }
#pragma warning restore

    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<SignUp> Signups { get; set; }
    public DbSet<AboutInfo> AboutInfos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Schedule>(options =>
        {
            options.HasKey(e => e.Id);

            options.HasMany(e => e.Signups)
                .WithOne(e => e.Schedule)
                .HasForeignKey(e => e.ScheduleId);
        });

        modelBuilder.Entity<SignUp>(options => { options.HasKey(e => e.Id); });

        modelBuilder.Entity<AboutInfo>(options => { options.HasKey(e => e.Id); });
    }
}
