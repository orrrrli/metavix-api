using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDomainModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyRecords_Doctors_DoctorId",
                table: "DailyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_Doctors_DoctorId",
                table: "LabResults");

            migrationBuilder.DropIndex(
                name: "IX_LabResults_DoctorId",
                table: "LabResults");

            migrationBuilder.DropIndex(
                name: "IX_DailyRecords_DoctorId",
                table: "DailyRecords");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "DailyRecords");

            migrationBuilder.DropColumn(
                name: "FastingGlucose",
                table: "DailyRecords");

            migrationBuilder.DropColumn(
                name: "PostprandialGlucose",
                table: "DailyRecords");

            migrationBuilder.DropColumn(
                name: "RecordedById",
                table: "DailyRecords");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "DailyRecords");

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "Patients",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EgoGlucose",
                table: "LabResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EgoProteins",
                table: "LabResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Doctors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WaistCm",
                table: "DailyRecords",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "GlucoseReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadingType = table.Column<int>(type: "integer", nullable: false),
                    ValueMgDl = table.Column<int>(type: "integer", nullable: false),
                    Time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Foods = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlucoseReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlucoseReadings_DailyRecords_DailyRecordId",
                        column: x => x.DailyRecordId,
                        principalTable: "DailyRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlucoseReadings_DailyRecordId",
                table: "GlucoseReadings",
                column: "DailyRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlucoseReadings");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "EgoGlucose",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "EgoProteins",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Doctors");

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "LabResults",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "WaistCm",
                table: "DailyRecords",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "DailyRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "FastingGlucose",
                table: "DailyRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PostprandialGlucose",
                table: "DailyRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecordedById",
                table: "DailyRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "DailyRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_DoctorId",
                table: "LabResults",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRecords_DoctorId",
                table: "DailyRecords",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyRecords_Doctors_DoctorId",
                table: "DailyRecords",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_Doctors_DoctorId",
                table: "LabResults",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
