using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IbnElgm3a.Migrations
{
    /// <inheritdoc />
    public partial class AddIconAndDescriptionToAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentId1",
                table: "Users",
                type: "character varying(50)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptAdmissions",
                table: "Faculties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AiChatbotEnabled",
                table: "Faculties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Building",
                table: "Faculties",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstablishedYear",
                table: "Faculties",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstructorCount",
                table: "Faculties",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OfficialEmail",
                table: "Faculties",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialPhone",
                table: "Faculties",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PublicProfile",
                table: "Faculties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "Departments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeadUserId",
                table: "Departments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstructorCount",
                table: "Departments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LevelCount",
                table: "Departments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Departments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Syllabus",
                table: "Courses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId1",
                table: "Users",
                column: "DepartmentId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId1",
                table: "Users",
                column: "DepartmentId1",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AcceptAdmissions",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "AiChatbotEnabled",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "Building",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "EstablishedYear",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "InstructorCount",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "OfficialEmail",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "OfficialPhone",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "PublicProfile",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "HeadUserId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "InstructorCount",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "LevelCount",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Syllabus",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "AuditLogs");
        }
    }
}
