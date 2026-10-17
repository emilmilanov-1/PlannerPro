using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlannerPro.Web.Data;
using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;
using PlannerPro.Web.ViewModels;

namespace PlannerPro.Web.Controllers;

public class StatisticsController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var tasks = await context.Tasks.AsNoTracking().ToListAsync();
        var progressTasks = tasks
            .Where(task => task.Status != PlannerTaskStatus.Postponed)
            .ToList();
        var activeTasks = tasks
            .Where(task => task.Status is not PlannerTaskStatus.Completed and not PlannerTaskStatus.Postponed)
            .ToList();
        var completedTasks = tasks.Count(task => task.Status == PlannerTaskStatus.Completed);
        var estimatedMinutes = progressTasks.Sum(task => task.EstimatedMinutes);
        var completedMinutes = progressTasks.Sum(task => Math.Min(task.CompletedMinutes, task.EstimatedMinutes));

        var weekStart = DateOnly.FromDateTime(DateTime.Today);
        var weekEnd = weekStart.AddDays(7);
        var weekStartTime = weekStart.ToDateTime(TimeOnly.MinValue);
        var weekEndTime = weekEnd.ToDateTime(TimeOnly.MinValue);
        var scheduleBlocks = await context.ScheduleBlocks
            .AsNoTracking()
            .Include(block => block.TaskItem)
            .Where(block => block.StartTime >= weekStartTime && block.StartTime < weekEndTime)
            .ToListAsync();

        var viewModel = new StatisticsViewModel
        {
            TotalTasks = tasks.Count,
            CompletedTasks = completedTasks,
            EstimatedMinutes = estimatedMinutes,
            CompletedMinutes = completedMinutes,
            TaskCompletionPercentage = Percentage(completedTasks, tasks.Count),
            TimeCompletionPercentage = Percentage(completedMinutes, estimatedMinutes),
            StatusDistribution = BuildStatusDistribution(tasks),
            PriorityDistribution = BuildPriorityDistribution(activeTasks),
            CategoryProgress = BuildCategoryProgress(progressTasks),
            WeeklyProgress = Enumerable.Range(0, 7)
                .Select(offset => BuildDayProgress(
                    weekStart.AddDays(offset),
                    scheduleBlocks))
                .ToList()
        };

        return View(viewModel);
    }

    private static IReadOnlyList<DistributionItemViewModel> BuildStatusDistribution(
        IReadOnlyCollection<TaskItem> tasks)
    {
        var definitions = new[]
        {
            (PlannerTaskStatus.New, "Нови", "distribution-blue"),
            (PlannerTaskStatus.InProgress, "В процес", "distribution-orange"),
            (PlannerTaskStatus.Completed, "Завършени", "distribution-green"),
            (PlannerTaskStatus.Postponed, "Отложени", "distribution-gray")
        };

        return definitions.Select(definition =>
        {
            var count = tasks.Count(task => task.Status == definition.Item1);
            return new DistributionItemViewModel
            {
                Label = definition.Item2,
                Count = count,
                Percentage = Percentage(count, tasks.Count),
                CssClass = definition.Item3
            };
        }).ToList();
    }

    private static IReadOnlyList<DistributionItemViewModel> BuildPriorityDistribution(
        IReadOnlyCollection<TaskItem> activeTasks)
    {
        var definitions = new[]
        {
            (TaskPriority.Low, "Нисък", "priority-low"),
            (TaskPriority.Medium, "Среден", "priority-medium"),
            (TaskPriority.High, "Висок", "priority-high"),
            (TaskPriority.Critical, "Критичен", "priority-critical")
        };

        return definitions.Select(definition =>
        {
            var count = activeTasks.Count(task => task.Priority == definition.Item1);
            return new DistributionItemViewModel
            {
                Label = definition.Item2,
                Count = count,
                Percentage = Percentage(count, activeTasks.Count),
                CssClass = definition.Item3
            };
        }).ToList();
    }

    private static IReadOnlyList<CategoryProgressViewModel> BuildCategoryProgress(
        IReadOnlyCollection<TaskItem> tasks)
    {
        var labels = new Dictionary<TaskCategory, string>
        {
            [TaskCategory.Study] = "Обучение",
            [TaskCategory.Work] = "Работа",
            [TaskCategory.Personal] = "Лични",
            [TaskCategory.Other] = "Други"
        };

        return Enum.GetValues<TaskCategory>().Select(category =>
        {
            var categoryTasks = tasks.Where(task => task.Category == category).ToList();
            var estimatedMinutes = categoryTasks.Sum(task => task.EstimatedMinutes);
            var completedMinutes = categoryTasks.Sum(task => Math.Min(task.CompletedMinutes, task.EstimatedMinutes));
            return new CategoryProgressViewModel
            {
                Category = category,
                Label = labels[category],
                TotalTasks = categoryTasks.Count,
                CompletedTasks = categoryTasks.Count(task => task.Status == PlannerTaskStatus.Completed),
                EstimatedMinutes = estimatedMinutes,
                CompletedMinutes = completedMinutes,
                CompletionPercentage = Percentage(completedMinutes, estimatedMinutes)
            };
        }).ToList();
    }

    private static WeeklyProgressViewModel BuildDayProgress(
        DateOnly date,
        IReadOnlyCollection<ScheduleBlock> scheduleBlocks)
    {
        var blocks = scheduleBlocks
            .Where(block => DateOnly.FromDateTime(block.StartTime) == date)
            .ToList();

        return new WeeklyProgressViewModel
        {
            Date = date,
            PlannedMinutes = blocks.Sum(block => block.DurationMinutes),
            CompletedMinutes = blocks
                .Where(block => block.IsCompleted || block.TaskItem?.Status == PlannerTaskStatus.Completed)
                .Sum(block => block.DurationMinutes),
            BlockCount = blocks.Count
        };
    }

    private static double Percentage(int value, int total) =>
        total == 0 ? 0 : Math.Round(value * 100d / total, 1);
}
