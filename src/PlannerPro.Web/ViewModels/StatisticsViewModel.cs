using PlannerPro.Web.Models.Enums;

namespace PlannerPro.Web.ViewModels;

public sealed class StatisticsViewModel
{
    public int TotalTasks { get; init; }

    public int CompletedTasks { get; init; }

    public int EstimatedMinutes { get; init; }

    public int CompletedMinutes { get; init; }

    public double TaskCompletionPercentage { get; init; }

    public double TimeCompletionPercentage { get; init; }

    public IReadOnlyList<DistributionItemViewModel> StatusDistribution { get; init; } = [];

    public IReadOnlyList<DistributionItemViewModel> PriorityDistribution { get; init; } = [];

    public IReadOnlyList<CategoryProgressViewModel> CategoryProgress { get; init; } = [];

    public IReadOnlyList<WeeklyProgressViewModel> WeeklyProgress { get; init; } = [];
}

public sealed class DistributionItemViewModel
{
    public string Label { get; init; } = string.Empty;

    public int Count { get; init; }

    public double Percentage { get; init; }

    public string CssClass { get; init; } = string.Empty;
}

public sealed class CategoryProgressViewModel
{
    public TaskCategory Category { get; init; }

    public string Label { get; init; } = string.Empty;

    public int TotalTasks { get; init; }

    public int CompletedTasks { get; init; }

    public int EstimatedMinutes { get; init; }

    public int CompletedMinutes { get; init; }

    public double CompletionPercentage { get; init; }
}

public sealed class WeeklyProgressViewModel
{
    public DateOnly Date { get; init; }

    public int PlannedMinutes { get; init; }

    public int CompletedMinutes { get; init; }

    public int BlockCount { get; init; }
}
