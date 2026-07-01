using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameDoctorNameFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Doctors",
                newName: "PaternalLastName");

            migrationBuilder.AddColumn<string>(
                name: "MaternalLastName",
                table: "Doctors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Doctors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaternalLastName",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Doctors");

            migrationBuilder.RenameColumn(
                name: "PaternalLastName",
                table: "Doctors",
                newName: "LastName");
        }
    }
}
