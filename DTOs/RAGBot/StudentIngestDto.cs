using System.Collections.Generic;

namespace IbnElgm3a.DTOs.RAGBot
{
    public class StudentIngestDto
    {
        public string Student_Id { get; set; } = string.Empty;
        public string Faculty_Id { get; set; } = string.Empty;
        public StudentDataDto Data { get; set; } = new();
    }

    public class StudentDataDto
    {
        public string? Name { get; set; }
        public float? Gpa { get; set; }
        public int? Level { get; set; }
        public string? Department { get; set; }
        public int? Completed_Hours { get; set; }
        public int? Remaining_Hours { get; set; }
        public int? Academic_Warnings { get; set; }
        public string? Enrollment_Status { get; set; }
        public Dictionary<string, string>? Grades { get; set; }
        public List<string>? Completed_Courses { get; set; }
        public List<string>? Current_Courses { get; set; }
        public Dictionary<string, object>? Extra { get; set; }
    }
}
