using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlannerPro.Web.Data;
using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;
using PlannerPro.Web.Services.Holidays;
using PlannerPro.Web.Services.Scheduling;
using PlannerPro.Web.ViewModels;

namespace PlannerPro.Web.Controllers;

public class PlannerController(
    AppDbContext context,
    IHolidayApiClient holidayApiClient,
    IScheduleOptimizerService scheduleOptimizerService) : Controller
{
    public async Task<IActionResult> Index(
        DateOnly? startDate,
        int? planningDays,
        CancellationToken cancellationToken)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        var request = CreateRequest(settings, startDate, planningDays);
        var warnings = ReadWarningsFromTempData();
        var viewModel = await BuildViewModelAsync(request, warnings, cancellationToken);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(
        [Bind(Prefix = "Request")] PlannerRequestViewModel request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildViewModelAsync(request, [], cancellationToken);
            return View(nameof(Index), invalidViewModel);
        }

        var settings = await GetSettingsAsync(cancellationToken);
        ApplyRequest(settings, request);

        var tasks = await context.Tasks
            .Where(task => task.Status != PlannerTaskStatus.Completed &&
                           task.Status != PlannerTaskStatus.Postponed)
            .ToListAsync(cancellationToken);

        var holidayWarnings = new List<string>();
        var holidays = request.SkipPublicHolidays
            ? await GetHolidaysForPeriodAsync(request, holidayWarnings, cancellationToken)
            : [];

        var scheduleStart = GetScheduleStart(request);
        var result = scheduleOptimizerService.Generate(
            tasks,
            settings,
            holidays,
            scheduleStart,
            request.PlanningDays,
            DateTime.UtcNow);

        var existingBlocks = await context.ScheduleBlocks.ToListAsync(cancellationToken);
        context.ScheduleBlocks.RemoveRange(existingBlocks);
        context.ScheduleBlocks.AddRange(result.Blocks);
        await context.SaveChangesAsync(cancellationToken);

        var warnings = holidayWarnings
            .Concat(result.Warnings.Select(warning => warning.Message))
            .ToArray();
        TempData["ScheduleWarnings"] = JsonSerializer.Serialize(warnings);
        TempData["SuccessMessage"] = result.Blocks.Count == 0
            ? "Графикът е обновен, но няма задачи, които могат да бъдат разпределени."
            : $"Графикът е генериран: {result.Blocks.Count} блока за {result.Blocks.Select(block => block.TaskItemId).Distinct().Count()} задачи.";

        return RedirectToAction(nameof(Index), new
        {
            startDate = request.StartDate.ToString("yyyy-MM-dd"),
            planningDays = request.PlanningDays
        });
    }

    private async Task<PlannerOverviewViewModel> BuildViewModelAsync(
        PlannerRequestViewModel request,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        var holidayResult = await holidayApiClient.GetHolidaysAsync(
            "BG",
            request.StartDate.Year,
            cancellationToken);
        var scheduleBlocks = await context.ScheduleBlocks
            .AsNoTracking()
            .Include(block => block.TaskItem)
            .OrderBy(block => block.StartTime)
            .ToListAsync(cancellationToken);
        var scheduleDays = scheduleBlocks
            .GroupBy(block => DateOnly.FromDateTime(block.StartTime))
            .Select(group => new ScheduleDayViewModel
            {
                Date = group.Key,
                Blocks = group.ToList()
            })
            .OrderBy(day => day.Date)
            .ToList();

        return new PlannerOverviewViewModel
        {
            Request = request,
            Year = request.StartDate.Year,
            Holidays = holidayResult.Holidays,
            HolidayApiAvailable = holidayResult.IsAvailable,
            HolidaysFromCache = holidayResult.FromCache,
            HolidayWarning = holidayResult.Warning,
            ScheduleDays = scheduleDays,
            ScheduleWarnings = warnings
        };
    }

    private async Task<PlannerSettings> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await context.PlannerSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new PlannerSettings();
        context.PlannerSettings.Add(settings);
        await context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private async Task<IReadOnlyCollection<DateOnly>> GetHolidaysForPeriodAsync(
        PlannerRequestViewModel request,
        ICollection<string> warnings,
        CancellationToken cancellationToken)
    {
        var endDate = request.StartDate.AddDays(request.PlanningDays - 1);
        var years = Enumerable.Range(request.StartDate.Year, endDate.Year - request.StartDate.Year + 1);
        var holidays = new List<DateOnly>();

        foreach (var year in years)
        {
            var result = await holidayApiClient.GetHolidaysAsync("BG", year, cancellationToken);
            holidays.AddRange(result.Holidays.Select(holiday => holiday.Date));
            if (!string.IsNullOrWhiteSpace(result.Warning))
            {
                warnings.Add(result.Warning);
            }
        }

        return holidays;
    }

    private static PlannerRequestViewModel CreateRequest(
        PlannerSettings settings,
        DateOnly? startDate,
        int? planningDays) => new()
        {
            StartDate = startDate ?? DateOnly.FromDateTime(DateTime.Today),
            PlanningDays = planningDays is >= 1 and <= 14 ? planningDays.Value : 7,
            DayStart = settings.DayStart,
            DayEnd = settings.DayEnd,
            DailyAvailableMinutes = settings.DailyAvailableMinutes,
            MaxBlockMinutes = settings.MaxBlockMinutes,
            BreakMinutes = settings.BreakMinutes,
            IncludeWeekends = settings.IncludeWeekends,
            SkipPublicHolidays = settings.SkipPublicHolidays
        };

    private static void ApplyRequest(PlannerSettings settings, PlannerRequestViewModel request)
    {
        settings.DayStart = request.DayStart;
        settings.DayEnd = request.DayEnd;
        settings.DailyAvailableMinutes = request.DailyAvailableMinutes;
        settings.MaxBlockMinutes = request.MaxBlockMinutes;
        settings.BreakMinutes = request.BreakMinutes;
        settings.IncludeWeekends = request.IncludeWeekends;
        settings.SkipPublicHolidays = request.SkipPublicHolidays;
        settings.CountryCode = "BG";
    }

    private static DateTime GetScheduleStart(PlannerRequestViewModel request)
    {
        var start = request.StartDate.ToDateTime(request.DayStart);
        if (request.StartDate != DateOnly.FromDateTime(DateTime.Today) || DateTime.Now <= start)
        {
            return start;
        }

        var now = DateTime.Now;
        var minutesToNextQuarter = (15 - now.Minute % 15) % 15;
        return now.AddMinutes(minutesToNextQuarter).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
    }

    private IReadOnlyList<string> ReadWarningsFromTempData()
    {
        if (TempData["ScheduleWarnings"] is not string serializedWarnings)
        {
            return [];
        }

        return JsonSerializer.Deserialize<string[]>(serializedWarnings) ?? [];
    }
}
