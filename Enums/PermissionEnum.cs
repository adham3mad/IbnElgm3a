using System.Text.Json.Serialization;

namespace IbnElgm3a.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PermissionEnum
    {
        // General / Dashboard
        Dashboard_Read = 1,

        // Users
        Dashboard_UsersRead = 10,
        Dashboard_UsersCreate = 11,
        Dashboard_UsersUpdate = 12,
        Dashboard_UsersDelete = 13,
        Dashboard_UsersImport = 14,
        Dashboard_UsersUpdateStatus = 15,

        Dashboard_StudentsCreate = 16,
        Dashboard_InstructorsCreate = 17,
        Dashboard_StudentsUpdate = 18,
        Dashboard_InstructorsUpdate = 19,
        
        DashboardUsersRead = 20, 
        DashboardUsersCreate = 21, 
        DashboardUsersUpdate = 22, 
        DashboardUsersDelete = 23, 
        
        // Roles
        Dashboard_RolesRead = 30,
        Dashboard_RolesCreate = 31,
        Dashboard_RolesUpdate = 32,
        Dashboard_RolesDelete = 33,

        // Features
        Dashboard_FeaturesRead = 40,

        // Announcements
        Dashboard_AnnouncementsRead = 50,
        Dashboard_AnnouncementsCreate = 51,
        Dashboard_AnnouncementsUpdate = 52,
        Dashboard_AnnouncementsDelete = 53,

        // Calendar
        Dashboard_CalendarRead = 60,
        Dashboard_CalendarCreate = 61,
        Dashboard_CalendarDelete = 62,
        Dashboard_CalendarUpdate = 63,

        // Complaints
        Dashboard_ComplaintsRead = 70,
        Dashboard_ComplaintsUpdate = 71,
        Dashboard_ComplaintsDelete = 72,

        // Academic
        Dashboard_CoursesRead = 80,
        Dashboard_CoursesCreate = 81,
        Dashboard_CoursesUpdate = 82,
        Dashboard_CoursesDelete = 83,

        Dashboard_ExamsRead = 90,
        Dashboard_ExamsCreate = 91,
        Dashboard_ExamsUpdate = 92,
        Dashboard_ExamsDelete = 93,

        Dashboard_ScheduleRead = 100,
        Dashboard_ScheduleCreate = 101,
        Dashboard_ScheduleUpdate = 102,
        Dashboard_ScheduleDelete = 103,

        // Structure
        Dashboard_StructureRead = 110,
        Dashboard_StructureCreate = 111,
        Dashboard_StructureUpdate = 112,
        Dashboard_StructureDelete = 113,

        // Settings & Reports
        Dashboard_SettingsRead = 120,
        Dashboard_SettingsUpdate = 121,
        Dashboard_ReportsRead = 130,
        Dashboard_ReportsExport = 131,

        // Platform
        Platform_ProfileRead = 1000,
        Platform_ProfileUpdate = 1001,
        Platform_CoursesEnrolled = 1010
    }
}
