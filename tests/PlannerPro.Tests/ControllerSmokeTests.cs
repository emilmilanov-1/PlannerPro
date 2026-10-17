using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using PlannerPro.Web.Data;
using PlannerPro.Web.Controllers;
using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;
using PlannerPro.Web.ViewModels;
using PlannerPro.Web.Services.Priorities;
using PlannerPro.Web.Services.Holidays;
using PlannerPro.Web.Services.Scheduling;

namespace PlannerPro.Tests;

public class ControllerSmokeTests
{
    [Fact]
    public async Task TasksIndex_ReturnsView()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var controller = new TasksController(context, new PriorityScoringService());

        var result = await controller.Index(null, null, null, null);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task HomeIndex_ReturnsDashboardData()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        context.Tasks.Add(new TaskItem
        {
            Title = "Важна задача",
            Deadline = DateTime.Now.AddDays(1),
            EstimatedMinutes = 60,
            UserImportance = 5,
            Priority = TaskPriority.High,
            PriorityScore = 70
        });
        await context.SaveChangesAsync();
        var task = await context.Tasks.SingleAsync();
        context.ScheduleBlocks.Add(new ScheduleBlock
        {
            TaskItemId = task.Id,
            StartTime = DateTime.Now.AddHours(1),
            EndTime = DateTime.Now.AddHours(2)
        });
        await context.SaveChangesAsync();
        var controller = new HomeController(context);

        var result = Assert.IsType<ViewResult>(await controller.Index());
        var model = Assert.IsType<DashboardViewModel>(result.Model);

        Assert.Equal(1, model.TotalTasks);
        Assert.Equal(1, model.HighPriorityTasks);
        Assert.Equal("Важна задача", model.NextTask?.Title);
        Assert.Equal("Важна задача", model.NextScheduleBlock?.TaskItem?.Title);
    }

    [Fact]
    public async Task TasksEdit_CompletesRelatedScheduleBlocks()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var task = new TaskItem
        {
            Title = "Задача с блок",
            Deadline = DateTime.Now.AddDays(1),
            EstimatedMinutes = 60,
            UserImportance = 3
        };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        context.ScheduleBlocks.Add(new ScheduleBlock
        {
            TaskItemId = task.Id,
            StartTime = DateTime.Now.AddHours(1),
            EndTime = DateTime.Now.AddHours(2)
        });
        await context.SaveChangesAsync();
        var controller = new TasksController(context, new PriorityScoringService())
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new StubTempDataProvider())
        };
        var input = new TaskItem
        {
            Id = task.Id,
            Title = task.Title,
            Deadline = task.Deadline,
            EstimatedMinutes = task.EstimatedMinutes,
            UserImportance = task.UserImportance,
            Status = PlannerTaskStatus.Completed
        };

        var result = await controller.Edit(task.Id, input);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True((await context.ScheduleBlocks.SingleAsync()).IsCompleted);
        Assert.Equal(60, (await context.Tasks.SingleAsync()).CompletedMinutes);
    }

    [Fact]
    public async Task TasksDelete_MissingTask_SetsErrorMessage()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var controller = new TasksController(context, new PriorityScoringService())
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new StubTempDataProvider())
        };

        var result = await controller.DeleteConfirmed(999);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(
            "Задачата не беше намерена или вече е изтрита.",
            controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task PlannerIndex_ReturnsView()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var controller = new PlannerController(
            context,
            new StubHolidayApiClient(),
            new ScheduleOptimizerService())
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new StubTempDataProvider())
        };

        var result = await controller.Index(null, null, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task PlannerGenerate_PersistsScheduleBlocks()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        context.PlannerSettings.Add(new PlannerSettings());
        context.Tasks.Add(new TaskItem
        {
            Title = "Задача за графика",
            Deadline = startDate.AddDays(2).ToDateTime(new TimeOnly(18, 0)),
            EstimatedMinutes = 120,
            UserImportance = 5,
            Priority = TaskPriority.Critical,
            PriorityScore = 90
        });
        await context.SaveChangesAsync();
        var controller = new PlannerController(
            context,
            new StubHolidayApiClient(),
            new ScheduleOptimizerService())
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new StubTempDataProvider())
        };
        var request = new PlannerRequestViewModel
        {
            StartDate = startDate,
            PlanningDays = 2,
            DayStart = new TimeOnly(9, 0),
            DayEnd = new TimeOnly(18, 0),
            DailyAvailableMinutes = 120,
            MaxBlockMinutes = 60,
            BreakMinutes = 15,
            SkipPublicHolidays = true
        };

        var result = await controller.Generate(request, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        var blocks = await context.ScheduleBlocks.AsNoTracking().ToListAsync();
        Assert.Equal(2, blocks.Count);
        Assert.Equal(120, blocks.Sum(block => block.DurationMinutes));
    }

    [Fact]
    public async Task StatisticsIndex_ReturnsCalculatedData()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var completedTask = new TaskItem
        {
            Title = "Завършена задача",
            Category = TaskCategory.Study,
            Deadline = DateTime.Now.AddDays(1),
            EstimatedMinutes = 60,
            CompletedMinutes = 60,
            Status = PlannerTaskStatus.Completed
        };
        var activeTask = new TaskItem
        {
            Title = "Активна задача",
            Category = TaskCategory.Work,
            Deadline = DateTime.Now.AddDays(2),
            EstimatedMinutes = 120,
            CompletedMinutes = 30,
            Status = PlannerTaskStatus.InProgress,
            Priority = TaskPriority.High
        };
        context.Tasks.AddRange(completedTask, activeTask);
        await context.SaveChangesAsync();
        context.ScheduleBlocks.Add(new ScheduleBlock
        {
            TaskItemId = completedTask.Id,
            StartTime = DateTime.Today.AddHours(9),
            EndTime = DateTime.Today.AddHours(10),
            IsCompleted = true
        });
        await context.SaveChangesAsync();
        var controller = new StatisticsController(context);

        var result = Assert.IsType<ViewResult>(await controller.Index());
        var model = Assert.IsType<StatisticsViewModel>(result.Model);

        Assert.Equal(2, model.TotalTasks);
        Assert.Equal(1, model.CompletedTasks);
        Assert.Equal(50, model.TaskCompletionPercentage);
        Assert.Equal(50, model.TimeCompletionPercentage);
        Assert.Equal(60, model.WeeklyProgress[0].CompletedMinutes);
    }

    private sealed class StubHolidayApiClient : IHolidayApiClient
    {
        public Task<HolidayFetchResult> GetHolidaysAsync(
            string countryCode,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new HolidayFetchResult([], IsAvailable: true, FromCache: false));
    }

    private sealed class StubTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
