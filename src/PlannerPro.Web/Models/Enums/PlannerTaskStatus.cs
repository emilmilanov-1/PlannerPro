using System.ComponentModel.DataAnnotations;

namespace PlannerPro.Web.Models.Enums;

public enum PlannerTaskStatus
{
    [Display(Name = "Нова")]
    New,

    [Display(Name = "В процес")]
    InProgress,

    [Display(Name = "Завършена")]
    Completed,

    [Display(Name = "Отложена")]
    Postponed
}
