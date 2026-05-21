using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInsulinDm1Tool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsulinDm1Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsulinName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Ric = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    SensitivityFactor = table.Column<int>(type: "integer", nullable: true),
                    TargetGlucose = table.Column<int>(type: "integer", nullable: true),
                    DoctorName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DoctorPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsulinDm1Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsulinDm1Profiles_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InsulinDm1Records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GlucoseBefore = table.Column<int>(type: "integer", nullable: true),
                    GlucoseAfter = table.Column<int>(type: "integer", nullable: true),
                    TotalCarbs = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: true),
                    DoseApplied = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    MealDescription = table.Column<string>(type: "text", nullable: true),
                    HowIFelt = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsulinDm1Records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsulinDm1Records_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsulinDm1Profiles_PatientId",
                table: "InsulinDm1Profiles",
                column: "PatientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsulinDm1Records_PatientId_RecordDate",
                table: "InsulinDm1Records",
                columns: new[] { "PatientId", "RecordDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsulinDm1Profiles");

            migrationBuilder.DropTable(
                name: "InsulinDm1Records");
        }
    }
}
