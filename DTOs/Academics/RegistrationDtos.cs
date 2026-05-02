using System.Collections.Generic;

namespace IbnElgm3a.DTOs.Academics
{
    public class DraftCourseDto
    {
        public string semester_id { get; set; } = string.Empty;
        public string course_id { get; set; } = string.Empty;
        public string section_id { get; set; } = string.Empty;
    }

    public class SubmitRegistrationDto
    {
        public string semester_id { get; set; } = string.Empty;
        public List<CourseSelectionDto> courses { get; set; } = new();
    }

    public class CourseSelectionDto
    {
        public string course_id { get; set; } = string.Empty;
        public string section_id { get; set; } = string.Empty;
    }
}
