using Microsoft.EntityFrameworkCore;
using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;
using PlannerPro.Web.Services.Priorities;

namespace PlannerPro.Web.Data;

public class DbInitializer(
    AppDbContext context,
    IPriorityScoringService priorityScoringService,
    ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync()
    {
        await context.Database.MigrateAsync();

        if (!await context.PlannerSettings.AnyAsync())
        {
            context.PlannerSettings.Add(new PlannerSettings());
        }

        var existingTasks = await context.Tasks.ToListAsync();
        if (existingTasks.Count > 0)
        {
            var scoringTime = DateTime.Now;
            foreach (var task in existingTasks)
            {
                priorityScoringService.Apply(task, scoringTime);
            }

            await context.SaveChangesAsync();
            return;
        }

        var now = DateTime.Now;
        var createdAt = DateTime.UtcNow.AddDays(-3);

        var tasks = new List<TaskItem>
        {
            new() { Title = "Подготовка на курсовия проект", Description = "Преглед на изискванията и реализация на MVC приложението.", Category = TaskCategory.Study, Deadline = now.AddDays(2).Date.AddHours(18), EstimatedMinutes = 240, CompletedMinutes = 90, UserImportance = 5, Status = PlannerTaskStatus.InProgress, Priority = TaskPriority.Critical, PriorityScore = 88, PriorityReason = "Близък срок, висока важност и голям оставащ обем.", CreatedAtUtc = createdAt },
            new() { Title = "Упражнения по програмиране", Description = "Решаване на задачите от последното упражнение.", Category = TaskCategory.Study, Deadline = now.AddDays(1).Date.AddHours(20), EstimatedMinutes = 120, UserImportance = 4, Priority = TaskPriority.High, PriorityScore = 72, PriorityReason = "Срокът е утре и важността е 4/5.", CreatedAtUtc = createdAt.AddHours(2) },
            new() { Title = "Преглед на лекционните материали", Category = TaskCategory.Study, Deadline = now.AddDays(6).Date.AddHours(17), EstimatedMinutes = 90, UserImportance = 3, Priority = TaskPriority.Medium, PriorityScore = 46, PriorityReason = "Средна важност и достатъчно време до срока.", CreatedAtUtc = createdAt.AddHours(4) },
            new() { Title = "Изпращане на седмичен отчет", Category = TaskCategory.Work, Deadline = now.AddDays(3).Date.AddHours(16), EstimatedMinutes = 45, UserImportance = 4, Priority = TaskPriority.High, PriorityScore = 62, PriorityReason = "Висока важност и срок до три дни.", CreatedAtUtc = createdAt.AddHours(6) },
            new() { Title = "Планиране на екипна среща", Category = TaskCategory.Work, Deadline = now.AddDays(8).Date.AddHours(11), EstimatedMinutes = 30, UserImportance = 2, Priority = TaskPriority.Low, PriorityScore = 25, PriorityReason = "Ниска важност и далечен срок.", CreatedAtUtc = createdAt.AddHours(8) },
            new() { Title = "Пазаруване за седмицата", Category = TaskCategory.Personal, Deadline = now.AddDays(1).Date.AddHours(19), EstimatedMinutes = 60, UserImportance = 3, Priority = TaskPriority.Medium, PriorityScore = 52, PriorityReason = "Близък срок и средна важност.", CreatedAtUtc = createdAt.AddHours(10) },
            new() { Title = "Тренировка", Category = TaskCategory.Personal, Deadline = now.AddDays(2).Date.AddHours(9), EstimatedMinutes = 75, UserImportance = 3, Priority = TaskPriority.Medium, PriorityScore = 44, PriorityReason = "Средна важност и умерено близък срок.", CreatedAtUtc = createdAt.AddHours(12) },
            new() { Title = "Записване на час при зъболекар", Category = TaskCategory.Personal, Deadline = now.AddDays(12).Date.AddHours(12), EstimatedMinutes = 15, UserImportance = 2, Priority = TaskPriority.Low, PriorityScore = 18, PriorityReason = "Кратка задача с далечен срок.", CreatedAtUtc = createdAt.AddHours(14) },
            new() { Title = "Предаване на домашна работа", Category = TaskCategory.Study, Deadline = now.AddDays(-1).Date.AddHours(23), EstimatedMinutes = 120, CompletedMinutes = 30, UserImportance = 5, Priority = TaskPriority.Critical, PriorityScore = 100, PriorityReason = "Задачата е просрочена и е с максимална важност.", CreatedAtUtc = createdAt.AddHours(16) },
            new() { Title = "Архивиране на учебни файлове", Category = TaskCategory.Other, Deadline = now.AddDays(-2).Date.AddHours(18), EstimatedMinutes = 45, CompletedMinutes = 45, UserImportance = 2, Status = PlannerTaskStatus.Completed, Priority = TaskPriority.Low, PriorityScore = 12, PriorityReason = "Задачата е завършена.", CreatedAtUtc = createdAt.AddHours(18), CompletedAtUtc = DateTime.UtcNow.AddDays(-2) }
        };

        foreach (var task in tasks)
        {
            priorityScoringService.Apply(task, now);
        }

        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {TaskCount} demonstration tasks.", tasks.Count);
    }
}
