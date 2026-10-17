using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using PlannerPro.Web.Data;
using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;
using PlannerPro.Web.ViewModels;

namespace PlannerPro.Web.Controllers;

public class HomeController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var tasks = await context.Tasks.AsNoTracking().ToListAsync();
        var activeTasks = tasks
            .Where(item => item.Status is not PlannerTaskStatus.Completed and not PlannerTaskStatus.Postponed)
            .ToList();
        var progressTasks = tasks
            .Where(item => item.Status != PlannerTaskStatus.Postponed)
            .ToList();
        var completedTasks = tasks.Count(item => item.Status == PlannerTaskStatus.Completed);
        var estimatedMinutes = progressTasks.Sum(item => item.EstimatedMinutes);
        var completedMinutes = progressTasks.Sum(item => Math.Min(item.CompletedMinutes, item.EstimatedMinutes));
        var nextTask = activeTasks
            .OrderByDescending(item => item.PriorityScore)
            .ThenBy(item => item.Deadline)
            .FirstOrDefault();
        var nextScheduleBlock = await context.ScheduleBlocks
            .AsNoTracking()
            .Include(block => block.TaskItem)
            .Where(block => block.EndTime > now &&
                            !block.IsCompleted &&
                            block.TaskItem != null &&
                            block.TaskItem.Status != PlannerTaskStatus.Completed &&
                            block.TaskItem.Status != PlannerTaskStatus.Postponed)
            .OrderBy(block => block.StartTime)
            .FirstOrDefaultAsync();

        var viewModel = new DashboardViewModel
        {
            TotalTasks = tasks.Count,
            ActiveTasks = activeTasks.Count,
            TodayTasks = activeTasks.Count(item => DateOnly.FromDateTime(item.Deadline) == today),
            OverdueTasks = activeTasks.Count(item => item.Deadline < now),
            CompletedTasks = completedTasks,
            HighPriorityTasks = activeTasks.Count(item => item.PriorityScore >= 55),
            EstimatedMinutes = estimatedMinutes,
            CompletedMinutes = completedMinutes,
            CompletionPercentage = tasks.Count == 0 ? 0 : Math.Round(completedTasks * 100d / tasks.Count, 1),
            TimeCompletionPercentage = estimatedMinutes == 0 ? 0 : Math.Round(completedMinutes * 100d / estimatedMinutes, 1),
            NextTask = nextTask,
            NextScheduleBlock = nextScheduleBlock,
            UpcomingDeadlines = activeTasks
                .Where(item => item.Deadline >= now)
                .OrderBy(item => item.Deadline)
                .Take(5)
                .ToList()
        };

        return View(viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
