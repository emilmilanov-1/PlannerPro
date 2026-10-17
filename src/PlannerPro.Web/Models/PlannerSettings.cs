using System.ComponentModel.DataAnnotations;

namespace PlannerPro.Web.Models;

public class PlannerSettings : IValidatableObject
{
    public int Id { get; set; } = 1;

    [Display(Name = "Начало на работния ден")]
    public TimeOnly DayStart { get; set; } = new(9, 0);

    [Display(Name = "Край на работния ден")]
    public TimeOnly DayEnd { get; set; } = new(18, 0);

    [Range(30, 720, ErrorMessage = "Дневното време трябва да е между 30 и 720 минути.")]
    [Display(Name = "Налични минути на ден")]
    public int DailyAvailableMinutes { get; set; } = 240;

    [Range(15, 240, ErrorMessage = "Максималният блок трябва да е между 15 и 240 минути.")]
    [Display(Name = "Максимален блок")]
    public int MaxBlockMinutes { get; set; } = 90;

    [Range(0, 120, ErrorMessage = "Почивката трябва да е между 0 и 120 минути.")]
    [Display(Name = "Почивка между блоковете")]
    public int BreakMinutes { get; set; } = 15;

    [Display(Name = "Включи събота и неделя")]
    public bool IncludeWeekends { get; set; }

    [Display(Name = "Пропускай официалните празници")]
    public bool SkipPublicHolidays { get; set; } = true;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = "BG";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DayStart >= DayEnd)
        {
            yield return new ValidationResult(
                "Началото на работния ден трябва да е преди края.",
                [nameof(DayStart), nameof(DayEnd)]);
            yield break;
        }

        var workWindowMinutes = (int)(DayEnd - DayStart).TotalMinutes;
        if (DailyAvailableMinutes > workWindowMinutes)
        {
            yield return new ValidationResult(
                "Наличните минути не могат да надвишават продължителността на работния ден.",
                [nameof(DailyAvailableMinutes)]);
        }
    }
}
