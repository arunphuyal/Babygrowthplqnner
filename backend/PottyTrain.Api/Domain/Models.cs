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
    Note = 6
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Parent;
    public Plan Plan { get; set; } = Plan.Free;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public List<Child> Children { get; set; } = new();
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
    /// <summary>Fluid volume in ml (Fluid), or Bristol stool scale 1-7 (Poop).</summary>
    public int? Amount { get; set; }
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
