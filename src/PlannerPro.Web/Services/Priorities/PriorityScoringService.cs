using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;

namespace PlannerPro.Web.Services.Priorities;

public sealed class PriorityScoringService : IPriorityScoringService
{
    private static readonly string[] ImportantKeywords =
    [
        "спешно", "изпит", "защита", "краен срок", "предаване", "курсов проект",
        "важно", "urgent", "exam", "deadline"
    ];

    public PriorityScoringResult Calculate(TaskItem task, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(task);

        var deadlineCategory = ClassifyDeadline(task.Deadline, now);

        if (task.Status == PlannerTaskStatus.Completed)
        {
            return new PriorityScoringResult(
                0,
                TaskPriority.Low,
                deadlineCategory,
                "Нисък приоритет — 0/100. Задачата е завършена и не изисква активно планиране.");
        }

        var reasons = new List<string>();
        var score = AddUrgencyScore(task.Deadline, now, reasons);

        var importance = Math.Clamp(task.UserImportance, 1, 5);
        var importanceScore = importance * 5;
        score += importanceScore;
        reasons.Add($"важност {importance}/5 (+{importanceScore} т.)");

        var matchedKeywords = FindKeywords(task);
        if (matchedKeywords.Count > 0)
        {
            var keywordScore = Math.Min(15, matchedKeywords.Count * 5);
            score += keywordScore;
            reasons.Add($"ключови думи „{string.Join("“, „", matchedKeywords)}“ (+{keywordScore} т.)");
        }

        var workloadScore = CalculateWorkloadScore(task, now);
        if (workloadScore > 0)
        {
            score += workloadScore;
            var remainingMinutes = Math.Max(0, task.EstimatedMinutes - task.CompletedMinutes);
            reasons.Add($"остават {FormatDuration(remainingMinutes)} преди близък срок (+{workloadScore} т.)");
        }

        if (task.Status == PlannerTaskStatus.InProgress)
        {
            score += 5;
            reasons.Add("задачата вече е започната (+5 т.)");
        }

        score = Math.Clamp(score, 0, 100);
        var priority = MapPriority(score);
        var reason = $"{GetPriorityLabel(priority)} приоритет — {score}/100. Причини: {string.Join("; ", reasons)}.";

        return new PriorityScoringResult(score, priority, deadlineCategory, reason);
    }

    public void Apply(TaskItem task, DateTime now)
    {
        var result = Calculate(task, now);
        task.PriorityScore = result.Score;
        task.Priority = result.Priority;
        task.DeadlineCategory = result.DeadlineCategory;
        task.PriorityReason = result.Reason;
    }

    public static TaskPriority MapPriority(int score) => Math.Clamp(score, 0, 100) switch
    {
        <= 29 => TaskPriority.Low,
        <= 54 => TaskPriority.Medium,
        <= 79 => TaskPriority.High,
        _ => TaskPriority.Critical
    };

    public static DeadlineCategory ClassifyDeadline(DateTime deadline, DateTime now)
    {
        if (deadline < now)
        {
            return DeadlineCategory.Overdue;
        }

        var daysUntilDeadline = (deadline.Date - now.Date).Days;
        return daysUntilDeadline switch
        {
            0 => DeadlineCategory.Today,
            <= 3 => DeadlineCategory.Soon,
            <= 7 => DeadlineCategory.Upcoming,
            _ => DeadlineCategory.Planned
        };
    }

    private static int AddUrgencyScore(DateTime deadline, DateTime now, ICollection<string> reasons)
    {
        if (deadline < now)
        {
            reasons.Add("срокът е изтекъл (+45 т.)");
            return 45;
        }

        var daysUntilDeadline = (deadline.Date - now.Date).Days;
        if (daysUntilDeadline == 0)
        {
            reasons.Add("срокът е днес (+42 т.)");
            return 42;
        }

        if (daysUntilDeadline <= 2)
        {
            reasons.Add(daysUntilDeadline == 1
                ? "срокът е утре (+36 т.)"
                : "срокът е след 2 дни (+36 т.)");
            return 36;
        }

        if (daysUntilDeadline <= 7)
        {
            reasons.Add($"срокът е след {daysUntilDeadline} дни (+24 т.)");
            return 24;
        }

        reasons.Add($"срокът е след {daysUntilDeadline} дни (+10 т.)");
        return 10;
    }

    private static List<string> FindKeywords(TaskItem task)
    {
        var text = $"{task.Title} {task.Description}";
        return ImportantKeywords
            .Where(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();
    }

    private static int CalculateWorkloadScore(TaskItem task, DateTime now)
    {
        var remainingMinutes = Math.Max(0, task.EstimatedMinutes - task.CompletedMinutes);
        var daysUntilDeadline = (task.Deadline.Date - now.Date).Days;

        if (remainingMinutes >= 240 && daysUntilDeadline <= 7)
        {
            return 10;
        }

        if (remainingMinutes >= 120 && daysUntilDeadline <= 3)
        {
            return 8;
        }

        return remainingMinutes >= 120 && daysUntilDeadline <= 7 ? 5 : 0;
    }

    private static string FormatDuration(int minutes)
    {
        if (minutes < 60)
        {
            return $"{minutes} минути";
        }

        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;
        return remainingMinutes == 0
            ? $"{hours} ч."
            : $"{hours} ч. и {remainingMinutes} мин.";
    }

    private static string GetPriorityLabel(TaskPriority priority) => priority switch
    {
        TaskPriority.Low => "Нисък",
        TaskPriority.Medium => "Среден",
        TaskPriority.High => "Висок",
        TaskPriority.Critical => "Критичен",
        _ => priority.ToString()
    };
}
