using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PlannerPro.Web.Data;
using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;
using PlannerPro.Web.Services.Priorities;

namespace PlannerPro.Tests;

public class DbInitializerTests
{
    [Fact]
    public async Task InitializeAsync_SeedsAndScoresEmptyDatabase()
    {
        await using var context = await CreateContextAsync();
        var initializer = CreateInitializer(context);

        await initializer.InitializeAsync();

        var tasks = await context.Tasks.AsNoTracking().ToListAsync();
        Assert.Equal(10, tasks.Count);
        Assert.Single(await context.PlannerSettings.AsNoTracking().ToListAsync());
        Assert.All(tasks, task => Assert.DoesNotContain("Milestone 3", task.PriorityReason));
        Assert.Contains(tasks, task => task.DeadlineCategory == DeadlineCategory.Overdue);
    }

    [Fact]
    public async Task InitializeAsync_RecalculatesExistingTask()
    {
        await using var context = await CreateContextAsync();
        context.Tasks.Add(new TaskItem
        {
            Title = "Спешно предаване на проект",
            Deadline = DateTime.Now.AddDays(1),
            EstimatedMinutes = 240,
            UserImportance = 5,
            Priority = TaskPriority.Low,
            PriorityScore = 1,
            PriorityReason = "Стара оценка"
        });
        await context.SaveChangesAsync();

        await CreateInitializer(context).InitializeAsync();

        var task = await context.Tasks.SingleAsync();
        Assert.True(task.PriorityScore >= 80);
        Assert.Equal(TaskPriority.Critical, task.Priority);
        Assert.Equal(DeadlineCategory.Soon, task.DeadlineCategory);
        Assert.DoesNotContain("Стара оценка", task.PriorityReason);
    }

    private static async Task<AppDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var context = new AppDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.MigrateAsync();
        return context;
    }

    private static DbInitializer CreateInitializer(AppDbContext context) => new(
        context,
        new PriorityScoringService(),
        NullLogger<DbInitializer>.Instance);
}
