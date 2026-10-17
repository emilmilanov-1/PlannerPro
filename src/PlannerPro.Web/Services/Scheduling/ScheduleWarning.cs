namespace PlannerPro.Web.Services.Scheduling;

public sealed record ScheduleWarning(
    int TaskItemId,
    string TaskTitle,
    int UnscheduledMinutes,
    string Message);
