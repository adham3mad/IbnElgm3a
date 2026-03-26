using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IbnElgm3a.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

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
                name: "RoleId",
                table: "Users",
                type: "character varying(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DescriptionAr = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: true),
                    Code = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Ar_Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Ar_Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FeatureId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    PermissionsId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolesId = table.Column<string>(type: "character varying(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.PermissionsId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionsId",
                        column: x => x.PermissionsId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_FeatureId",
                table: "Permissions",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RolesId",
                table: "RolePermissions",
                column: "RolesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_type", "student,instructor")
                .Annotation("Npgsql:Enum:announcement_priority", "normal,urgent")
                .Annotation("Npgsql:Enum:announcement_target_type", "all,faculty,department,role")
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
                .OldAnnotation("Npgsql:Enum:schedule_recurrence", "weekly,biweekly")
                .OldAnnotation("Npgsql:Enum:sub_admin_scope_type", "university,faculty,department")
                .OldAnnotation("Npgsql:Enum:user_role", "student,instructor,admin")
                .OldAnnotation("Npgsql:Enum:user_status", "active,inactive,at_risk");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
