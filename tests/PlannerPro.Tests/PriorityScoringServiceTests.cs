using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;
using PlannerPro.Web.Services.Priorities;

namespace PlannerPro.Tests;

public class PriorityScoringServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 24, 10, 0, 0);
    private readonly PriorityScoringService _service = new();

    [Fact]
    public void OverdueTask_GetsMorePointsThanDistantTask()
    {
        var overdue = CreateTask(deadline: Now.AddHours(-1));
        var distant = CreateTask(deadline: Now.AddDays(20));

        var overdueResult = _service.Calculate(overdue, Now);
        var distantResult = _service.Calculate(distant, Now);

        Assert.True(overdueResult.Score > distantResult.Score);
        Assert.Equal(DeadlineCategory.Overdue, overdueResult.DeadlineCategory);
        Assert.Equal(DeadlineCategory.Planned, distantResult.DeadlineCategory);
    }

    [Fact]
    public void HigherImportance_IncreasesScore()
    {
        var lowImportance = CreateTask(importance: 1);
        var highImportance = CreateTask(importance: 5);

        var lowResult = _service.Calculate(lowImportance, Now);
        var highResult = _service.Calculate(highImportance, Now);

        Assert.Equal(20, highResult.Score - lowResult.Score);
    }

    [Fact]
    public void KeywordMatching_IsCaseInsensitive()
    {
        var ordinary = CreateTask(title: "Преглед на материали");
        var important = CreateTask(title: "СПЕШНО: подготовка за ИЗПИТ");

        var ordinaryResult = _service.Calculate(ordinary, Now);
        var importantResult = _service.Calculate(important, Now);

        Assert.Equal(10, importantResult.Score - ordinaryResult.Score);
        Assert.Contains("спешно", importantResult.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("изпит", importantResult.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, TaskPriority.Low)]
    [InlineData(29, TaskPriority.Low)]
    [InlineData(30, TaskPriority.Medium)]
    [InlineData(54, TaskPriority.Medium)]
    [InlineData(55, TaskPriority.High)]
    [InlineData(79, TaskPriority.High)]
    [InlineData(80, TaskPriority.Critical)]
    [InlineData(100, TaskPriority.Critical)]
    public void PriorityBoundaries_AreMappedCorrectly(int score, TaskPriority expected)
    {
        Assert.Equal(expected, PriorityScoringService.MapPriority(score));
    }

    [Theory]
    [InlineData(-1, TaskPriority.Low)]
    [InlineData(101, TaskPriority.Critical)]
    public void PriorityMapping_ClampsValuesOutsideRange(int score, TaskPriority expected)
    {
        Assert.Equal(expected, PriorityScoringService.MapPriority(score));
    }

    [Theory]
    [InlineData(-1, DeadlineCategory.Overdue)]
    [InlineData(0, DeadlineCategory.Today)]
    [InlineData(1, DeadlineCategory.Soon)]
    [InlineData(3, DeadlineCategory.Soon)]
    [InlineData(4, DeadlineCategory.Upcoming)]
    [InlineData(7, DeadlineCategory.Upcoming)]
    [InlineData(8, DeadlineCategory.Planned)]
    public void DeadlineClassification_UsesExpectedGroups(int dayOffset, DeadlineCategory expected)
    {
        var deadline = dayOffset < 0 ? Now.AddMinutes(-1) : Now.Date.AddDays(dayOffset).AddHours(18);

        Assert.Equal(expected, PriorityScoringService.ClassifyDeadline(deadline, Now));
    }

    [Fact]
    public void Score_IsAlwaysBetweenZeroAndOneHundred()
    {
        var task = CreateTask(
            title: "Спешно: изпит и краен срок",
            deadline: Now.AddDays(-2),
            importance: 5,
            estimatedMinutes: 1440,
            status: PlannerTaskStatus.InProgress);

        var result = _service.Calculate(task, Now);

        Assert.InRange(result.Score, 0, 100);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void CompletedTask_HasNoActivePriority()
    {
        var task = CreateTask(
            title: "Спешно: изпит",
            deadline: Now.AddHours(-2),
            importance: 5,
            status: PlannerTaskStatus.Completed);

        var result = _service.Calculate(task, Now);

        Assert.Equal(0, result.Score);
        Assert.Equal(TaskPriority.Low, result.Priority);
        Assert.Contains("завършена", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_UpdatesAllCalculatedFields()
    {
        var task = CreateTask(title: "Спешно предаване", deadline: Now.AddDays(1), importance: 5);

        _service.Apply(task, Now);

        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(DeadlineCategory.Soon, task.DeadlineCategory);
        Assert.Equal(71, task.PriorityScore);
        Assert.Contains("71/100", task.PriorityReason);
    }

    private static TaskItem CreateTask(
        string title = "Обикновена задача",
        DateTime? deadline = null,
        int importance = 3,
        int estimatedMinutes = 60,
        PlannerTaskStatus status = PlannerTaskStatus.New) => new()
        {
            Title = title,
            Deadline = deadline ?? Now.AddDays(10),
            UserImportance = importance,
            EstimatedMinutes = estimatedMinutes,
            Status = status
        };
}
