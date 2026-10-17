using System.ComponentModel.DataAnnotations;
using PlannerPro.Web.Models;

namespace PlannerPro.Tests;

public class TaskItemValidationTests
{
    [Fact]
    public void Validation_Fails_WhenTitleIsTooShort()
    {
        var task = CreateValidTask();
        task.Title = "AB";

        var results = Validate(task);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(TaskItem.Title)));
    }

    [Fact]
    public void Validation_Fails_WhenCompletedMinutesExceedEstimate()
    {
        var task = CreateValidTask();
        task.EstimatedMinutes = 60;
        task.CompletedMinutes = 90;

        var results = Validate(task);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(TaskItem.CompletedMinutes)));
    }

    [Fact]
    public void Validation_Passes_ForValidTask()
    {
        var task = CreateValidTask();

        var results = Validate(task);

        Assert.Empty(results);
    }

    private static TaskItem CreateValidTask() => new()
    {
        Title = "Валидна задача",
        Deadline = DateTime.Now.AddDays(1),
        EstimatedMinutes = 60,
        CompletedMinutes = 15,
        UserImportance = 3
    };

    private static List<ValidationResult> Validate(TaskItem task)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(task, new ValidationContext(task), results, validateAllProperties: true);
        return results;
    }
}
