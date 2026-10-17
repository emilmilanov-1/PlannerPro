using System.ComponentModel.DataAnnotations;

namespace PlannerPro.Web.Models.Enums;

public enum DeadlineCategory
{
    [Display(Name = "Просрочена")]
    Overdue,

    [Display(Name = "Днес")]
    Today,

    [Display(Name = "Скоро")]
    Soon,

    [Display(Name = "Предстояща")]
    Upcoming,

    [Display(Name = "Планирана")]
    Planned
}
