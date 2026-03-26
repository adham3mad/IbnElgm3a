using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace IbnElgm3a.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        [EnumMember(Value = "student")]
        Student,
        
        [EnumMember(Value = "instructor")]
        Instructor,
        
        [EnumMember(Value = "admin")]
        Admin
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserStatus
    {
        [EnumMember(Value = "active")]
        Active,
        
        [EnumMember(Value = "inactive")]
        Inactive,
        
        [EnumMember(Value = "at_risk")]
        AtRisk
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ComplaintStatus
    {
        [EnumMember(Value = "open")]
        Open,
        
        [EnumMember(Value = "in_review")]
        InReview,
        
        [EnumMember(Value = "resolved")]
        Resolved
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ComplaintType
    {
        [EnumMember(Value = "academic")]
        Academic,
        
        [EnumMember(Value = "financial")]
        Financial,
        
        [EnumMember(Value = "technical")]
        Technical,
        
        [EnumMember(Value = "facility")]
        Facility,
        
        [EnumMember(Value = "other")]
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DayOfWeekEnum
    {
        [EnumMember(Value = "saturday")]
        Saturday,
        [EnumMember(Value = "sunday")]
        Sunday,
        [EnumMember(Value = "monday")]
        Monday,
        [EnumMember(Value = "tuesday")]
        Tuesday,
        [EnumMember(Value = "wednesday")]
        Wednesday,
        [EnumMember(Value = "thursday")]
        Thursday,
        [EnumMember(Value = "friday")]
        Friday
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ClassType
    {
        [EnumMember(Value = "lecture")]
        Lecture,
        [EnumMember(Value = "lab")]
        Lab,
        [EnumMember(Value = "tutorial")]
        Tutorial
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ScheduleRecurrence
    {
        [EnumMember(Value = "weekly")]
        Weekly,
        [EnumMember(Value = "biweekly")]
        Biweekly
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExamType
    {
        [EnumMember(Value = "midterm")]
        Midterm,
        [EnumMember(Value = "final")]
        Final
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExamStatus
    {
        [EnumMember(Value = "draft")]
        Draft,
        [EnumMember(Value = "published")]
        Published
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SubAdminScopeType
    {
        [EnumMember(Value = "university")]
        University,
        [EnumMember(Value = "faculty")]
        Faculty,
        [EnumMember(Value = "department")]
        Department
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AnnouncementTargetType
    {
        [EnumMember(Value = "all")]
        All,
        [EnumMember(Value = "faculty")]
        Faculty,
        [EnumMember(Value = "department")]
        Department,
        [EnumMember(Value = "role")]
        Role
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AnnouncementPriority
    {
        [EnumMember(Value = "normal")]
        Normal,
        [EnumMember(Value = "urgent")]
        Urgent
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CalendarEventType
    {
        [EnumMember(Value = "academic")]
        Academic,
        [EnumMember(Value = "exam")]
        Exam,
        [EnumMember(Value = "holiday")]
        Holiday,
        [EnumMember(Value = "admin")]
        Admin,
        [EnumMember(Value = "registration")]
        Registration
    }
}
