using IbnElgm3a.DTOs.Users;
using IbnElgm3a.DTOs.Students;
using IbnElgm3a.DTOs.Guardians;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.Enums;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IbnElgm3a.Services;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/students")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILocalizationService _localizer;
        private readonly IEmailService _emailService;

        public StudentsController(AppDbContext context, IConfiguration config, ILocalizationService localizer, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _localizer = localizer;
            _emailService = emailService;
        }

        private async Task<string> GenerateAcademicNumberAsync(string facultyId, int year)
        {
            var faculty = await _context.Faculties.FindAsync(facultyId);
            if (faculty == null) throw new Exception(_localizer.GetMessage("FACULTY_NOT_FOUND"));
            if (string.IsNullOrEmpty(faculty.Code)) throw new Exception(_localizer.GetMessage("INTERNAL_FACULTY_CODE_MISSING"));

            var yearPart = year.ToString().Substring(year.ToString().Length - 2);
            var facCode = faculty.Code.PadRight(4, '0').Substring(0, 4);
            var prefix = yearPart + facCode;
            
            var lastNumber = await _context.Students
                .Where(s => s.AcademicNumber.StartsWith(prefix))
                .OrderByDescending(s => s.AcademicNumber)
                .Select(s => s.AcademicNumber)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (lastNumber != null && lastNumber.Length >= 12)
            {
                if (int.TryParse(lastNumber.Substring(6), out var currentSeq))
                {
                    nextSeq = currentSeq + 1;
                }
            }

            return prefix + nextSeq.ToString("D6");
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_UsersRead)]
        public async Task<IActionResult> GetStudents(
            [FromQuery] string? q = null,
            [FromQuery] string? faculty_id = null,
            [FromQuery] string? dept_id = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.User!.Faculty)
                .Include(s => s.User!.Department)
                .AsQueryable();

            if (!string.IsNullOrEmpty(faculty_id)) query = query.Where(s => s.User!.FacultyId == faculty_id);
            if (!string.IsNullOrEmpty(dept_id)) query = query.Where(s => s.DepartmentId == dept_id);
            if (!string.IsNullOrEmpty(q))
            {
                var qLower = q.ToLower();
                query = query.Where(s => s.User!.Name.ToLower().Contains(qLower) || s.User!.Email.ToLower().Contains(qLower) || s.AcademicNumber.Contains(q));
            }

            var total = await query.CountAsync();
            var students = await query
                .OrderBy(s => s.User!.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(s => new UserListResponseDto
                {
                    Id = s.UserId,
                    FullName = s.User!.Name,
                    Email = s.User!.Email,
                    StudentId = s.AcademicNumber,
                    Faculty = s.User!.Faculty != null ? new IdNameDto { Id = s.User!.Faculty.Id, Name = s.User!.Faculty.NameAr ?? s.User!.Faculty.Name } : null,
                    Department = s.User!.Department != null ? new IdNameDto { Id = s.User!.Department.Id, Name = s.User!.Department.NameAr ?? s.User!.Department.Name } : null,
                    Year = s.Level,
                    Gpa = s.GPA,
                    Status = s.User!.Status,
                    EnrolledAt = s.EnrollmentDate
                }).ToListAsync();

            var pagination = new ApiPagination { Page = page, Limit = limit, Total = total, HasMore = (page * limit) < total };
            return Ok(ApiResponse<List<UserListResponseDto>>.CreateSuccess(students, pagination: pagination));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_UsersRead)]
        public async Task<IActionResult> GetStudentById(string id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                    .ThenInclude(u => u!.Faculty)
                .Include(s => s.User)
                    .ThenInclude(u => u!.Department)
                .Include(s => s.Guardians)
                    .ThenInclude(sg => sg.Guardian)
                .FirstOrDefaultAsync(s => s.Id == id || s.UserId == id);

            if (student == null) return NotFound(ApiResponse<object>.CreateError("STUDENT_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            var user = student.User!;
            var dto = new UserListResponseDto
            {
                Id = user.Id,
                FullName = user.Name,
                Email = user.Email,
                StudentId = student.AcademicNumber,
                Faculty = user.Faculty != null ? new IdNameDto { Id = user.Faculty.Id, Name = user.Faculty.NameAr ?? user.Faculty.Name } : null,
                Department = user.Department != null ? new IdNameDto { Id = user.Department.Id, Name = user.Department.NameAr ?? user.Department.Name } : null,
                Year = student.Level,
                Gpa = student.GPA,
                Status = user.Status,
                EnrolledAt = student.EnrollmentDate,
                StudentData = new StudentDetailsDto
                {
                    BirthDate = student.BirthDate,
                    Gender = student.Gender,
                    Nationality = student.Nationality,
                    Guardians = student.Guardians.Select(sg => new CreateGuardianRequestDto
                    {
                        FullName = sg.Guardian?.FullName ?? "",
                        NationalId = sg.Guardian?.NationalId ?? "",
                        Phone = sg.Guardian?.Phone ?? "",
                        Email = sg.Guardian?.Email ?? "",
                        Address = sg.Guardian?.Address ?? "",
                        Job = sg.Guardian?.Job ?? ""
                    }).ToList()
                }
            };

            return Ok(ApiResponse<UserListResponseDto>.CreateSuccess(dto));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_StudentsCreate)]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequestDto request)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (exists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_EMAIL", _localizer.GetMessage("DUPLICATE_EMAIL")));

            var nidExists = await _context.Users.AnyAsync(u => u.NationalId == request.NationalId);
            if (nidExists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NATIONAL_ID", _localizer.GetMessage("DUPLICATE_NATIONAL_ID")));

            var pepper = _config["PASSWORD_PEPPER"] ?? "";
            var hash = BCrypt.Net.BCrypt.HashPassword(request.NationalId + pepper);
            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "student");

            var user = new User
            {
                Id = "usr_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                Name = request.FullName,
                FullNameAr = request.FullNameAr,
                Email = request.Email,
                NationalId = request.NationalId,
                Phone = request.Phone,
                RoleId = studentRole?.Id ?? "",
                FacultyId = request.FacultyId,
                DepartmentId = request.DepartmentId,
                PasswordHash = hash,
                MustChangePw = true,
                Status = UserStatus.Active
            };

            var student = new Student
            {
                Id = "stu_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                UserId = user.Id,
                AcademicNumber = await GenerateAcademicNumberAsync(request.FacultyId, DateTime.Now.Year),
                Level = request.Year ?? 1,
                EnrollmentDate = DateTimeOffset.UtcNow,
                DepartmentId = request.DepartmentId,
                BirthDate = request.BirthDate,
                Gender = request.Gender,
                Nationality = request.Nationality
            };

            foreach (var gDto in request.Guardians)
            {
                var guardian = new Guardian
                {
                    Id = "gdn_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                    FullName = gDto.FullName,
                    NationalId = gDto.NationalId,
                    Phone = gDto.Phone,
                    Email = gDto.Email,
                    Address = gDto.Address,
                    Job = gDto.Job
                };
                student.Guardians.Add(new StudentGuardian { Guardian = guardian, RelationType = RelationType.Guardian });
            }

            user.Student = student;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send Welcome Email (Password is NationalId by default)
            await _emailService.SendWelcomeEmailAsync(user.Email, user.Name, user.NationalId);

            return Created("", ApiResponse<object>.CreateSuccess(new { id = user.Id }));
        }

        [HttpPatch("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_StudentsUpdate)]
        public async Task<IActionResult> UpdateStudent(string id, [FromBody] UpdateStudentRequestDto request)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Guardians)
                    .ThenInclude(sg => sg.Guardian)
                .FirstOrDefaultAsync(s => s.Id == id || s.UserId == id);

            if (student == null) return NotFound(ApiResponse<object>.CreateError("STUDENT_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            var user = student.User!;
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_EMAIL", _localizer.GetMessage("DUPLICATE_EMAIL")));
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.FullName)) user.Name = request.FullName;
            if (request.FullNameAr != null) user.FullNameAr = request.FullNameAr;
            if (!string.IsNullOrEmpty(request.NationalId)) user.NationalId = request.NationalId;
            if (request.Phone != null) user.Phone = request.Phone;
            
            if (request.BirthDate.HasValue) student.BirthDate = request.BirthDate.Value;
            if (request.Gender.HasValue) student.Gender = request.Gender.Value;
            if (request.Nationality != null) student.Nationality = request.Nationality;
            if (request.Year.HasValue) student.Level = request.Year.Value;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("USER_UPDATED") }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_UsersDelete)]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id || s.UserId == id);
            if (student == null) return NotFound(ApiResponse<object>.CreateError("STUDENT_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            _context.Users.Remove(student.User!);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
