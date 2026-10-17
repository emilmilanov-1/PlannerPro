using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlannerPro.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannerSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlannerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DayStart = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    DayEnd = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    DailyAvailableMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxBlockMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    BreakMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    IncludeWeekends = table.Column<bool>(type: "INTEGER", nullable: false),
                    SkipPublicHolidays = table.Column<bool>(type: "INTEGER", nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleBlocks_Tasks_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleBlocks_StartTime",
                table: "ScheduleBlocks",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleBlocks_TaskItemId",
                table: "ScheduleBlocks",
                column: "TaskItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlannerSettings");

            migrationBuilder.DropTable(
                name: "ScheduleBlocks");
        }
    }
}
