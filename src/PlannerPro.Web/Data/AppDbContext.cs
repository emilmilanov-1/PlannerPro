using Microsoft.EntityFrameworkCore;
using PlannerPro.Web.Models;

namespace PlannerPro.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<PlannerSettings> PlannerSettings => Set<PlannerSettings>();

    public DbSet<ScheduleBlock> ScheduleBlocks => Set<ScheduleBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var task = modelBuilder.Entity<TaskItem>();
        task.Property(item => item.Category).HasConversion<string>().HasMaxLength(30);
        task.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
        task.Property(item => item.Priority).HasConversion<string>().HasMaxLength(30);
        task.Property(item => item.DeadlineCategory).HasConversion<string>().HasMaxLength(30);
        task.HasIndex(item => item.Deadline);
        task.HasIndex(item => item.Status);
        task.HasIndex(item => item.Priority);

        var settings = modelBuilder.Entity<PlannerSettings>();
        settings.Property(item => item.CountryCode).HasMaxLength(2);

        var scheduleBlock = modelBuilder.Entity<ScheduleBlock>();
        scheduleBlock.Ignore(item => item.DurationMinutes);
        scheduleBlock
            .HasOne(item => item.TaskItem)
            .WithMany(item => item.ScheduleBlocks)
            .HasForeignKey(item => item.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);
        scheduleBlock.HasIndex(item => item.StartTime);
        scheduleBlock.HasIndex(item => item.TaskItemId);
    }
}
