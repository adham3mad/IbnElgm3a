using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Models.Data
{
    public static class PermissionSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            var enumValues = Enum.GetValues<PermissionEnum>();

            foreach (var code in enumValues)
            {
                var codeStr = code.ToString();
                var parts = codeStr.Split('_');
                
                if (parts.Length < 2) continue;

                var moduleName = parts[0]; // Dashboard or Platform
                var featureName = parts[1]; // Announcements, Users, etc.
                var operationName = parts.Length > 2 ? parts[2] : "Read";

                // Ensure Feature exists
                var feature = await context.Features
                    .FirstOrDefaultAsync(f => f.Name == featureName);

                if (feature == null)
                {
                    feature = new Feature
                    {
                        Name = featureName,
                        NameAr = TranslateFeature(featureName),
                        Type = moduleName == "Dashboard" ? AppType.Dashboard : AppType.Platform,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Features.Add(feature);
                    await context.SaveChangesAsync();
                }

                // Ensure Permission exists
                var exists = await context.Permissions.AnyAsync(p => p.Code == code);
                if (!exists)
                {
                    context.Permissions.Add(new Permission
                    {
                        Id = Guid.NewGuid(),
                        Code = code,
                        Name = operationName,
                        Ar_Name = TranslateOperation(operationName),
                        FeatureId = feature.Id,
                        CreatedAt = DateTime.UtcNow,
                        Description = $"Allow {operationName} on {featureName}"
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        private static string TranslateFeature(string name)
        {
            return name switch
            {
                "Users" => "المستخدمين",
                "Roles" => "الأدوار",
                "Permissions" => "الصلاحيات",
                "Announcements" => "الإعلانات",
                "Courses" => "المواد الدراسية",
                "Faculties" => "الكليات",
                "Departments" => "الأقسام",
                "Students" => "الطلاب",
                "Instructors" => "المحاضرين",
                "Complaints" => "الشكاوى",
                "Calendar" => "التقويم",
                "Exams" => "الاختبارات",
                "Schedule" => "الجداول",
                "Rooms" => "القاعات",
                "Settings" => "إعدادات النظام",
                "AuditLogs" => "سجل الأحداث",
                "SubAdmins" => "مديري النظام الفرعي",
                "Enrollments" => "التسجيلات",
                "Sections" => "الشُعب",
                "Semesters" => "الفصول الدراسية",
                "Grades" => "الدرجات",
                "Guardians" => "أولياء الأمور",
                _ => name
            };
        }

        private static string TranslateOperation(string op)
        {
            return op switch
            {
                "Read" => "عرض",
                "Create" => "إضافة",
                "Update" => "تعديل",
                "Delete" => "حذف",
                "Export" => "تصدير",
                "Import" => "استيراد",
                "UpdateStatus" => "تعديل الحالة",
                _ => op
            };
        }
    }
}
