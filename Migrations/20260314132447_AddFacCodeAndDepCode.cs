using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IbnElgm3a.Migrations
{
    /// <inheritdoc />
    public partial class AddFacCodeAndDepCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacCode",
                table: "Faculties",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Departments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "DepCode",
                table: "Departments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacCode",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "DepCode",
                table: "Departments");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Departments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
