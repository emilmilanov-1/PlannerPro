using System.ComponentModel.DataAnnotations;

namespace PlannerPro.Web.Models.Enums;

public enum TaskPriority
{
    [Display(Name = "Нисък")]
    Low,

    [Display(Name = "Среден")]
    Medium,

    [Display(Name = "Висок")]
    High,

    [Display(Name = "Критичен")]
    Critical
}
