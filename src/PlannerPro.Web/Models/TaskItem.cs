using System.ComponentModel.DataAnnotations;
using PlannerPro.Web.Models.Enums;

namespace PlannerPro.Web.Models;

public class TaskItem : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Заглавието е задължително.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Заглавието трябва да е между 3 и 150 знака.")]
    [Display(Name = "Заглавие")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1500, ErrorMessage = "Описанието може да съдържа максимум 1500 знака.")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [EnumDataType(typeof(TaskCategory), ErrorMessage = "Избери валидна категория.")]
    [Display(Name = "Категория")]
    public TaskCategory Category { get; set; } = TaskCategory.Study;

    [Required(ErrorMessage = "Крайният срок е задължителен.")]
    [Display(Name = "Краен срок")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}")]
    public DateTime Deadline { get; set; }

    [Range(15, 1440, ErrorMessage = "Прогнозното време трябва да е между 15 и 1440 минути.")]
    [Display(Name = "Прогнозни минути")]
    public int EstimatedMinutes { get; set; } = 60;

    [Range(0, 1440, ErrorMessage = "Изпълненото време трябва да е между 0 и 1440 минути.")]
    [Display(Name = "Изпълнени минути")]
    public int CompletedMinutes { get; set; }

    [Range(1, 5, ErrorMessage = "Важността трябва да е между 1 и 5.")]
    [Display(Name = "Важност")]
    public int UserImportance { get; set; } = 3;

    [EnumDataType(typeof(PlannerTaskStatus), ErrorMessage = "Избери валиден статус.")]
    [Display(Name = "Статус")]
    public PlannerTaskStatus Status { get; set; } = PlannerTaskStatus.New;

    [Display(Name = "Приоритет")]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [Range(0, 100)]
    [Display(Name = "Оценка на приоритета")]
    public int PriorityScore { get; set; } = 30;

    [Display(Name = "Срокова група")]
    public DeadlineCategory DeadlineCategory { get; set; } = DeadlineCategory.Planned;

    [StringLength(500)]
    [Display(Name = "Обяснение на приоритета")]
    public string PriorityReason { get; set; } = "Оценката се изчислява автоматично при записване на задачата.";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<ScheduleBlock> ScheduleBlocks { get; set; } = new List<ScheduleBlock>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CompletedMinutes > EstimatedMinutes)
        {
            yield return new ValidationResult(
                "Изпълнените минути не могат да надвишават прогнозните.",
                [nameof(CompletedMinutes)]);
        }
    }
}
