namespace PlannerPro.Web.Services.Holidays;

public sealed record HolidayFetchResult(
    IReadOnlyList<HolidayDto> Holidays,
    bool IsAvailable,
    bool FromCache,
    string? Warning = null);
