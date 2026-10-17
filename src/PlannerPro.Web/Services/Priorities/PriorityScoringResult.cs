using PlannerPro.Web.Models.Enums;

namespace PlannerPro.Web.Services.Priorities;

public sealed record PriorityScoringResult(
    int Score,
    TaskPriority Priority,
    DeadlineCategory DeadlineCategory,
    string Reason);
