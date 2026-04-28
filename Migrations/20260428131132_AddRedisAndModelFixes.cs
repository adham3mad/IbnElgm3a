using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IbnElgm3a.Migrations
{
    /// <inheritdoc />
    public partial class AddRedisAndModelFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlots_Sections_CourseSectionId",
                table: "ScheduleSlots");

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
                name: "Permissions",
                table: "SubAdmins");

            migrationBuilder.RenameColumn(
                name: "CourseSectionId",
                table: "ScheduleSlots",
                newName: "SectionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleSlots_CourseSectionId",
                table: "ScheduleSlots",
                newName: "IX_ScheduleSlots_SectionId");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_type", "student,instructor")
                .Annotation("Npgsql:Enum:announcement_priority", "normal,urgent")
                .Annotation("Npgsql:Enum:announcement_target_type", "all,faculty,department,role")
                .Annotation("Npgsql:Enum:app_type", "dashboard,platform")
                .Annotation("Npgsql:Enum:calendar_event_type", "academic,exam,holiday,admin,registration")
                .Annotation("Npgsql:Enum:class_type", "lecture,lab,tutorial")
                .Annotation("Npgsql:Enum:complaint_status", "open,in_review,resolved,closed")
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
                .OldAnnotation("Npgsql:Enum:room_type", "lecture_hall,lab,tutorial_room")
                .OldAnnotation("Npgsql:Enum:schedule_recurrence", "weekly,biweekly")
                .OldAnnotation("Npgsql:Enum:seating_strategy", "alphabetical,random,by_gpa")
                .OldAnnotation("Npgsql:Enum:sub_admin_scope_type", "university,faculty,department")
                .OldAnnotation("Npgsql:Enum:user_role", "student,instructor,admin")
                .OldAnnotation("Npgsql:Enum:user_status", "active,inactive,at_risk");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegistrationEndDate",
                table: "Semesters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegistrationStartDate",
                table: "Semesters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClassType",
                table: "Sections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnrolledCount",
                table: "Sections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "HeadUserId",
                table: "Departments",
                type: "character varying(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ComplaintMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ComplaintId = table.Column<string>(type: "character varying(50)", nullable: false),
                    SenderId = table.Column<string>(type: "character varying(50)", nullable: false),
                    SenderRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    AttachmentsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintMessages_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplaintMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActionUrl = table.Column<string>(type: "text", nullable: true),
                    StudentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationDrafts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SemesterId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationDrafts_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrationDrafts_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SemesterId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RefCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewerNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationRequests_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrationRequests_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationDraftCourses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DraftId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CourseId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SectionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationDraftCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationDraftCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrationDraftCourses_RegistrationDrafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "RegistrationDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrationDraftCourses_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationRequestCourses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CourseId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SectionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationRequestCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationRequestCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrationRequestCourses_RegistrationRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "RegistrationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrationRequestCourses_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadUserId",
                table: "Departments",
                column: "HeadUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintMessages_ComplaintId",
                table: "ComplaintMessages",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintMessages_SenderId",
                table: "ComplaintMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_StudentId",
                table: "Notifications",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationDraftCourses_CourseId",
                table: "RegistrationDraftCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationDraftCourses_DraftId",
                table: "RegistrationDraftCourses",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationDraftCourses_SectionId",
                table: "RegistrationDraftCourses",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationDrafts_SemesterId",
                table: "RegistrationDrafts",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationDrafts_StudentId",
                table: "RegistrationDrafts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequestCourses_CourseId",
                table: "RegistrationRequestCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequestCourses_RequestId",
                table: "RegistrationRequestCourses",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequestCourses_SectionId",
                table: "RegistrationRequestCourses",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_SemesterId",
                table: "RegistrationRequests",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_StudentId",
                table: "RegistrationRequests",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_HeadUserId",
                table: "Departments",
                column: "HeadUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlots_Sections_SectionId",
                table: "ScheduleSlots",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_HeadUserId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlots_Sections_SectionId",
                table: "ScheduleSlots");

            migrationBuilder.DropTable(
                name: "ComplaintMessages");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RegistrationDraftCourses");

            migrationBuilder.DropTable(
                name: "RegistrationRequestCourses");

            migrationBuilder.DropTable(
                name: "RegistrationDrafts");

            migrationBuilder.DropTable(
                name: "RegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_Departments_HeadUserId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "RegistrationEndDate",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "RegistrationStartDate",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "ClassType",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "EnrolledCount",
                table: "Sections");

            migrationBuilder.RenameColumn(
                name: "SectionId",
                table: "ScheduleSlots",
                newName: "CourseSectionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleSlots_SectionId",
                table: "ScheduleSlots",
                newName: "IX_ScheduleSlots_CourseSectionId");

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
                .OldAnnotation("Npgsql:Enum:complaint_status", "open,in_review,resolved,closed")
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
                name: "DepartmentId1",
                table: "Users",
                type: "character varying(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Permissions",
                table: "SubAdmins",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "HeadUserId",
                table: "Departments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId1",
                table: "Users",
                column: "DepartmentId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlots_Sections_CourseSectionId",
                table: "ScheduleSlots",
                column: "CourseSectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId1",
                table: "Users",
                column: "DepartmentId1",
                principalTable: "Departments",
                principalColumn: "Id");
        }
    }
}
