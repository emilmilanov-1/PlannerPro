using PlannerPro.Web.Models;

namespace PlannerPro.Web.ViewModels;

public sealed class ScheduleDayViewModel
{
    public DateOnly Date { get; init; }

    public IReadOnlyList<ScheduleBlock> Blocks { get; init; } = [];

    public int PlannedMinutes => Blocks.Sum(block => block.DurationMinutes);
}
