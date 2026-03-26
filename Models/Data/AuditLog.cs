namespace IbnElgm3a.Model.Data
{
    public class AuditLog : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public virtual User? User { get; set; }

        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;

        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Description { get; set; }
    }
}
