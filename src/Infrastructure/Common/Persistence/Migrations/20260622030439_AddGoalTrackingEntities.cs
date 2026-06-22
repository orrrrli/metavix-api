using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalTrackingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicalGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomValue = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalGoals_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggeredBy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalEvaluations_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalEvaluationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalEvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValueUsed = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    GoalUsed = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalEvaluationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalEvaluationItems_GoalEvaluations_GoalEvaluationId",
                        column: x => x.GoalEvaluationId,
                        principalTable: "GoalEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalGoals_PatientId",
                table: "ClinicalGoals",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalEvaluationItems_GoalEvaluationId",
                table: "GoalEvaluationItems",
                column: "GoalEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalEvaluations_PatientId",
                table: "GoalEvaluations",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicalGoals");

            migrationBuilder.DropTable(
                name: "GoalEvaluationItems");

            migrationBuilder.DropTable(
                name: "GoalEvaluations");
        }
    }
}
