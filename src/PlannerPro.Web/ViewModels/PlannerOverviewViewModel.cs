using PlannerPro.Web.Services.Holidays;

namespace PlannerPro.Web.ViewModels;

public sealed class PlannerOverviewViewModel
{
    public PlannerRequestViewModel Request { get; init; } = new();

    public int Year { get; init; }

    public IReadOnlyList<HolidayDto> Holidays { get; init; } = [];

    public bool HolidayApiAvailable { get; init; }

    public bool HolidaysFromCache { get; init; }

    public string? HolidayWarning { get; init; }

    public IReadOnlyList<ScheduleDayViewModel> ScheduleDays { get; init; } = [];

    public IReadOnlyList<string> ScheduleWarnings { get; init; } = [];

    public int TotalScheduledMinutes => ScheduleDays.Sum(day => day.PlannedMinutes);

    public int ScheduledTaskCount => ScheduleDays
        .SelectMany(day => day.Blocks)
        .Select(block => block.TaskItemId)
        .Distinct()
        .Count();

    public bool HasSchedule => ScheduleDays.Any(day => day.Blocks.Count > 0);
}
