using System.Text.Json.Serialization;

namespace IbnElgm3a.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PermissionEnum
    {
        // Dashboard Platform
        Dashboard_Main_Read = 1,

        // Announcements
        Dashboard_Announcements_Read = 10,
        Dashboard_Announcements_Create = 11,
        Dashboard_Announcements_Update = 12,
        Dashboard_Announcements_Delete = 13,

        // Audit Logs
        Dashboard_AuditLogs_Read = 20,

        // Calendar
        Dashboard_Calendar_Read = 30,
        Dashboard_Calendar_Create = 31,
        Dashboard_Calendar_Update = 32,
        Dashboard_Calendar_Delete = 33,

        // Complaints
        Dashboard_Complaints_Read = 40,
        Dashboard_Complaints_Update = 41,
        Dashboard_Complaints_Delete = 42,

        // Courses
        Dashboard_Courses_Read = 50,
        Dashboard_Courses_Create = 51,
        Dashboard_Courses_Update = 52,
        Dashboard_Courses_Delete = 53,

        // Departments
        Dashboard_Departments_Read = 60,
        Dashboard_Departments_Create = 61,
        Dashboard_Departments_Update = 62,
        Dashboard_Departments_Delete = 63,

        // Enrollments
        Dashboard_Enrollments_Read = 70,
        Dashboard_Enrollments_Create = 71,
        Dashboard_Enrollments_Update = 72,
        Dashboard_Enrollments_Delete = 73,

        // Exams
        Dashboard_Exams_Read = 80,
        Dashboard_Exams_Create = 81,
        Dashboard_Exams_Update = 82,
        Dashboard_Exams_Delete = 83,

        // Faculties
        Dashboard_Faculties_Read = 90,
        Dashboard_Faculties_Create = 91,
        Dashboard_Faculties_Update = 92,
        Dashboard_Faculties_Delete = 93,

        // Features
        Dashboard_Features_Read = 100,

        // Grades
        Dashboard_Grades_Read = 110,
        Dashboard_Grades_Update = 111,

        // Guardians
        Dashboard_Guardians_Read = 120,
        Dashboard_Guardians_Update = 121,
        Dashboard_Guardians_Delete = 122,

        // Instructors
        Dashboard_Instructors_Read = 130,
        Dashboard_Instructors_Create = 131,
        Dashboard_Instructors_Update = 132,
        Dashboard_Instructors_Delete = 133,

        // Permissions
        Dashboard_Permissions_Read = 140,

        // Reports
        Dashboard_Reports_Read = 150,
        Dashboard_Reports_Export = 151,

        // Roles
        Dashboard_Roles_Read = 160,
        Dashboard_Roles_Create = 161,
        Dashboard_Roles_Update = 162,
        Dashboard_Roles_Delete = 163,

        // Rooms
        Dashboard_Rooms_Read = 170,
        Dashboard_Rooms_Create = 171,
        Dashboard_Rooms_Update = 172,
        Dashboard_Rooms_Delete = 173,

        // Schedule
        Dashboard_Schedule_Read = 180,
        Dashboard_Schedule_Create = 181,
        Dashboard_Schedule_Update = 182,
        Dashboard_Schedule_Delete = 183,

        // Sections
        Dashboard_Sections_Read = 190,
        Dashboard_Sections_Create = 191,
        Dashboard_Sections_Update = 192,
        Dashboard_Sections_Delete = 193,

        // Semesters
        Dashboard_Semesters_Read = 200,
        Dashboard_Semesters_Create = 201,
        Dashboard_Semesters_Update = 202,
        Dashboard_Semesters_Delete = 203,

        // Settings
        Dashboard_Settings_Read = 210,
        Dashboard_Settings_Update = 211,

        // Students
        Dashboard_Students_Read = 220,
        Dashboard_Students_Create = 221,
        Dashboard_Students_Update = 222,
        Dashboard_Students_Delete = 223,

        // SubAdmins
        Dashboard_SubAdmins_Read = 230,
        Dashboard_SubAdmins_Create = 231,
        Dashboard_SubAdmins_Update = 232,
        Dashboard_SubAdmins_Delete = 233,

        // Users
        Dashboard_Users_Read = 240,
        Dashboard_Users_Create = 241,
        Dashboard_Users_Update = 242,
        Dashboard_Users_Delete = 243,
        Dashboard_Users_UpdateStatus = 244,
        Dashboard_Users_Import = 245,

        // Platform / App
        Platform_Profile_Read = 1000,
        Platform_Profile_Update = 1001,
        Platform_Courses_Enrolled = 1010
    }
}
