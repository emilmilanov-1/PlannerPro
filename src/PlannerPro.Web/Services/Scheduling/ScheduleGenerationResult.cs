using PlannerPro.Web.Models;

namespace PlannerPro.Web.Services.Scheduling;

public sealed record ScheduleGenerationResult(
    IReadOnlyList<ScheduleBlock> Blocks,
    IReadOnlyList<ScheduleWarning> Warnings);
