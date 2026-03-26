using IbnElgm3a.Enums;
using System;

namespace IbnElgm3a.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class RequirePermissionAttribute : Attribute
    {
        public PermissionEnum PermissionCode { get; }

        public RequirePermissionAttribute(PermissionEnum permission)
        {
            PermissionCode = permission;
        }
    }
}
