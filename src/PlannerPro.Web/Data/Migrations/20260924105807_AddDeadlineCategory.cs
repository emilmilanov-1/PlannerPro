using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlannerPro.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeadlineCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeadlineCategory",
                table: "Tasks",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "Planned");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadlineCategory",
                table: "Tasks");
        }
    }
}
