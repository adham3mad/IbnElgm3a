using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace IbnElgm3a.Services.Localization
{
    public class LocalizationService : ILocalizationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalizationService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private static readonly Dictionary<string, Dictionary<string, string>> Messages = new()
        {
            {
                "ar", new Dictionary<string, string>
                {
                    // Auth
                    { "INVALID_CREDENTIALS", "بيانات الدخول غير صحيحة" },
                    { "USER_NOT_FOUND", "لم يتم العثور على المستخدم" },
                    { "ACCOUNT_INACTIVE", "تم إيقاف حسابك. يرجى مراجعة الإدارة" },
                    { "PASSWORD_UPDATED", "تم تحديث كلمة المرور بنجاح" },
                    { "INVALID_TOKEN", "الرمز غير صالح أو منتهي الصلاحية" },
                    { "INVALID_OTP", "رمز التحقق غير صالح أو منتهي الصلاحية" },
                    { "REGISTRATION_FAILED", "حدث خطأ أثناء تسجيل الجهاز" },
                    { "UNAUTHORIZED", "غير مصرح لك بالوصول" },
                    { "FORBIDDEN", "ليس لديك الصلاحية المطلوبة لتنفيذ العملية" },
                    
                    // Users
                    { "DUPLICATE_EMAIL", "البريد الإلكتروني مسجل مسبقاً" },
                    { "USER_UPDATED", "تم تحديث بيانات المستخدم بنجاح" },
                    
                    // Entities (Courses, Departments...)
                    { "DUPLICATE_COURSE_CODE", "رمز المقرر مسجل مسبقاً" },
                    { "COURSE_NOT_FOUND", "لم يتم العثور على المقرر" },
                    { "FACULTY_NOT_FOUND", "لم يتم العثور على الكلية" },
                    { "DEPARTMENT_NOT_FOUND", "لم يتم العثور على القسم" },
                    { "INVALID_DEPARTMENT_FOR_FACULTY", "القسم المختار لا ينتمي للكلية المحددة" },
                    { "SUBADMIN_NOT_FOUND", "لم يتم العثور على المشرف" },
                    { "SUBADMIN_EXISTS", "المستخدم مسجل ومشرف بالفعل" },
                    { "ROLE_NOT_FOUND", "الدور غير موجود" },
                    { "ROLE_ASSIGNED_TO_USER", "هذا الدور مرتبط بمستخدمين بالفعل" },
                    
                    // Helpdesk
                    { "COMPLAINT_NOT_FOUND", "الشكوى غير موجودة" },
                    { "COMPLAINT_UPDATED", "تم تحديث حالة الشكوى بنجاح" },
                    
                    // Academic
                    { "EXAM_NOT_FOUND", "الامتحان غير موجود" },
                    { "EVENT_NOT_FOUND", "الحدث غير موجود" },
                    { "ANNOUNCEMENT_NOT_FOUND", "لم يتم العثور على الإعلان" },
                    { "SEMESTER_NOT_FOUND", "لم يتم العثور على الفصل الدراسي" },
                    { "SLOT_NOT_FOUND", "لم يتم العثور على الفترة الزمنية" },
                    { "HALL_NOT_FOUND", "لم يتم العثور على القاعة" },
                    { "SCHEDULE_CONFLICT", "يوجد تعارض في الجدول الزمني لهذه القاعة" },
                    
                    // Specific Errors
                    { "DUPLICATE_NAME", "الاسم مسجل مسبقاً" },
                    { "DUPLICATE_NATIONAL_ID", "الرقم القومي مسجل مسبقاً" },
                    { "DUPLICATE_COURSE_TITLE", "اسم المقرر مسجل مسبقاً" },
                    { "DUPLICATE_FAC_CODE", "رمز الكلية مسجل مسبقاً" },
                    { "DUPLICATE_DEP_CODE", "رمز القسم مسجل مسبقاً" },
                    { "FACULTY_NOT_EMPTY", "لا يمكن حذف كلية بها أقسام" },
                    { "DEPARTMENT_NOT_EMPTY", "لا يمكن حذف قسم به طلاب أو مدرسون" },
                    { "FILE_EMPTY", "الملف فارغ" },
                    { "JOB_NOT_FOUND", "لم يتم العثور على المهمة" },
                    { "USER_ID_REQUIRED", "معرف المستخدم مطلوب" },
                    { "INTERNAL_FACULTY_CODE_MISSING", "رمز الكلية غير موجود في قاعدة البيانات" },
                    { "INVALID_SIGNATURE", "فشل التحقق من التوقيع البيومتري" },
                    { "INVALID_PASSWORD", "كلمة المرور الحالية غير صحيحة" },
                    { "FILE_INVALID_TYPE", "نوع الملف غير صالح. المسموح: .jpg, .jpeg, .png" },
                    { "FILE_TOO_LARGE", "حجم الملف كبير جداً. الحد الأقصى: 2 ميجابايت" },
                    { "PASSWORD_MISSING_UPPER", "يجب أن تحتوي كلمة المرور على حرف كبير واحد على الأقل." },
                    { "PASSWORD_MISSING_LOWER", "يجب أن تحتوي كلمة المرور على حرف صغير واحد على الأقل." },
                    { "PASSWORD_MISSING_DIGIT", "يجب أن تحتوي كلمة المرور على رقم واحد على الأقل." },
                    { "PASSWORD_MISSING_SPECIAL", "يجب أن تحتوي كلمة المرور على رمز خاص واحد على الأقل." },
                    { "PASSWORD_TOO_SHORT", "يجب أن تكون كلمة المرور 10 أحرف على الأقل." },
                    
                    // Generic
                    { "CREATED_SUCCESS", "تمت الإضافة بنجاح" },
                    { "UPDATED_SUCCESS", "تم التحديث بنجاح" },
                    { "DELETED_SUCCESS", "تم الحذف بنجاح" },
                    { "PASSWORD_CHANGED_SUCCESS", "تم تغيير كلمة المرور بنجاح" },
                    { "SAME_PASSWORD_NOT_ALLOWED", "لا يمكن استخدام كلمة المرور الحالية ككلمة مرور جديدة" },
                    { "EXAM_PUBLISHED", "تم نشر الامتحان بنجاح" },
                    { "ANNOUNCEMENT_UPDATED", "تم تحديث الإعلان بنجاح" },
                    { "ANNOUNCEMENT_DELETED", "تم حذف الإعلان بنجاح" }
                }
            },
            {
                "en", new Dictionary<string, string>
                {
                    { "INVALID_CREDENTIALS", "Invalid credentials provided." },
                    { "USER_NOT_FOUND", "User not found." },
                    { "ACCOUNT_INACTIVE", "Account is inactive. Please contact support." },
                    { "PASSWORD_UPDATED", "Password updated successfully." },
                    { "INVALID_TOKEN", "The reset token is invalid or expired." },
                    { "INVALID_OTP", "The OTP code is invalid or expired." },
                    { "REGISTRATION_FAILED", "Failed to register device." },
                    { "UNAUTHORIZED", "Unauthorized access." },
                    { "FORBIDDEN", "You do not have the required permissions." },
                    
                    { "DUPLICATE_EMAIL", "Email already exists." },
                    { "USER_UPDATED", "User updated successfully." },
                    
                    { "DUPLICATE_COURSE_CODE", "Course Code already exists." },
                    { "COURSE_NOT_FOUND", "Course not found." },
                    { "FACULTY_NOT_FOUND", "Faculty not found." },
                    { "DEPARTMENT_NOT_FOUND", "Department not found." },
                    { "INVALID_DEPARTMENT_FOR_FACULTY", "The selected department does not belong to this faculty." },
                    { "SUBADMIN_NOT_FOUND", "Sub-Admin not found." },
                    { "SUBADMIN_EXISTS", "User is already a Sub-Admin." },
                    { "ROLE_NOT_FOUND", "Role not found." },
                    { "ROLE_ASSIGNED_TO_USER", "Role is already assigned to a user." },
                    
                    { "COMPLAINT_NOT_FOUND", "Complaint not found." },
                    { "COMPLAINT_UPDATED", "Complaint updated successfully." },
                    
                    { "EXAM_NOT_FOUND", "Exam not found." },
                    { "EVENT_NOT_FOUND", "Calendar event not found." },
                    { "ANNOUNCEMENT_NOT_FOUND", "Announcement not found." },
                    { "SEMESTER_NOT_FOUND", "Semester not found." },
                    { "SLOT_NOT_FOUND", "Schedule slot not found." },
                    { "HALL_NOT_FOUND", "Examination hall not found." },
                    { "SCHEDULE_CONFLICT", "There is a schedule conflict for this room." },

                    { "DUPLICATE_NAME", "Name already exists." },
                    { "DUPLICATE_NATIONAL_ID", "National ID already exists." },
                    { "DUPLICATE_COURSE_TITLE", "Course title already exists." },
                    { "DUPLICATE_FAC_CODE", "Faculty manual code already exists." },
                    { "DUPLICATE_DEP_CODE", "Department manual code already exists." },
                    { "FACULTY_NOT_EMPTY", "Cannot delete faculty with existing departments." },
                    { "DEPARTMENT_NOT_EMPTY", "Cannot delete department with existing students/instructors." },
                    { "FILE_EMPTY", "File is empty" },
                    { "JOB_NOT_FOUND", "Job not found" },
                    { "USER_ID_REQUIRED", "User Id is required" },
                    { "INTERNAL_FACULTY_CODE_MISSING", "Faculty code is missing in database" },
                    { "INVALID_SIGNATURE", "Biometric signature verification failed." },
                    { "INVALID_PASSWORD", "Current password is incorrect." },
                    { "FILE_INVALID_TYPE", "Invalid file type. Allowed: .jpg, .jpeg, .png" },
                    { "FILE_TOO_LARGE", "File is too large. Maximum size: 2MB" },
                    { "PASSWORD_MISSING_UPPER", "Password must contain at least one uppercase letter." },
                    { "PASSWORD_MISSING_LOWER", "Password must contain at least one lowercase letter." },
                    { "PASSWORD_MISSING_DIGIT", "Password must contain at least one digit." },
                    { "PASSWORD_MISSING_SPECIAL", "Password must contain at least one special character." },
                    { "PASSWORD_TOO_SHORT", "Password must be at least 10 characters long." },
                    
                    { "CREATED_SUCCESS", "Created successfully." },
                    { "UPDATED_SUCCESS", "Updated successfully." },
                    { "DELETED_SUCCESS", "Deleted successfully." },
                    { "PASSWORD_CHANGED_SUCCESS", "Password changed successfully." },
                    { "SAME_PASSWORD_NOT_ALLOWED", "New password cannot be the same as the current password." },
                    { "EXAM_PUBLISHED", "Exam published successfully." },
                    { "ANNOUNCEMENT_UPDATED", "Announcement updated successfully." },
                    { "ANNOUNCEMENT_DELETED", "Announcement deleted successfully." }
                }
            }
        };

        public string GetMessage(string key)
        {
            var language = "ar"; // default

            if (_httpContextAccessor.HttpContext != null && 
                _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Accept-Language", out var acceptLanguage))
            {
                var lang = acceptLanguage.ToString().Split(',')[0].Trim().ToLower();
                if (lang.StartsWith("en")) language = "en";
            }

            if (Messages.TryGetValue(language, out var localizedDict))
            {
                if (localizedDict.TryGetValue(key, out var localizedStr))
                {
                    return localizedStr;
                }
            }

            return key; // Fallback to raw key if missing
        }
    }
}
