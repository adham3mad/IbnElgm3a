using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace IbnElgm3a.Controllers.Students
{
    [ApiController]
    [Route("student/registration")]
    [Authorize]
    public class StudentRegistrationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IbnElgm3a.Services.Localization.ILocalizationService _localizer;

        public StudentRegistrationController(AppDbContext context, IbnElgm3a.Services.Localization.ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet("window")]
        public async Task<IActionResult> GetRegistrationWindow()
        {
            var now = DateTimeOffset.UtcNow;
            var nextSemester = await _context.Semesters
                .Where(s => s.StartDate > now)
                .OrderBy(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (nextSemester == null)
            {
                return Ok(new
                {
                    is_open = false,
                    student_eligible = false,
                    ineligibility_reason = _localizer.GetMessage("NO_UPCOMING_SEMESTER")
                });
            }

            var isOpen = (nextSemester.RegistrationStartDate <= now && nextSemester.RegistrationEndDate >= now);
            var closesInHours = (isOpen && nextSemester.RegistrationEndDate.HasValue) ? (int)(nextSemester.RegistrationEndDate.Value - now).TotalHours : 0;

            var userId = GetUserId();
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
            
            bool isEligible = true;
            string? ineligibilityReason = null;

            if (student?.User?.Status != IbnElgm3a.Enums.UserStatus.Active)
            {
                isEligible = false;
                ineligibilityReason = _localizer.GetMessage("INACTIVE_ACCOUNT");
            }
            else if (await _context.RegistrationRequests.AnyAsync(r => r.StudentId == student.Id && r.SemesterId == nextSemester.Id && (r.Status == "pending" || r.Status == "approved")))
            {
                isEligible = false;
                ineligibilityReason = _localizer.GetMessage("ALREADY_SUBMITTED");
            }

            return Ok(new
            {
                is_open = isOpen,
                semester_id = nextSemester.Id,
                semester_name = nextSemester.Name,
                start_date = nextSemester.RegistrationStartDate,
                end_date = nextSemester.RegistrationEndDate,
                closes_in_hours = closesInHours,
                min_credit_hours = 9,
                max_credit_hours = 18,
                student_eligible = isEligible,
                eligibility_reason = isEligible ? null : ineligibilityReason,
                ineligibility_reason = ineligibilityReason
            });
        }

        [HttpGet("available-courses")]
        public async Task<IActionResult> GetAvailableCourses([FromQuery] string semester_id, [FromQuery] string? type = null, [FromQuery] string? search = null, [FromQuery] bool available_only = false, [FromQuery] int page = 1, [FromQuery] int per_page = 20)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var completedCredits = await _context.Enrollments
                .Include(e => e.Section).ThenInclude(s => s!.Course)
                .Include(e => e.Grade)
                .Where(e => e.StudentId == student.Id && e.Grade != null && e.Grade.LetterGrade != IbnElgm3a.Enums.LetterGrade.F)
                .SumAsync(e => e.Section!.Course!.CreditHours);

            var query = _context.Courses
                .Include(c => c.Sections.Where(sec => sec.SemesterId == semester_id))
                    .ThenInclude(sec => sec.ScheduleSlots)
                        .ThenInclude(ss => ss.Room)
                .Include(c => c.Sections)
                    .ThenInclude(sec => sec.Instructor)
                        .ThenInclude(i => i!.User)
                .Where(c => c.DepartmentId == student.DepartmentId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var ls = search.ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(ls) || c.CourseCode.ToLower().Contains(ls));
            }

            var courses = await query.ToListAsync();

            var result = new
            {
                semester_id = semester_id,
                student_credit_hours_completed = completedCredits,
                courses = courses.Select(c => new
                {
                    id = c.Id,
                    code = c.CourseCode,
                    name = c.Title,
                    credit_hours = c.CreditHours,
                    type = "core",
                    description = c.Description,
                    prerequisites_met = true, // Simplified for now, usually requires historical grades check
                    prerequisites = new List<object>(),
                    sections = c.Sections.Select(sec => new
                    {
                        id = sec.Id,
                        label = sec.Name,
                        instructor = sec.Instructor?.User?.Name ?? "TBD",
                        capacity = sec.Capacity,
                        enrolled = sec.EnrolledCount,
                        available_seats = sec.Capacity - sec.EnrolledCount,
                        availability_status = (sec.Capacity - sec.EnrolledCount) <= 0 ? "full" : (sec.Capacity - sec.EnrolledCount) <= 10 ? "high_demand" : "available",
                        schedule_slots = sec.ScheduleSlots.Select(ss => new
                        {
                            day = ss.Day.ToString(),
                            start_time = ss.StartTime,
                            end_time = ss.EndTime,
                            room = ss.Room?.Name ?? "TBD",
                            type = sec.ClassType.ToString().ToLower()
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            return Ok(result);
        }

        public class DraftCourseDto
        {
            public string semester_id { get; set; } = string.Empty;
            public string course_id { get; set; } = string.Empty;
            public string section_id { get; set; } = string.Empty;
        }

        [HttpPost("draft/courses")]
        public async Task<IActionResult> AddCourseToDraft([FromBody] DraftCourseDto dto)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var draft = await _context.RegistrationDrafts
                .Include(d => d.Courses)
                    .ThenInclude(dc => dc.Course)
                .Include(d => d.Courses)
                    .ThenInclude(dc => dc.Section)
                        .ThenInclude(sec => sec!.ScheduleSlots)
                .FirstOrDefaultAsync(d => d.StudentId == student.Id && d.SemesterId == dto.semester_id);

            if (draft == null)
            {
                draft = new RegistrationDraft { StudentId = student.Id, SemesterId = dto.semester_id };
                _context.RegistrationDrafts.Add(draft);
                await _context.SaveChangesAsync();
            }

            if (draft.Courses.Any(c => c.CourseId == dto.course_id))
            {
                return Conflict(new { error = "conflict", message = _localizer.GetMessage("COURSE_ALREADY_IN_DRAFT") });
            }

            var course = await _context.Courses.FindAsync(dto.course_id);
            var section = await _context.Sections.Include(s => s.ScheduleSlots).FirstOrDefaultAsync(s => s.Id == dto.section_id);

            if (course == null || section == null) return UnprocessableEntity(new { error = "validation_error", message = _localizer.GetMessage("INVALID_COURSE_OR_SECTION") });

            draft.Courses.Add(new RegistrationDraftCourse
            {
                DraftId = draft.Id,
                CourseId = dto.course_id,
                SectionId = dto.section_id
            });
            await _context.SaveChangesAsync();

            var conflicts = CalculateConflicts(draft.Courses.ToList());

            return Ok(new
            {
                draft_id = draft.Id,
                semester_id = draft.SemesterId,
                total_credit_hours = draft.Courses.Sum(c => c.Course?.CreditHours ?? 0),
                courses = draft.Courses.Select(dc => new
                {
                    course_id = dc.CourseId,
                    course_code = dc.Course?.CourseCode,
                    course_name = dc.Course?.Title,
                    section_id = dc.SectionId,
                    section_label = dc.Section?.Name,
                    schedule = dc.Section?.ScheduleSlots.Select(ss => $"{ss.Day} {ss.StartTime}-{ss.EndTime}").FirstOrDefault() ?? "TBD",
                    credit_hours = dc.Course?.CreditHours
                }).ToList(),
                conflicts = conflicts
            });
        }

        [HttpDelete("draft/courses/{course_id}")]
        public async Task<IActionResult> RemoveCourseFromDraft(string course_id)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var draft = await _context.RegistrationDrafts
                .Include(d => d.Courses)
                    .ThenInclude(dc => dc.Course)
                .Include(d => d.Courses)
                    .ThenInclude(dc => dc.Section)
                .FirstOrDefaultAsync(d => d.StudentId == student.Id);

            if (draft == null) return NotFound(new { error = "not_found", message = _localizer.GetMessage("DRAFT_NOT_FOUND") });

            var courseToRemove = draft.Courses.FirstOrDefault(c => c.CourseId == course_id);
            if (courseToRemove == null) return NotFound(new { error = "not_found", message = _localizer.GetMessage("COURSE_NOT_IN_DRAFT") });

            _context.RegistrationDraftCourses.Remove(courseToRemove);
            await _context.SaveChangesAsync();

            var remainingCourses = draft.Courses.Where(c => c.CourseId != course_id).ToList();
            var conflicts = CalculateConflicts(remainingCourses);

            return Ok(new
            {
                removed_course_id = course_id,
                draft_id = draft.Id,
                total_credit_hours = remainingCourses.Sum(c => c.Course?.CreditHours ?? 0),
                remaining_courses = remainingCourses.Select(dc => new
                {
                    course_id = dc.CourseId,
                    course_code = dc.Course?.CourseCode,
                    course_name = dc.Course?.Title,
                    section_id = dc.SectionId,
                    section_label = dc.Section?.Name,
                    credit_hours = dc.Course?.CreditHours
                }).ToList(),
                conflicts = conflicts
            });
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetRegistrationStatus([FromQuery] string? semester_id = null)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var activeSemester = semester_id != null 
                ? await _context.Semesters.FindAsync(semester_id)
                : await _context.Semesters.Where(s => s.StartDate > DateTimeOffset.UtcNow).OrderBy(s => s.StartDate).FirstOrDefaultAsync();

            if (activeSemester == null) return NotFound(new { message = _localizer.GetMessage("SEMESTER_NOT_FOUND") });

            var request = await _context.RegistrationRequests
                .Include(r => r.Courses)
                    .ThenInclude(rc => rc.Course)
                .Include(r => r.Courses)
                    .ThenInclude(rc => rc.Section)
                        .ThenInclude(sec => sec!.ScheduleSlots)
                .FirstOrDefaultAsync(r => r.StudentId == student.Id && r.SemesterId == activeSemester.Id);

            if (request != null)
            {
                return Ok(new
                {
                    semester_id = activeSemester.Id,
                    semester_name = activeSemester.Name,
                    status = request.Status,
                    draft_id = (string?)null,
                    ref_code = request.RefCode,
                    submitted_at = request.SubmittedAt,
                    reviewed_at = request.ReviewedAt,
                    reviewer_note = request.ReviewerNote,
                    total_credit_hours = request.Courses.Sum(c => c.Course?.CreditHours ?? 0),
                    courses = request.Courses.Select(rc => new
                    {
                        course_id = rc.CourseId,
                        course_code = rc.Course?.CourseCode,
                        course_name = rc.Course?.Title,
                        section_id = rc.SectionId,
                        section_label = rc.Section?.Name,
                        schedule = rc.Section?.ScheduleSlots.Select(ss => $"{ss.Day} {ss.StartTime}-{ss.EndTime}").FirstOrDefault() ?? "TBD",
                        credit_hours = rc.Course?.CreditHours,
                        approval_status = rc.ApprovalStatus
                    }).ToList(),
                    conflicts = new List<object>()
                });
            }
            else
            {
                var draft = await _context.RegistrationDrafts
                    .Include(d => d.Courses)
                        .ThenInclude(dc => dc.Course)
                    .Include(d => d.Courses)
                        .ThenInclude(dc => dc.Section)
                            .ThenInclude(sec => sec!.ScheduleSlots)
                    .FirstOrDefaultAsync(d => d.StudentId == student.Id && d.SemesterId == activeSemester.Id);

                if (draft == null) return Ok(new { status = "none", semester_id = activeSemester.Id, conflicts = new List<object>() });

                return Ok(new
                {
                    semester_id = activeSemester.Id,
                    semester_name = activeSemester.Name,
                    status = "draft",
                    draft_id = draft.Id,
                    total_credit_hours = draft.Courses.Sum(c => c.Course?.CreditHours ?? 0),
                    courses = draft.Courses.Select(dc => new
                    {
                        course_id = dc.CourseId,
                        course_code = dc.Course?.CourseCode,
                        course_name = dc.Course?.Title,
                        section_id = dc.SectionId,
                        section_label = dc.Section?.Name,
                        schedule = dc.Section?.ScheduleSlots.Select(ss => $"{ss.Day} {ss.StartTime}-{ss.EndTime}").FirstOrDefault() ?? "TBD",
                        credit_hours = dc.Course?.CreditHours,
                        approval_status = (string?)null
                    }).ToList(),
                    conflicts = CalculateConflicts(draft.Courses.ToList())
                });
            }
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

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateRegistration([FromBody] SubmitRegistrationDto dto)
        {
            var courseIds = dto.courses.Select(c => c.course_id).ToList();
            var sectionIds = dto.courses.Select(c => c.section_id).ToList();

            var courses = await _context.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync();
            var sections = await _context.Sections.Include(s => s.ScheduleSlots).Where(s => sectionIds.Contains(s.Id)).ToListAsync();

            var fakeDraftCourses = dto.courses.Select(c => new RegistrationDraftCourse
            {
                CourseId = c.course_id,
                Course = courses.FirstOrDefault(crs => crs.Id == c.course_id),
                SectionId = c.section_id,
                Section = sections.FirstOrDefault(sec => sec.Id == c.section_id)
            }).ToList();

            var conflicts = CalculateConflicts(fakeDraftCourses);

            return Ok(new
            {
                valid = !conflicts.Any(),
                total_credit_hours = courses.Sum(c => c.CreditHours),
                within_credit_limit = courses.Sum(c => c.CreditHours) <= 18,
                errors = conflicts
            });
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitRegistration([FromBody] SubmitRegistrationDto dto)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            // Perform final validation before submission
            var courseIds = dto.courses.Select(c => c.course_id).ToList();
            var sectionIds = dto.courses.Select(c => c.section_id).ToList();
            var courses = await _context.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync();
            var sections = await _context.Sections.Include(s => s.ScheduleSlots).Where(s => sectionIds.Contains(s.Id)).ToListAsync();
            
            var fakeDraftCourses = dto.courses.Select(c => new RegistrationDraftCourse
            {
                CourseId = c.course_id,
                Course = courses.FirstOrDefault(crs => crs.Id == c.course_id),
                SectionId = c.section_id,
                Section = sections.FirstOrDefault(sec => sec.Id == c.section_id)
            }).ToList();

            if (CalculateConflicts(fakeDraftCourses).Any())
            {
                return BadRequest(new { error = "schedule_conflict", message = _localizer.GetMessage("SCHEDULE_CONFLICT") });
            }

            var refCode = $"REG-{DateTime.UtcNow.Year % 100}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";

            var req = new RegistrationRequest
            {
                StudentId = student.Id,
                SemesterId = dto.semester_id,
                RefCode = refCode,
                Status = "pending",
                SubmittedAt = DateTimeOffset.UtcNow
            };

            var totalCredits = 0;
            foreach (var c in dto.courses)
            {
                var crs = courses.FirstOrDefault(x => x.Id == c.course_id);
                if (crs != null) totalCredits += crs.CreditHours;

                req.Courses.Add(new RegistrationRequestCourse
                {
                    RequestId = req.Id,
                    CourseId = c.course_id,
                    SectionId = c.section_id
                });
            }

            _context.RegistrationRequests.Add(req);

            var draft = await _context.RegistrationDrafts.FirstOrDefaultAsync(d => d.StudentId == student.Id && d.SemesterId == dto.semester_id);
            if (draft != null) _context.RegistrationDrafts.Remove(draft);

            await _context.SaveChangesAsync();

            return Created("", new
            {
                registration_id = req.Id,
                ref_code = req.RefCode,
                status = "pending",
                total_credit_hours = totalCredits,
                submitted_at = req.SubmittedAt,
                message = _localizer.GetMessage("REGISTRATION_SUBMITTED")
            });
        }

        private List<object> CalculateConflicts(List<RegistrationDraftCourse> courses)
        {
            var conflicts = new List<object>();
            var courseList = courses.Where(c => c.Section != null).ToList();

            for (int i = 0; i < courseList.Count; i++)
            {
                for (int j = i + 1; j < courseList.Count; j++)
                {
                    var sec1 = courseList[i].Section!;
                    var sec2 = courseList[j].Section!;

                    foreach (var slot1 in sec1.ScheduleSlots)
                    {
                        foreach (var slot2 in sec2.ScheduleSlots)
                        {
                            if (slot1.Day == slot2.Day)
                            {
                                // Overlap check: [s1, e1] and [s2, e2] overlap if s1 < e2 and s2 < e1
                                if (string.Compare(slot1.StartTime, slot2.EndTime) < 0 && string.Compare(slot2.StartTime, slot1.EndTime) < 0)
                                {
                                    conflicts.Add(new
                                    {
                                        type = "schedule_conflict",
                                        message = $"{courseList[i].Course?.CourseCode} {sec1.Name} ({slot1.StartTime}-{slot1.EndTime}) " + 
                                                 _localizer.GetMessage("CONFLICTS_WITH") + 
                                                 $" {courseList[j].Course?.CourseCode} {sec2.Name} ({slot2.StartTime}-{slot2.EndTime})",
                                        affected_courses = new[] { courseList[i].CourseId, courseList[j].CourseId },
                                        affected_sections = new[] { courseList[i].SectionId, courseList[j].SectionId }
                                    });
                                }
                            }
                        }
                    }
                }
            }
            return conflicts;
        }
    }
}
