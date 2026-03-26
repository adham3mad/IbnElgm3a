using IbnElgm3a.Enums;

namespace IbnElgm3a.Model.Data
{
    public class StudentGuardian
    {
        public string StudentId { get; set; } = string.Empty;
        public virtual Student? Student { get; set; }

        public string GuardianId { get; set; } = string.Empty;
        public virtual Guardian? Guardian { get; set; }

        public RelationType RelationType { get; set; }
        public bool IsPrimary { get; set; }
    }
}
