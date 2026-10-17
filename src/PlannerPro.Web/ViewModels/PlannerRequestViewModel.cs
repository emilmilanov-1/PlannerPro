using System.ComponentModel.DataAnnotations;

namespace PlannerPro.Web.ViewModels;

public sealed class PlannerRequestViewModel : IValidatableObject
{
    [DataType(DataType.Date)]
    [Display(Name = "Начална дата")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(1, 14, ErrorMessage = "Периодът трябва да е между 1 и 14 дни.")]
    [Display(Name = "Период в дни")]
    public int PlanningDays { get; set; } = 7;

    [DataType(DataType.Time)]
    [Display(Name = "Начало на деня")]
    public TimeOnly DayStart { get; set; } = new(9, 0);

    [DataType(DataType.Time)]
    [Display(Name = "Край на деня")]
    public TimeOnly DayEnd { get; set; } = new(18, 0);

    [Range(30, 720, ErrorMessage = "Наличното време трябва да е между 30 и 720 минути.")]
    [Display(Name = "Налични минути на ден")]
    public int DailyAvailableMinutes { get; set; } = 240;

    [Range(15, 240, ErrorMessage = "Максималният блок трябва да е между 15 и 240 минути.")]
    [Display(Name = "Максимален блок")]
    public int MaxBlockMinutes { get; set; } = 90;

    [Range(0, 120, ErrorMessage = "Почивката трябва да е между 0 и 120 минути.")]
    [Display(Name = "Почивка в минути")]
    public int BreakMinutes { get; set; } = 15;

    [Display(Name = "Включи събота и неделя")]
    public bool IncludeWeekends { get; set; }

    [Display(Name = "Пропускай официалните празници")]
    public bool SkipPublicHolidays { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DayStart >= DayEnd)
        {
            yield return new ValidationResult(
                "Началото на деня трябва да е преди края.",
                [nameof(DayStart), nameof(DayEnd)]);
            yield break;
        }

        var workWindowMinutes = (int)(DayEnd - DayStart).TotalMinutes;
        if (DailyAvailableMinutes > workWindowMinutes)
        {
            yield return new ValidationResult(
                "Наличните минути не могат да надвишават работния прозорец.",
                [nameof(DailyAvailableMinutes)]);
        }

        if (StartDate < DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult(
                "Началната дата не може да бъде в миналото.",
                [nameof(StartDate)]);
        }
    }
}
