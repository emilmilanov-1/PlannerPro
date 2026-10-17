using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;
using PlannerPro.Web.Services.Scheduling;

namespace PlannerPro.Tests;

public class ScheduleOptimizerServiceTests
{
    private static readonly DateTime MondayAtNine = new(2026, 9, 28, 9, 0, 0);
    private static readonly DateTime GeneratedAtUtc = new(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);
    private readonly ScheduleOptimizerService _service = new();

    [Fact]
    public void Generate_ReturnsEmptyScheduleForEmptyTaskList()
    {
        var result = _service.Generate([], CreateSettings(), [], MondayAtNine, 7, GeneratedAtUtc);

        Assert.Empty(result.Blocks);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Generate_ExcludesCompletedAndPostponedTasks()
    {
        var tasks = new[]
        {
            CreateTask(1, PlannerTaskStatus.Completed),
            CreateTask(2, PlannerTaskStatus.Postponed)
        };

        var result = _service.Generate(tasks, CreateSettings(), [], MondayAtNine, 7, GeneratedAtUtc);

        Assert.Empty(result.Blocks);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Generate_SplitsLargeTaskIntoMaximumSizedBlocks()
    {
        var task = CreateTask(1, estimatedMinutes: 200);
        var settings = CreateSettings(maxBlockMinutes: 90, dailyAvailableMinutes: 240);

        var result = _service.Generate([task], settings, [], MondayAtNine, 1, GeneratedAtUtc);

        Assert.Equal([90, 90, 20], result.Blocks.Select(block => block.DurationMinutes));
        Assert.All(result.Blocks, block => Assert.True(block.DurationMinutes <= 90));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Generate_CreatesNonOverlappingBlocksWithBreaks()
    {
        var tasks = new[]
        {
            CreateTask(1, estimatedMinutes: 30, priorityScore: 80),
            CreateTask(2, estimatedMinutes: 30, priorityScore: 70)
        };
        var settings = CreateSettings(breakMinutes: 15);

        var result = _service.Generate(tasks, settings, [], MondayAtNine, 1, GeneratedAtUtc);

        Assert.Equal(2, result.Blocks.Count);
        Assert.Equal(MondayAtNine.AddMinutes(30), result.Blocks[0].EndTime);
        Assert.Equal(MondayAtNine.AddMinutes(45), result.Blocks[1].StartTime);
        Assert.True(result.Blocks[0].EndTime <= result.Blocks[1].StartTime);
    }

    [Fact]
    public void Generate_SkipsWeekendWhenDisabled()
    {
        var saturday = new DateTime(2026, 10, 3, 9, 0, 0);
        var task = CreateTask(1, deadline: saturday.AddDays(4));
        var settings = CreateSettings(includeWeekends: false);

        var result = _service.Generate([task], settings, [], saturday, 3, GeneratedAtUtc);

        var block = Assert.Single(result.Blocks);
        Assert.Equal(DayOfWeek.Monday, block.StartTime.DayOfWeek);
    }

    [Fact]
    public void Generate_SkipsPublicHolidayWhenEnabled()
    {
        var task = CreateTask(1);
        var settings = CreateSettings(skipPublicHolidays: true);
        var holiday = DateOnly.FromDateTime(MondayAtNine);

        var result = _service.Generate([task], settings, [holiday], MondayAtNine, 2, GeneratedAtUtc);

        var block = Assert.Single(result.Blocks);
        Assert.Equal(holiday.AddDays(1), DateOnly.FromDateTime(block.StartTime));
    }

    [Fact]
    public void Generate_UsesHolidayWhenSkippingIsDisabled()
    {
        var task = CreateTask(1);
        var settings = CreateSettings(skipPublicHolidays: false);
        var holiday = DateOnly.FromDateTime(MondayAtNine);

        var result = _service.Generate([task], settings, [holiday], MondayAtNine, 1, GeneratedAtUtc);

        var block = Assert.Single(result.Blocks);
        Assert.Equal(holiday, DateOnly.FromDateTime(block.StartTime));
    }

    [Fact]
    public void Generate_DoesNotPlaceBlockAfterDeadline()
    {
        var deadline = MondayAtNine.AddHours(1);
        var task = CreateTask(1, estimatedMinutes: 60, deadline: deadline);

        var result = _service.Generate([task], CreateSettings(), [], MondayAtNine, 1, GeneratedAtUtc);

        var block = Assert.Single(result.Blocks);
        Assert.True(block.EndTime <= deadline);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Generate_ReturnsWarningWhenTaskDoesNotFit()
    {
        var task = CreateTask(1, estimatedMinutes: 300);
        var settings = CreateSettings(dailyAvailableMinutes: 120);

        var result = _service.Generate([task], settings, [], MondayAtNine, 1, GeneratedAtUtc);

        Assert.Equal(120, result.Blocks.Sum(block => block.DurationMinutes));
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(180, warning.UnscheduledMinutes);
        Assert.Contains("не може да бъде вместена", warning.Message);
    }

    [Fact]
    public void Generate_PrioritizesHigherScoreWhenTimeIsLimited()
    {
        var low = CreateTask(1, estimatedMinutes: 60, priorityScore: 20, title: "Нисък приоритет");
        var high = CreateTask(2, estimatedMinutes: 60, priorityScore: 90, title: "Критичен приоритет");
        var settings = CreateSettings(dailyAvailableMinutes: 60);

        var result = _service.Generate([low, high], settings, [], MondayAtNine, 1, GeneratedAtUtc);

        var block = Assert.Single(result.Blocks);
        Assert.Equal(high.Id, block.TaskItemId);
        Assert.Contains(result.Warnings, warning => warning.TaskItemId == low.Id);
    }

    [Fact]
    public void Generate_UsesOnlyRemainingTaskMinutes()
    {
        var task = CreateTask(1, estimatedMinutes: 120);
        task.CompletedMinutes = 45;

        var result = _service.Generate([task], CreateSettings(), [], MondayAtNine, 1, GeneratedAtUtc);

        Assert.Equal(75, result.Blocks.Sum(block => block.DurationMinutes));
    }

    [Fact]
    public void Generate_ReportsOverdueTaskInsteadOfThrowing()
    {
        var task = CreateTask(1, deadline: MondayAtNine.AddMinutes(-1));

        var result = _service.Generate([task], CreateSettings(), [], MondayAtNine, 7, GeneratedAtUtc);

        Assert.Empty(result.Blocks);
        Assert.Contains("изтекъл", Assert.Single(result.Warnings).Message);
    }

    private static PlannerSettings CreateSettings(
        int dailyAvailableMinutes = 240,
        int maxBlockMinutes = 90,
        int breakMinutes = 15,
        bool includeWeekends = true,
        bool skipPublicHolidays = true) => new()
        {
            DayStart = new TimeOnly(9, 0),
            DayEnd = new TimeOnly(18, 0),
            DailyAvailableMinutes = dailyAvailableMinutes,
            MaxBlockMinutes = maxBlockMinutes,
            BreakMinutes = breakMinutes,
            IncludeWeekends = includeWeekends,
            SkipPublicHolidays = skipPublicHolidays
        };

    private static TaskItem CreateTask(
        int id,
        PlannerTaskStatus status = PlannerTaskStatus.New,
        int estimatedMinutes = 60,
        int priorityScore = 50,
        DateTime? deadline = null,
        string? title = null) => new()
        {
            Id = id,
            Title = title ?? $"Задача {id}",
            Status = status,
            EstimatedMinutes = estimatedMinutes,
            PriorityScore = priorityScore,
            Deadline = deadline ?? MondayAtNine.AddDays(7)
        };
}
