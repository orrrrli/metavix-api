using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalEvaluationItemCkdStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // KDIGO 2024 stage label ("G1".."G5") populated by the backend when the item
            // is eGFR and ValueUsed is non-null. Max length 8 covers the longest current
            // label ("G3a" / "G3b") with headroom; matches the convention used by the
            // GoalEvaluationItem.Reason column.
            migrationBuilder.AddColumn<string>(
                name: "CkdStage",
                table: "GoalEvaluationItems",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CkdStage",
                table: "GoalEvaluationItems");
        }
    }
}
