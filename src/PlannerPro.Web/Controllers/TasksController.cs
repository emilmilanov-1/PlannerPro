using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlannerPro.Web.Data;
using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;
using PlannerPro.Web.ViewModels;
using PlannerPro.Web.Services.Priorities;

namespace PlannerPro.Web.Controllers;

public class TasksController(AppDbContext context, IPriorityScoringService priorityScoringService) : Controller
{
    public async Task<IActionResult> Index(
        string? search,
        PlannerTaskStatus? status,
        TaskCategory? category,
        TaskPriority? priority)
    {
        var query = context.Tasks.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.Like(item.Title, pattern) ||
                (item.Description != null && EF.Functions.Like(item.Description, pattern)));
        }

        if (status.HasValue)
        {
            query = query.Where(item => item.Status == status.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(item => item.Category == category.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(item => item.Priority == priority.Value);
        }

        var viewModel = new TaskListViewModel
        {
            Tasks = await query
                .OrderBy(item => item.Status == PlannerTaskStatus.Completed)
                .ThenByDescending(item => item.PriorityScore)
                .ThenBy(item => item.Deadline)
                .ToListAsync(),
            Search = search,
            Status = status,
            Category = category,
            Priority = priority
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var taskItem = await context.Tasks.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return taskItem is null ? NotFound() : View(taskItem);
    }

    public IActionResult Create()
    {
        return View(new TaskItem
        {
            Deadline = DateTime.Today.AddDays(1).AddHours(18),
            EstimatedMinutes = 60,
            UserImportance = 3
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Title,Description,Category,Deadline,EstimatedMinutes,CompletedMinutes,UserImportance,Status")] TaskItem taskItem)
    {
        if (taskItem.Deadline <= DateTime.Now)
        {
            ModelState.AddModelError(nameof(TaskItem.Deadline), "При нова задача крайният срок трябва да е в бъдещето.");
        }

        if (!ModelState.IsValid)
        {
            return View(taskItem);
        }

        taskItem.CreatedAtUtc = DateTime.UtcNow;
        taskItem.CompletedAtUtc = taskItem.Status == PlannerTaskStatus.Completed ? DateTime.UtcNow : null;
        priorityScoringService.Apply(taskItem, DateTime.Now);

        context.Add(taskItem);
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Задачата беше създадена успешно.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var taskItem = await context.Tasks.FindAsync(id);
        return taskItem is null ? NotFound() : View(taskItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Title,Description,Category,Deadline,EstimatedMinutes,CompletedMinutes,UserImportance,Status")] TaskItem input)
    {
        if (id != input.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var taskItem = await context.Tasks.FirstOrDefaultAsync(item => item.Id == id);
        if (taskItem is null)
        {
            return NotFound();
        }

        taskItem.Title = input.Title;
        taskItem.Description = input.Description;
        taskItem.Category = input.Category;
        taskItem.Deadline = input.Deadline;
        taskItem.EstimatedMinutes = input.EstimatedMinutes;
        taskItem.CompletedMinutes = input.CompletedMinutes;
        taskItem.UserImportance = input.UserImportance;
        taskItem.Status = input.Status;
        taskItem.UpdatedAtUtc = DateTime.UtcNow;

        if (input.Status == PlannerTaskStatus.Completed)
        {
            taskItem.CompletedAtUtc ??= DateTime.UtcNow;
            taskItem.CompletedMinutes = taskItem.EstimatedMinutes;
        }
        else
        {
            taskItem.CompletedAtUtc = null;
        }

        var scheduleBlocks = await context.ScheduleBlocks
            .Where(block => block.TaskItemId == id)
            .ToListAsync();
        foreach (var block in scheduleBlocks)
        {
            block.IsCompleted = input.Status == PlannerTaskStatus.Completed;
        }

        priorityScoringService.Apply(taskItem, DateTime.Now);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await TaskExistsAsync(id))
            {
                return NotFound();
            }

            throw;
        }

        TempData["SuccessMessage"] = "Промените бяха запазени.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var taskItem = await context.Tasks.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return taskItem is null ? NotFound() : View(taskItem);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var taskItem = await context.Tasks.FindAsync(id);
        if (taskItem is not null)
        {
            context.Tasks.Remove(taskItem);
            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Задачата беше изтрита.";
        }
        else
        {
            TempData["ErrorMessage"] = "Задачата не беше намерена или вече е изтрита.";
        }

        return RedirectToAction(nameof(Index));
    }

    private Task<bool> TaskExistsAsync(int id) => context.Tasks.AnyAsync(item => item.Id == id);
}
