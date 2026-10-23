using System.ComponentModel.DataAnnotations;
using PottyTrain.Api.Domain;

namespace PottyTrain.Api.Dtos;

public record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(8)] string Password, [Required] string DisplayName);
public record LoginRequest([Required] string Email, [Required] string Password);
public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);
public record UserDto(Guid Id, string Email, string DisplayName, UserRole Role, Plan Plan, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt);

public record ChildRequest([Required, MaxLength(100)] string Name, DateOnly BirthDate, DateOnly? TrainingStartDate, string? Notes);
public record ChildDto(Guid Id, string Name, DateOnly BirthDate, DateOnly? TrainingStartDate, string? Notes);

public record LogRequest(LogType Type, DateTime? OccurredAt, bool? InPotty, int? Amount, string? Notes);
public record LogDto(Guid Id, Guid ChildId, LogType Type, DateTime OccurredAt, bool? InPotty, int? Amount, string? Notes);

public record DayInsight(DateOnly Date, int Successes, int Accidents, int Sits, int Fluids);
public record InsightsDto(
    int Days, int TotalSuccesses, int TotalAccidents, double SuccessRatePercent,
    int CurrentDryStreakHours, double AvgMinutesBetweenSuccesses,
    DateTime? PredictedNextVisit, int[] SuccessesByHour, List<DayInsight> Daily);

public record ArticleRequest([Required] string Slug, [Required] string Title, string Summary, string Body, bool IsPremium, bool IsPublished);
public record PlanOfferRequest(Plan Plan, [Required] string Name, decimal MonthlyPrice, decimal YearlyPrice, string Features, bool IsActive);
public record UpdateUserRequest(Plan? Plan, bool? IsActive, UserRole? Role);
public record WaitlistRequest([Required, EmailAddress] string Email, string? Source);
