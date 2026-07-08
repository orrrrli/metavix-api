using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateClinicalGoalToRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
