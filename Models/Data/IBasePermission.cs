using IbnElgm3a.Enums;

namespace IbnElgm3a.Models.Data
{
    public interface IBasePermission
    {
        Guid Id { get; set; }
        string Name { get; set; }
        int? Method { get; set; }
        PermissionEnum Code { get; set; }
        string? Description { get; set; }
        string? Ar_Name { get; set; }
        string? Ar_Description { get; set; }
        DateTime CreatedAt { get; set; }
        int FeatureId { get; set; }
    }
}
