using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalGoalPatientParameterUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ClinicalGoals_PatientId_ParameterId",
                table: "ClinicalGoals",
                columns: new[] { "PatientId", "ParameterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicalGoals_PatientId_ParameterId",
                table: "ClinicalGoals");
        }
    }
}
