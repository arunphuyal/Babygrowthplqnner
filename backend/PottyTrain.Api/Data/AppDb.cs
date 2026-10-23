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
    public DbSet<ChildCaregiver> Caregivers => Set<ChildCaregiver>();
    public DbSet<ChildInvite> Invites => Set<ChildInvite>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<User>().Property(u => u.Email).HasMaxLength(256);
        b.Entity<Child>().HasOne(c => c.Owner).WithMany(u => u.Children).HasForeignKey(c => c.OwnerId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<LogEntry>().HasOne(l => l.Child).WithMany(c => c.Logs).HasForeignKey(l => l.ChildId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<LogEntry>().HasIndex(l => new { l.ChildId, l.OccurredAt });
        b.Entity<LogEntry>().Property(l => l.Category).HasMaxLength(30);
        b.Entity<LogEntry>().Property(l => l.Tags).HasMaxLength(200);
        b.Entity<Child>().Property(c => c.TimeZoneId).HasMaxLength(64).HasDefaultValue("UTC");
        b.Entity<ChildCaregiver>().HasIndex(c => new { c.ChildId, c.UserId }).IsUnique();
        b.Entity<ChildCaregiver>().HasOne(c => c.Child).WithMany().HasForeignKey(c => c.ChildId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ChildCaregiver>().HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ChildInvite>().HasIndex(i => i.CodeHash).IsUnique();
        b.Entity<ChildInvite>().HasOne(i => i.Child).WithMany().HasForeignKey(i => i.ChildId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RefreshToken>().HasIndex(t => t.TokenHash).IsUnique();
        b.Entity<RefreshToken>().HasIndex(t => t.UserId);
        b.Entity<PasswordResetToken>().HasIndex(t => t.TokenHash).IsUnique();
        b.Entity<Article>().HasIndex(a => a.Slug).IsUnique();
        b.Entity<WaitlistEntry>().HasIndex(w => w.Email).IsUnique();
        b.Entity<PlanOffer>().Property(p => p.MonthlyPrice).HasPrecision(10, 2);
        b.Entity<PlanOffer>().Property(p => p.YearlyPrice).HasPrecision(10, 2);
    }
}

/// <summary>Lets `dotnet ef` create the context without starting the whole app (which would seed/migrate on startup).</summary>
public class AppDbFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDb>().UseSqlite("Data Source=design-time.db").Options);
}
