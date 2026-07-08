using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateClinicalGoalToRangesAndAddReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy single-value custom goals are replaced by a four-threshold band model.
            // Existing rows are smoke-test data only; the column is dropped rather than migrated.
            migrationBuilder.DropColumn(
                name: "CustomValue",
                table: "ClinicalGoals");

            migrationBuilder.AddColumn<decimal>(
                name: "CustomAtRiskHigh",
                table: "ClinicalGoals",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomAtRiskLow",
                table: "ClinicalGoals",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomOutOfRangeHigh",
                table: "ClinicalGoals",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomOutOfRangeLow",
                table: "ClinicalGoals",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "GoalEvaluationItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "GoalEvaluationItems");

            migrationBuilder.DropColumn(
                name: "CustomAtRiskHigh",
                table: "ClinicalGoals");

            migrationBuilder.DropColumn(
                name: "CustomAtRiskLow",
                table: "ClinicalGoals");

            migrationBuilder.DropColumn(
                name: "CustomOutOfRangeHigh",
                table: "ClinicalGoals");

            migrationBuilder.DropColumn(
                name: "CustomOutOfRangeLow",
                table: "ClinicalGoals");

            migrationBuilder.AddColumn<decimal>(
                name: "CustomValue",
                table: "ClinicalGoals",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
