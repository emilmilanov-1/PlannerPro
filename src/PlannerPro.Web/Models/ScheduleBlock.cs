using System.ComponentModel.DataAnnotations;

namespace PlannerPro.Web.Models;

public class ScheduleBlock : IValidatableObject
{
    public int Id { get; set; }

    public int TaskItemId { get; set; }

    public TaskItem? TaskItem { get; set; }

    [Display(Name = "Начало")]
    public DateTime StartTime { get; set; }

    [Display(Name = "Край")]
    public DateTime EndTime { get; set; }

    [Display(Name = "Изпълнен")]
    public bool IsCompleted { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "Краят на блока трябва да бъде след началото.",
                [nameof(EndTime)]);
        }
    }
}
