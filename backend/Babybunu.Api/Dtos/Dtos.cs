using System.ComponentModel.DataAnnotations;
using Babybunu.Api.Domain;

namespace Babybunu.Api.Dtos;

public record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(8)] string Password, [Required] string DisplayName,
    [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$")] string? Language = null);
public record LoginRequest([Required] string Email, [Required] string Password);
public record AuthResponse(string Token, DateTime ExpiresAt, string RefreshToken, UserDto User);
public record RefreshRequest([Required] string RefreshToken);
public record VerifyEmailRequest([Required] string Token);
public record DeleteAccountRequest([Required] string Password);
public record ForgotPasswordRequest([Required, EmailAddress] string Email);
public record ResetPasswordRequest([Required] string Token, [Required, MinLength(8)] string NewPassword);
public record InviteRequest(bool CanEdit = true);
public record InviteResponse(string Code, DateTime ExpiresAt, bool CanEdit);
public record AcceptInviteRequest([Required] string Code);
public record CaregiverDto(Guid UserId, string DisplayName, string Email, bool CanEdit);
public record UserDto(Guid Id, string Email, string DisplayName, UserRole Role, Plan Plan, DateTime? PlanExpiresAt, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt, string Language = "en", bool EmailVerified = false);

public record ChildRequest([Required, MaxLength(100)] string Name, DateOnly BirthDate, DateOnly? TrainingStartDate, string? Notes, string? TimeZoneId = null);
public record ChildDto(Guid Id, string Name, DateOnly BirthDate, DateOnly? TrainingStartDate, string? Notes, string TimeZoneId, bool IsOwner, bool CanEdit);

public record LogRequest(
    LogType Type, DateTime? OccurredAt, bool? InPotty, int? Amount, string? Notes,
    LogSize? Size = null, int? StoolType = null, OutputColour? Colour = null, string? Category = null, string[]? Tags = null,
    /// <summary>Optional client-generated id. Re-sending the same id (e.g. an offline retry) returns the existing log instead of creating a duplicate.</summary>
    Guid? Id = null);
public record LogDto(
    Guid Id, Guid ChildId, LogType Type, DateTime OccurredAt, bool? InPotty, int? Amount, string? Notes,
    LogSize? Size, int? StoolType, OutputColour? Colour, string? Category, string[] Tags, string? LoggedBy = null);

public record DayInsight(DateOnly Date, int Successes, int Accidents, int Sits, int Fluids);
public record DailyBalance(DateOnly Date, int IntakeMl, int Drinks, int Meals, int Urine, int Stools);
public record IntakeOutputDto(double? MedianDrinkToUrineMinutes, int LagSamples, double? AvgUrineIntervalMinutes, double? AvgStoolIntervalHours);
public record InsightNote(string Code, string Message);
public record InsightsDto(
    int Days, int TotalSuccesses, int TotalAccidents, double SuccessRatePercent,
    int CurrentDryStreakHours, double AvgMinutesBetweenSuccesses,
    DateTime? PredictedNextVisit, string? PredictionModel, int[] SuccessesByHour, List<DayInsight> Daily,
    List<DailyBalance> Balance, IntakeOutputDto IntakeOutput, List<InsightNote> Notes, List<CorrelationDto> Correlations);
public record CorrelationDto(string Factor, string Message, double ExposedAverage, double OtherAverage, int ExposedCount, int OtherCount);
public record PredictionDto(DateTime NextLikelyAt, double Confidence, string Model, string? Reason);
public record PredictionResponse(PredictionDto? Best, List<PredictionDto> Alternatives);

public record ArticleRequest([Required, RegularExpression("^[a-z0-9-]{1,80}$")] string Slug, [Required] string Title, string Summary, string Body, bool IsPremium, bool IsPublished,
    [MaxLength(120)] string? MetaTitle = null, [MaxLength(320)] string? MetaDescription = null, [MaxLength(80)] string? FocusKeyword = null,
    [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$")] string? Language = null);
public record PlanOfferRequest(Plan Plan, [Required] string Name, decimal MonthlyPrice, decimal YearlyPrice, string Features, bool IsActive);
public record UpdateUserRequest(Plan? Plan, bool? IsActive, UserRole? Role);
public record WaitlistRequest([Required, EmailAddress] string Email, [MaxLength(200)] string? Source);

public record DonationCheckoutRequest(long AmountCents);
public record DonationConfigDto(bool Enabled, string Currency, int[] PresetsCents, int MinCents, int MaxCents);

public record ReportLogRow(string LocalTime, string Type, string Details, string? LoggedBy);
public record ReportDto(string ChildName, int? AgeMonths, string TimeZone, DateOnly From, DateOnly To, DateTime GeneratedAt,
    InsightsDto Insights, int[] StoolTypeCounts, List<ReportLogRow>? Logs, string Disclaimer);
public record ReportLinkRequest(DateOnly From, DateOnly To, bool IncludeLogs = false, int ExpiresInDays = 7);
public record ReportLinkDto(Guid Id, string Token, string Path, DateOnly From, DateOnly To, bool IncludeLogs, DateTime ExpiresAt);
public record ReportLinkSummary(Guid Id, DateOnly From, DateOnly To, bool IncludeLogs, DateTime ExpiresAt, DateTime CreatedAt);
