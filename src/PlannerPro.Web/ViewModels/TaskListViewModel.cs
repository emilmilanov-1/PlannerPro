using PlannerPro.Web.Models;
using PlannerPro.Web.Models.Enums;

namespace PlannerPro.Web.ViewModels;

public class TaskListViewModel
{
    public IReadOnlyList<TaskItem> Tasks { get; init; } = [];
    public string? Search { get; init; }
    public PlannerTaskStatus? Status { get; init; }
    public TaskCategory? Category { get; init; }
    public TaskPriority? Priority { get; init; }
}
