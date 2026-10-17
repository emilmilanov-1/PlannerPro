using PlannerPro.Web.Models;

namespace PlannerPro.Web.Services.Scheduling;

public interface IScheduleOptimizerService
{
    ScheduleGenerationResult Generate(
        IReadOnlyCollection<TaskItem> tasks,
        PlannerSettings settings,
        IReadOnlyCollection<DateOnly> holidays,
        DateTime scheduleStart,
        int planningDays,
        DateTime generatedAtUtc);
}
