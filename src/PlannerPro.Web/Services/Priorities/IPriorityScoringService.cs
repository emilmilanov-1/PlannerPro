using PlannerPro.Web.Models;

namespace PlannerPro.Web.Services.Priorities;

public interface IPriorityScoringService
{
    PriorityScoringResult Calculate(TaskItem task, DateTime now);

    void Apply(TaskItem task, DateTime now);
}
