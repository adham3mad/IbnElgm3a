using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IbnElgm3a.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToV2_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InternalNote",
                table: "Complaints");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_type", "student,instructor")
                .Annotation("Npgsql:Enum:announcement_priority", "normal,urgent")
                .Annotation("Npgsql:Enum:announcement_target_type", "all,faculty,department,role")
                .Annotation("Npgsql:Enum:app_type", "dashboard,platform")
                .Annotation("Npgsql:Enum:calendar_event_type", "academic,exam,holiday,admin,registration")
                .Annotation("Npgsql:Enum:class_type", "lecture,lab,tutorial")
                .Annotation("Npgsql:Enum:complaint_status", "open,in_review,resolved")
                .Annotation("Npgsql:Enum:complaint_type", "academic,financial,technical,facility,other")
                .Annotation("Npgsql:Enum:day_of_week_enum", "saturday,sunday,monday,tuesday,wednesday,thursday,friday")
                .Annotation("Npgsql:Enum:enrollment_status", "pending,enrolled,dropped,completed")
                .Annotation("Npgsql:Enum:exam_status", "draft,published")
                .Annotation("Npgsql:Enum:exam_type", "midterm,final")
                .Annotation("Npgsql:Enum:gender", "male,female")
                .Annotation("Npgsql:Enum:letter_grade", "a,a_minus,b_plus,b,b_minus,c_plus,c,d,f")
                .Annotation("Npgsql:Enum:relation_type", "father,mother,guardian,other")
                .Annotation("Npgsql:Enum:room_type", "lecture_hall,lab,tutorial_room")
                .Annotation("Npgsql:Enum:schedule_recurrence", "weekly,biweekly")
                .Annotation("Npgsql:Enum:seating_strategy", "alphabetical,random,by_gpa")
                .Annotation("Npgsql:Enum:sub_admin_scope_type", "university,faculty,department")
                .Annotation("Npgsql:Enum:user_role", "student,instructor,admin")
                .Annotation("Npgsql:Enum:user_status", "active,inactive,at_risk")
                .OldAnnotation("Npgsql:Enum:account_type", "student,instructor")
                .OldAnnotation("Npgsql:Enum:announcement_priority", "normal,urgent")
                .OldAnnotation("Npgsql:Enum:announcement_target_type", "all,faculty,department,role")
                .OldAnnotation("Npgsql:Enum:app_type", "dashboard,platform")
                .OldAnnotation("Npgsql:Enum:calendar_event_type", "academic,exam,holiday,admin,registration")
                .OldAnnotation("Npgsql:Enum:class_type", "lecture,lab,tutorial")
                .OldAnnotation("Npgsql:Enum:complaint_status", "open,in_review,resolved")
                .OldAnnotation("Npgsql:Enum:complaint_type", "academic,financial,technical,facility,other")
                .OldAnnotation("Npgsql:Enum:day_of_week_enum", "saturday,sunday,monday,tuesday,wednesday,thursday,friday")
                .OldAnnotation("Npgsql:Enum:enrollment_status", "pending,enrolled,dropped,completed")
                .OldAnnotation("Npgsql:Enum:exam_status", "draft,published")
                .OldAnnotation("Npgsql:Enum:exam_type", "midterm,final")
                .OldAnnotation("Npgsql:Enum:gender", "male,female")
                .OldAnnotation("Npgsql:Enum:letter_grade", "a,a_minus,b_plus,b,b_minus,c_plus,c,d,f")
                .OldAnnotation("Npgsql:Enum:relation_type", "father,mother,guardian,other")
                .OldAnnotation("Npgsql:Enum:schedule_recurrence", "weekly,biweekly")
                .OldAnnotation("Npgsql:Enum:sub_admin_scope_type", "university,faculty,department")
                .OldAnnotation("Npgsql:Enum:user_role", "student,instructor,admin")
                .OldAnnotation("Npgsql:Enum:user_status", "active,inactive,at_risk");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Permissions",
                table: "SubAdmins",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Rooms",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "Exams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeatPlanPdfUrl",
                table: "Exams",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeatingStrategy",
                table: "Exams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "BulkImportJobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "BulkImportJobs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ComplaintNotes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ComplaintId = table.Column<string>(type: "character varying(50)", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(50)", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintNotes_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplaintNotes_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintNotes_AuthorId",
                table: "ComplaintNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintNotes_ComplaintId",
                table: "ComplaintNotes",
                column: "ComplaintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplaintNotes");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "SubAdmins");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "SeatPlanPdfUrl",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "SeatingStrategy",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "BulkImportJobs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "BulkImportJobs");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_type", "student,instructor")
                .Annotation("Npgsql:Enum:announcement_priority", "normal,urgent")
                .Annotation("Npgsql:Enum:announcement_target_type", "all,faculty,department,role")
                .Annotation("Npgsql:Enum:app_type", "dashboard,platform")
                .Annotation("Npgsql:Enum:calendar_event_type", "academic,exam,holiday,admin,registration")
                .Annotation("Npgsql:Enum:class_type", "lecture,lab,tutorial")
                .Annotation("Npgsql:Enum:complaint_status", "open,in_review,resolved")
                .Annotation("Npgsql:Enum:complaint_type", "academic,financial,technical,facility,other")
                .Annotation("Npgsql:Enum:day_of_week_enum", "saturday,sunday,monday,tuesday,wednesday,thursday,friday")
                .Annotation("Npgsql:Enum:enrollment_status", "pending,enrolled,dropped,completed")
                .Annotation("Npgsql:Enum:exam_status", "draft,published")
                .Annotation("Npgsql:Enum:exam_type", "midterm,final")
                .Annotation("Npgsql:Enum:gender", "male,female")
                .Annotation("Npgsql:Enum:letter_grade", "a,a_minus,b_plus,b,b_minus,c_plus,c,d,f")
                .Annotation("Npgsql:Enum:relation_type", "father,mother,guardian,other")
                .Annotation("Npgsql:Enum:schedule_recurrence", "weekly,biweekly")
                .Annotation("Npgsql:Enum:sub_admin_scope_type", "university,faculty,department")
                .Annotation("Npgsql:Enum:user_role", "student,instructor,admin")
                .Annotation("Npgsql:Enum:user_status", "active,inactive,at_risk")
                .OldAnnotation("Npgsql:Enum:account_type", "student,instructor")
                .OldAnnotation("Npgsql:Enum:announcement_priority", "normal,urgent")
                .OldAnnotation("Npgsql:Enum:announcement_target_type", "all,faculty,department,role")
                .OldAnnotation("Npgsql:Enum:app_type", "dashboard,platform")
                .OldAnnotation("Npgsql:Enum:calendar_event_type", "academic,exam,holiday,admin,registration")
                .OldAnnotation("Npgsql:Enum:class_type", "lecture,lab,tutorial")
                .OldAnnotation("Npgsql:Enum:complaint_status", "open,in_review,resolved")
                .OldAnnotation("Npgsql:Enum:complaint_type", "academic,financial,technical,facility,other")
                .OldAnnotation("Npgsql:Enum:day_of_week_enum", "saturday,sunday,monday,tuesday,wednesday,thursday,friday")
                .OldAnnotation("Npgsql:Enum:enrollment_status", "pending,enrolled,dropped,completed")
                .OldAnnotation("Npgsql:Enum:exam_status", "draft,published")
                .OldAnnotation("Npgsql:Enum:exam_type", "midterm,final")
                .OldAnnotation("Npgsql:Enum:gender", "male,female")
                .OldAnnotation("Npgsql:Enum:letter_grade", "a,a_minus,b_plus,b,b_minus,c_plus,c,d,f")
                .OldAnnotation("Npgsql:Enum:relation_type", "father,mother,guardian,other")
                .OldAnnotation("Npgsql:Enum:room_type", "lecture_hall,lab,tutorial_room")
                .OldAnnotation("Npgsql:Enum:schedule_recurrence", "weekly,biweekly")
                .OldAnnotation("Npgsql:Enum:seating_strategy", "alphabetical,random,by_gpa")
                .OldAnnotation("Npgsql:Enum:sub_admin_scope_type", "university,faculty,department")
                .OldAnnotation("Npgsql:Enum:user_role", "student,instructor,admin")
                .OldAnnotation("Npgsql:Enum:user_status", "active,inactive,at_risk");

            migrationBuilder.AddColumn<string>(
                name: "InternalNote",
                table: "Complaints",
                type: "text",
                nullable: true);
        }
    }
}
