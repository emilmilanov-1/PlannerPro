using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;

namespace PlannerPro.Web.Services.Scheduling;

public sealed class ScheduleOptimizerService : IScheduleOptimizerService
{
    public ScheduleGenerationResult Generate(
        IReadOnlyCollection<TaskItem> tasks,
        PlannerSettings settings,
        IReadOnlyCollection<DateOnly> holidays,
        DateTime scheduleStart,
        int planningDays,
        DateTime generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(holidays);

        if (planningDays is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(planningDays), "Периодът трябва да е между 1 и 31 дни.");
        }

        ValidateSettings(settings);

        var holidaySet = holidays.ToHashSet();
        var slots = CreateDaySlots(settings, holidaySet, scheduleStart, planningDays);
        var blocks = new List<ScheduleBlock>();
        var warnings = new List<ScheduleWarning>();

        var activeTasks = tasks
            .Where(task => task.Status is not PlannerTaskStatus.Completed and not PlannerTaskStatus.Postponed)
            .OrderByDescending(task => task.PriorityScore)
            .ThenBy(task => task.Deadline)
            .ThenBy(task => task.Id);

        foreach (var task in activeTasks)
        {
            var remainingMinutes = Math.Max(0, task.EstimatedMinutes - task.CompletedMinutes);
            if (remainingMinutes == 0)
            {
                continue;
            }

            foreach (var slot in slots)
            {
                while (remainingMinutes > 0)
                {
                    var availableMinutes = slot.GetAvailableMinutesBefore(task.Deadline);
                    if (availableMinutes <= 0)
                    {
                        break;
                    }

                    var blockMinutes = Math.Min(
                        remainingMinutes,
                        Math.Min(settings.MaxBlockMinutes, availableMinutes));
                    var blockStart = slot.Cursor;
                    var blockEnd = blockStart.AddMinutes(blockMinutes);

                    blocks.Add(new ScheduleBlock
                    {
                        TaskItemId = task.Id,
                        TaskItem = task,
                        StartTime = blockStart,
                        EndTime = blockEnd,
                        GeneratedAtUtc = generatedAtUtc
                    });

                    slot.Reserve(blockMinutes, settings.BreakMinutes);
                    remainingMinutes -= blockMinutes;
                }

                if (remainingMinutes == 0)
                {
                    break;
                }
            }

            if (remainingMinutes > 0)
            {
                var message = task.Deadline <= scheduleStart
                    ? $"Срокът на „{task.Title}“ вече е изтекъл. Остават {remainingMinutes} минути без график."
                    : $"„{task.Title}“ не може да бъде вместена преди срока. Остават {remainingMinutes} минути без график.";
                warnings.Add(new ScheduleWarning(task.Id, task.Title, remainingMinutes, message));
            }
        }

        return new ScheduleGenerationResult(blocks, warnings);
    }

    private static List<DaySlot> CreateDaySlots(
        PlannerSettings settings,
        HashSet<DateOnly> holidays,
        DateTime scheduleStart,
        int planningDays)
    {
        var slots = new List<DaySlot>();
        var firstDate = DateOnly.FromDateTime(scheduleStart);

        for (var offset = 0; offset < planningDays; offset++)
        {
            var date = firstDate.AddDays(offset);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            if ((!settings.IncludeWeekends && isWeekend) ||
                (settings.SkipPublicHolidays && holidays.Contains(date)))
            {
                continue;
            }

            var dayStart = date.ToDateTime(settings.DayStart);
            var cursor = offset == 0 && scheduleStart > dayStart ? scheduleStart : dayStart;
            var dayEnd = date.ToDateTime(settings.DayEnd);
            if (cursor < dayEnd)
            {
                slots.Add(new DaySlot(cursor, dayEnd, settings.DailyAvailableMinutes));
            }
        }

        return slots;
    }

    private static void ValidateSettings(PlannerSettings settings)
    {
        if (settings.DayStart >= settings.DayEnd)
        {
            throw new ArgumentException("Началото на работния ден трябва да е преди края.", nameof(settings));
        }

        var workWindowMinutes = (int)(settings.DayEnd - settings.DayStart).TotalMinutes;
        if (settings.DailyAvailableMinutes is < 1 or > 720 ||
            settings.DailyAvailableMinutes > workWindowMinutes)
        {
            throw new ArgumentException("Невалидно налично време за деня.", nameof(settings));
        }

        if (settings.MaxBlockMinutes is < 1 or > 240 || settings.BreakMinutes is < 0 or > 120)
        {
            throw new ArgumentException("Невалидна продължителност на блок или почивка.", nameof(settings));
        }
    }

    private sealed class DaySlot(DateTime cursor, DateTime dayEnd, int dailyAvailableMinutes)
    {
        public DateTime Cursor { get; private set; } = cursor;

        private DateTime DayEnd { get; } = dayEnd;

        private int RemainingWorkMinutes { get; set; } = dailyAvailableMinutes;

        public int GetAvailableMinutesBefore(DateTime deadline)
        {
            var effectiveEnd = deadline < DayEnd ? deadline : DayEnd;
            var windowMinutes = (int)Math.Floor((effectiveEnd - Cursor).TotalMinutes);
            return Math.Max(0, Math.Min(RemainingWorkMinutes, windowMinutes));
        }

        public void Reserve(int workMinutes, int breakMinutes)
        {
            Cursor = Cursor.AddMinutes(workMinutes + breakMinutes);
            RemainingWorkMinutes -= workMinutes;
        }
    }
}
