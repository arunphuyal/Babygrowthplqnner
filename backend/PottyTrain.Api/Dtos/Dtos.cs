using System.ComponentModel.DataAnnotations;
using PottyTrain.Api.Domain;

namespace PottyTrain.Api.Dtos;

public record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(8)] string Password, [Required] string DisplayName);
public record LoginRequest([Required] string Email, [Required] string Password);
public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);
public record UserDto(Guid Id, string Email, string DisplayName, UserRole Role, Plan Plan, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt);

public record ChildRequest([Required, MaxLength(100)] string Name, DateOnly BirthDate, DateOnly? TrainingStartDate, string? Notes);
public record ChildDto(Guid Id, string Name, DateOnly BirthDate, DateOnly? TrainingStartDate, string? Notes);

public record LogRequest(
    LogType Type, DateTime? OccurredAt, bool? InPotty, int? Amount, string? Notes,
    LogSize? Size = null, int? StoolType = null, OutputColour? Colour = null, string? Category = null, string[]? Tags = null);
public record LogDto(
    Guid Id, Guid ChildId, LogType Type, DateTime OccurredAt, bool? InPotty, int? Amount, string? Notes,
    LogSize? Size, int? StoolType, OutputColour? Colour, string? Category, string[] Tags);

public record DayInsight(DateOnly Date, int Successes, int Accidents, int Sits, int Fluids);
public record DailyBalance(DateOnly Date, int IntakeMl, int Drinks, int Meals, int Urine, int Stools);
public record IntakeOutputDto(double? MedianDrinkToUrineMinutes, int LagSamples, double? AvgUrineIntervalMinutes, double? AvgStoolIntervalHours);
public record InsightNote(string Code, string Message);
public record InsightsDto(
    int Days, int TotalSuccesses, int TotalAccidents, double SuccessRatePercent,
    int CurrentDryStreakHours, double AvgMinutesBetweenSuccesses,
    DateTime? PredictedNextVisit, string? PredictionModel, int[] SuccessesByHour, List<DayInsight> Daily,
    List<DailyBalance> Balance, IntakeOutputDto IntakeOutput, List<InsightNote> Notes);
public record PredictionDto(DateTime NextLikelyAt, double Confidence, string Model, string? Reason);
public record PredictionResponse(PredictionDto? Best, List<PredictionDto> Alternatives);

public record ArticleRequest([Required] string Slug, [Required] string Title, string Summary, string Body, bool IsPremium, bool IsPublished);
public record PlanOfferRequest(Plan Plan, [Required] string Name, decimal MonthlyPrice, decimal YearlyPrice, string Features, bool IsActive);
public record UpdateUserRequest(Plan? Plan, bool? IsActive, UserRole? Role);
public record WaitlistRequest([Required, EmailAddress] string Email, string? Source);
