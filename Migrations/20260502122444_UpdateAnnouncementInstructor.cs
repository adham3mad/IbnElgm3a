using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IbnElgm3a.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnnouncementInstructor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Audience",
                table: "Announcements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InstructorId",
                table: "Announcements",
                type: "character varying(50)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_InstructorId",
                table: "Announcements",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Instructors_InstructorId",
                table: "Announcements",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Instructors_InstructorId",
                table: "Announcements");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_InstructorId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "Audience",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Announcements");
        }
    }
}
