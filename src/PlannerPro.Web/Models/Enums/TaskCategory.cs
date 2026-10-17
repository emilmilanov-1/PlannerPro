using System.ComponentModel.DataAnnotations;

namespace PlannerPro.Web.Models.Enums;

public enum TaskCategory
{
    [Display(Name = "Обучение")]
    Study,

    [Display(Name = "Работа")]
    Work,

    [Display(Name = "Лични")]
    Personal,

    [Display(Name = "Други")]
    Other
}
