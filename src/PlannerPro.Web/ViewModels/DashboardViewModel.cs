using PlannerPro.Web.Models;

namespace PlannerPro.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalTasks { get; init; }
    public int ActiveTasks { get; init; }
    public int TodayTasks { get; init; }
    public int OverdueTasks { get; init; }
    public int HighPriorityTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int EstimatedMinutes { get; init; }
    public int CompletedMinutes { get; init; }
    public double CompletionPercentage { get; init; }
    public double TimeCompletionPercentage { get; init; }
    public TaskItem? NextTask { get; init; }
    public ScheduleBlock? NextScheduleBlock { get; init; }
    public IReadOnlyList<TaskItem> UpcomingDeadlines { get; init; } = [];
}
