using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace IbnElgm3a.Enums
{
    public class EnumMemberTypeConverter<T> : TypeConverter where T : struct, Enum
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string str)
            {
                foreach (var field in typeof(T).GetFields())
                {
                    var attr = field.GetCustomAttribute<EnumMemberAttribute>();
                    if (attr != null && string.Equals(attr.Value, str, StringComparison.OrdinalIgnoreCase))
                    {
                        return field.GetValue(null);
                    }
                    if (string.Equals(field.Name, str, StringComparison.OrdinalIgnoreCase))
                    {
                        return field.GetValue(null);
                    }
                }
                if (Enum.TryParse<T>(str, true, out var result))
                {
                    return result;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
    }

    public class UserRoleConverter : EnumMemberTypeConverter<UserRole> {}
    public class UserStatusConverter : EnumMemberTypeConverter<UserStatus> {}
    public class ComplaintStatusConverter : EnumMemberTypeConverter<ComplaintStatus> {}
    public class ComplaintTypeConverter : EnumMemberTypeConverter<ComplaintType> {}
    public class DayOfWeekEnumConverter : EnumMemberTypeConverter<DayOfWeekEnum> {}
    public class ClassTypeConverter : EnumMemberTypeConverter<ClassType> {}
    public class ScheduleRecurrenceConverter : EnumMemberTypeConverter<ScheduleRecurrence> {}
    public class ExamTypeConverter : EnumMemberTypeConverter<ExamType> {}
    public class ExamStatusConverter : EnumMemberTypeConverter<ExamStatus> {}
    public class SubAdminScopeTypeConverter : EnumMemberTypeConverter<SubAdminScopeType> {}
    public class AnnouncementTargetTypeConverter : EnumMemberTypeConverter<AnnouncementTargetType> {}
    public class AnnouncementPriorityConverter : EnumMemberTypeConverter<AnnouncementPriority> {}
    public class CalendarEventTypeConverter : EnumMemberTypeConverter<CalendarEventType> {}
    public class SeatingStrategyConverter : EnumMemberTypeConverter<SeatingStrategy> {}
    public class RoomTypeConverter : EnumMemberTypeConverter<RoomType> {}
    public class QuizStatusConverter : EnumMemberTypeConverter<QuizStatus> {}
    public class AssignmentStatusConverter : EnumMemberTypeConverter<AssignmentStatus> {}
    public class SubmissionStatusConverter : EnumMemberTypeConverter<SubmissionStatus> {}
    public class InstructorCourseStatusConverter : EnumMemberTypeConverter<InstructorCourseStatus> {}
    public class RosterRiskStatusConverter : EnumMemberTypeConverter<RosterRiskStatus> {}
    public class NotificationTypeConverter : EnumMemberTypeConverter<NotificationType> {}

    [TypeConverter(typeof(UserRoleConverter))]
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

    [TypeConverter(typeof(UserStatusConverter))]
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

    [TypeConverter(typeof(ComplaintStatusConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ComplaintStatus
    {
        [EnumMember(Value = "open")]
        Open,
        
        [EnumMember(Value = "in_review")]
        InReview,
        
        [EnumMember(Value = "resolved")]
        Resolved,
        
        [EnumMember(Value = "closed")]
        Closed
    }

    [TypeConverter(typeof(ComplaintTypeConverter))]
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

    [TypeConverter(typeof(DayOfWeekEnumConverter))]
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

    [TypeConverter(typeof(ClassTypeConverter))]
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

    [TypeConverter(typeof(ScheduleRecurrenceConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ScheduleRecurrence
    {
        [EnumMember(Value = "weekly")]
        Weekly,
        [EnumMember(Value = "biweekly")]
        Biweekly
    }

    [TypeConverter(typeof(ExamTypeConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExamType
    {
        [EnumMember(Value = "midterm")]
        Midterm,
        [EnumMember(Value = "final")]
        Final
    }

    [TypeConverter(typeof(ExamStatusConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExamStatus
    {
        [EnumMember(Value = "draft")]
        Draft,
        [EnumMember(Value = "published")]
        Published
    }

    [TypeConverter(typeof(SubAdminScopeTypeConverter))]
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

    [TypeConverter(typeof(AnnouncementTargetTypeConverter))]
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

    [TypeConverter(typeof(AnnouncementPriorityConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AnnouncementPriority
    {
        [EnumMember(Value = "normal")]
        Normal,
        [EnumMember(Value = "urgent")]
        Urgent
    }

    [TypeConverter(typeof(CalendarEventTypeConverter))]
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

    [TypeConverter(typeof(SeatingStrategyConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SeatingStrategy
    {
        [EnumMember(Value = "alphabetical")]
        Alphabetical,
        [EnumMember(Value = "random")]
        Random,
        [EnumMember(Value = "by_gpa")]
        ByGpa
    }

    [TypeConverter(typeof(RoomTypeConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoomType
    {
        [EnumMember(Value = "lecture_hall")]
        LectureHall,
        [EnumMember(Value = "lab")]
        Lab,
        [EnumMember(Value = "tutorial_room")]
        TutorialRoom
    }

    [TypeConverter(typeof(QuizStatusConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum QuizStatus
    {
        [EnumMember(Value = "draft")]
        Draft,
        [EnumMember(Value = "published")]
        Published,
        [EnumMember(Value = "closed")]
        Closed
    }

    [TypeConverter(typeof(AssignmentStatusConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AssignmentStatus
    {
        [EnumMember(Value = "draft")]
        Draft,
        [EnumMember(Value = "published")]
        Published,
        [EnumMember(Value = "closed")]
        Closed
    }

    [TypeConverter(typeof(SubmissionStatusConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SubmissionStatus
    {
        [EnumMember(Value = "submitted")]
        Submitted,
        [EnumMember(Value = "graded")]
        Graded,
        [EnumMember(Value = "late")]
        Late,
        [EnumMember(Value = "missing")]
        Missing
    }

    [TypeConverter(typeof(InstructorCourseStatusConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InstructorCourseStatus
    {
        [EnumMember(Value = "active")]
        Active,
        [EnumMember(Value = "all")]
        All
    }

    [TypeConverter(typeof(RosterRiskStatusConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RosterRiskStatus
    {
        [EnumMember(Value = "good")]
        Good,
        [EnumMember(Value = "watch")]
        Watch,
        [EnumMember(Value = "at_risk")]
        AtRisk
    }

    [TypeConverter(typeof(NotificationTypeConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NotificationType
    {
        [EnumMember(Value = "announcement")]
        Announcement,
        [EnumMember(Value = "exam")]
        Exam,
        [EnumMember(Value = "complaint")]
        Complaint,
        [EnumMember(Value = "schedule")]
        Schedule,
        [EnumMember(Value = "registration")]
        Registration
    }
}
