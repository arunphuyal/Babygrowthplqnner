namespace PottyTrain.Api.Domain;

public enum UserRole { Parent = 0, Admin = 1 }

public enum Plan { Free = 0, Premium = 1, Family = 2 }

public enum LogType
{
    Pee = 0,
    Poop = 1,
    Accident = 2,
    Sit = 3,        // sat on the potty, nothing happened
    Fluid = 4,
    Reward = 5,
    Note = 6,
    Food = 7        // meal or snack
}

/// <summary>Rough size, used when parents don't measure: dribble/sip, small, medium, large.</summary>
public enum LogSize { Tiny = 0, Small = 1, Medium = 2, Large = 3 }

public enum OutputColour { Normal = 0, Pale = 1, Dark = 2, Green = 3, Red = 4, Black = 5 }

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Parent;
    public Plan Plan { get; set; } = Plan.Free;
    /// <summary>When a paid plan lapses (set by billing webhooks). Null = no expiry (e.g. granted by an admin).</summary>
    public DateTime? PlanExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public List<Child> Children { get; set; } = new();

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Plan EffectivePlan => PlanExpiresAt is { } e && e < DateTime.UtcNow ? Plan.Free : Plan;
}

public class Child
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    public string Name { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public DateOnly? TrainingStartDate { get; set; }
    public string? Notes { get; set; }
    /// <summary>IANA time zone (e.g. "Europe/London") used to bucket logs into the child's local days and hours.</summary>
    public string TimeZoneId { get; set; } = "UTC";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<LogEntry> Logs { get; set; } = new();
}

public class LogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Child? Child { get; set; }
    public LogType Type { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    /// <summary>Optional: for Pee/Poop/Sit, true if it happened in the potty/toilet.</summary>
    public bool? InPotty { get; set; }
    /// <summary>Volume in ml: drink amount (Fluid) or measured urine (Pee).</summary>
    public int? Amount { get; set; }
    /// <summary>Rough size for Pee, Poop, Fluid and Food when no exact amount is known.</summary>
    public LogSize? Size { get; set; }
    /// <summary>Bristol stool scale 1-7 (Poop only).</summary>
    public int? StoolType { get; set; }
    public OutputColour? Colour { get; set; }
    /// <summary>Fluid: water/milk/juice/...; Food: meal/snack.</summary>
    public string? Category { get; set; }
    /// <summary>Comma-separated lowercase tags, e.g. "fibre,dairy" (Fluid and Food).</summary>
    public string? Tags { get; set; }
    public Guid? LoggedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Article
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsPremium { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PlanOffer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Plan Plan { get; set; }
    public string Name { get; set; } = "";
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string Features { get; set; } = ""; // newline-separated
    public bool IsActive { get; set; } = true;
}

public class WaitlistEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A user (other than the owner) who may view, and optionally add logs to, a child.</summary>
public class ChildCaregiver
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Child? Child { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public bool CanEdit { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChildInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Child? Child { get; set; }
    public string CodeHash { get; set; } = "";
    public bool CanEdit { get; set; } = true;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
}

public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
