using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Domain;

namespace PottyTrain.Api.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<PlanOffer> PlanOffers => Set<PlanOffer>();
    public DbSet<WaitlistEntry> Waitlist => Set<WaitlistEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<User>().Property(u => u.Email).HasMaxLength(256);
        b.Entity<Child>().HasOne(c => c.Owner).WithMany(u => u.Children).HasForeignKey(c => c.OwnerId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<LogEntry>().HasOne(l => l.Child).WithMany(c => c.Logs).HasForeignKey(l => l.ChildId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<LogEntry>().HasIndex(l => new { l.ChildId, l.OccurredAt });
        b.Entity<Article>().HasIndex(a => a.Slug).IsUnique();
        b.Entity<WaitlistEntry>().HasIndex(w => w.Email).IsUnique();
        b.Entity<PlanOffer>().Property(p => p.MonthlyPrice).HasPrecision(10, 2);
        b.Entity<PlanOffer>().Property(p => p.YearlyPrice).HasPrecision(10, 2);
    }
}
