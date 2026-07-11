using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Common.Persistence.Migrations
{
    /// <summary>
    /// Doctor-driven MRN assignment:
    /// 1. Backfills every patient that still has an empty MedicalRecordNumber
    ///    with a sequential <c>MRN-2026-NNNNNN</c> value, ordered by
    ///    CreatedAt so the order is stable across runs.
    /// 2. Makes <c>MedicalRecordNumber</c> nullable in the database. Until a
    ///    doctor accepts a link request, new patients keep the empty string
    ///    as a "not yet assigned" placeholder.
    /// 3. Creates a partial unique index that excludes the empty string, so
    ///    the application-level uniqueness check is backed by the database.
    /// </summary>
    public partial class AddMrnUniqueIndexAndBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Backfill empty MRNs with sequential MRN-2026-NNNNNN values.
            //    Done BEFORE making the column nullable / adding the index so
            //    existing rows have a concrete value the index can see.
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT ""Id"",
                           'MRN-2026-' || LPAD(row_number() OVER (ORDER BY ""CreatedAt"", ""Id"")::text, 6, '0') AS mrn
                      FROM ""Patients""
                     WHERE ""MedicalRecordNumber"" IS NULL OR ""MedicalRecordNumber"" = ''
                )
                UPDATE ""Patients"" p
                   SET ""MedicalRecordNumber"" = r.mrn
                  FROM ranked r
                 WHERE p.""Id"" = r.""Id"";
            ");

            // 2) Make the column nullable — fresh patients register with
            //    empty MRN until a doctor accepts a link request. Column type
            //    stays as `text` (unbounded) — no length change vs. the
            //    pre-existing schema.
            migrationBuilder.AlterColumn<string>(
                name: "MedicalRecordNumber",
                table: "Patients",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            // 3) Partial unique index — backstop against the application-level
            //    ExistsByMedicalRecordNumberAsync check. Empty-string rows are
            //    excluded so multiple unassigned patients don't collide.
            migrationBuilder.CreateIndex(
                name: "IX_Patients_MedicalRecordNumber",
                table: "Patients",
                column: "MedicalRecordNumber",
                unique: true,
                filter: "\"MedicalRecordNumber\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_MedicalRecordNumber",
                table: "Patients");

            // Restore the column as NOT NULL. The backfill above guarantees
            // there are no empty strings, so this is safe.
            migrationBuilder.AlterColumn<string>(
                name: "MedicalRecordNumber",
                table: "Patients",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
