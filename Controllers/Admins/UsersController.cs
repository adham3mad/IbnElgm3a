using IbnElgm3a.DTOs.Users;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("admin/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILocalizationService _localizer;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

        public UsersController(AppDbContext context, IConfiguration config, ILocalizationService localizer, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _config = config;
            _localizer = localizer;
            _scopeFactory = scopeFactory;
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
        [RequirePermission(PermissionEnum.Dashboard_Users_Read)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] UserRole role,
            [FromQuery] UserStatus? status = null,
            [FromQuery] string? q = null,
            [FromQuery] string? faculty_id = null,
            [FromQuery] string? dept_id = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string sort_by = "name",
            [FromQuery] string sort_dir = "asc")
        {
            var query = _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.Department)
                .Include(u => u.Student)
                .Where(u => u.Role != null && u.Role.Name.ToLower() == role.ToString().ToLower());

            if (status.HasValue) query = query.Where(u => u.Status == status.Value);
            if (!string.IsNullOrEmpty(faculty_id)) query = query.Where(u => u.FacultyId == faculty_id);
            if (!string.IsNullOrEmpty(dept_id)) query = query.Where(u => u.DepartmentId == dept_id);
            if (!string.IsNullOrEmpty(q))
            {
                var qLower = q.ToLower();
                query = query.Where(u => u.Name.ToLower().Contains(qLower) || u.Email.ToLower().Contains(qLower));
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Name) 
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(u => new UserListResponseDto
                {
                    Id = u.Id,
                    FullName = u.Name,
                    Email = u.Email,
                    StudentId = u.Student != null ? u.Student.AcademicNumber : null,
                    Faculty = u.Faculty != null ? new IdNameDto { Id = u.Faculty.Id, Name = u.Faculty.NameAr ?? u.Faculty.Name } : null,
                    Department = u.Department != null ? new IdNameDto { Id = u.Department.Id, Name = u.Department.NameAr ?? u.Department.Name } : null,
                    Year = u.Student != null ? u.Student.Level : null,
                    Gpa = u.Student != null ? u.Student.GPA : null,
                    Status = u.Status,
                    EnrolledAt = u.Student != null ? u.Student.EnrollmentDate : null
                }).ToListAsync();

            var pagination = new ApiPagination { Page = page, Limit = limit, Total = total, HasMore = (page * limit) < total };
            return Ok(ApiResponse<List<UserListResponseDto>>.CreateSuccess(users, pagination: pagination));
        }

        [HttpGet("{user_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Users_Read)]
        public async Task<IActionResult> GetUserById(string user_id)
        {
            var user = await _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.Department)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == user_id);

            if (user == null) return NotFound(ApiResponse<object>.CreateError("USER_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            var dto = new UserListResponseDto
            {
                Id = user.Id,
                FullName = user.Name,
                Email = user.Email,
                Faculty = user.Faculty != null ? new IdNameDto { Id = user.Faculty.Id, Name = user.Faculty.NameAr ?? user.Faculty.Name } : null,
                Department = user.Department != null ? new IdNameDto { Id = user.Department.Id, Name = user.Department.NameAr ?? user.Department.Name } : null,
                Status = user.Status
            };

            return Ok(ApiResponse<UserListResponseDto>.CreateSuccess(dto));
        }

        [HttpPatch("{user_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Users_Update)]
        public async Task<IActionResult> UpdateUser(string user_id, [FromBody] UpdateUserRequestDto request)
        {
            var user = await _context.Users.FindAsync(user_id);
            if (user == null) return NotFound(ApiResponse<object>.CreateError("USER_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            if (!string.IsNullOrEmpty(request.FullName)) user.Name = request.FullName;
            if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
            if (!string.IsNullOrEmpty(request.NationalId)) user.NationalId = request.NationalId;
            if (!string.IsNullOrEmpty(request.Phone)) user.Phone = request.Phone;
            if (!string.IsNullOrEmpty(request.FacultyId)) user.FacultyId = request.FacultyId;
            if (!string.IsNullOrEmpty(request.DepartmentId)) user.DepartmentId = request.DepartmentId;
            if (request.Status.HasValue) user.Status = request.Status.Value;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("USER_UPDATED") }));
        }

        [HttpPatch("{user_id}/status")]
        [RequirePermission(PermissionEnum.Dashboard_Users_UpdateStatus)]
        public async Task<IActionResult> UpdateUserStatus(string user_id, [FromBody] UpdateUserStatusRequestDto request)
        {
            var user = await _context.Users.FindAsync(user_id);
            if (user == null) return NotFound(ApiResponse<object>.CreateError("USER_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            if (request.Status.HasValue) user.Status = request.Status.Value;
            user.InactiveReason = request.Reason ?? user.InactiveReason;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("USER_UPDATED") }));
        }

        [HttpDelete("{user_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Users_Delete)]
        public async Task<IActionResult> DeleteUser(string user_id)
        {
            var user = await _context.Users.FindAsync(user_id);
            if (user == null) return NotFound(ApiResponse<object>.CreateError("USER_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }

        [HttpPost("import")]
        [RequirePermission(PermissionEnum.Dashboard_Users_Import)]
        public async Task<IActionResult> BulkImport([FromBody] ImportUsersRequestDto request)
        {
            var job = new BulkImportJob 
            { 
                Id = "job_" + Guid.NewGuid().ToString("N").Substring(0, 10), 
                Type = request.Type,
                FileUrl = request.FileUrl,
                Status = "pending" 
            };
            
            _context.BulkImportJobs.Add(job);
            await _context.SaveChangesAsync();

            // Fire and forget background task
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var currentJob = await db.BulkImportJobs.FindAsync(job.Id);
                if (currentJob == null) return;

                currentJob.Status = "processing";
                await db.SaveChangesAsync();

                var failedRows = new List<object>();
                int importedCount = 0;
                int totalRowsCount = 0;

                try 
                {
                    // 1. Fetch CSV data
                    string csvData;
                    if (currentJob.FileUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                        currentJob.FileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        using var client = new System.Net.Http.HttpClient();
                        csvData = await client.GetStringAsync(currentJob.FileUrl);
                    }
                    else
                    {
                        // Fallback to local file path
                        var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), currentJob.FileUrl);
                        if (!System.IO.File.Exists(path))
                        {
                            throw new System.IO.FileNotFoundException($"Local CSV file not found at {path}");
                        }
                        csvData = await System.IO.File.ReadAllTextAsync(path);
                    }

                    // 2. Parse lines
                    var lines = csvData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length <= 1)
                    {
                        throw new InvalidOperationException("CSV file is empty or missing data rows");
                    }

                    // 3. Extract headers
                    var headerLine = lines[0];
                    var headers = SplitCsvLine(headerLine)
                        .Select(h => h.ToLowerInvariant().Replace("_", "").Replace(" ", ""))
                        .ToArray();

                    int nationalIdIdx = Array.IndexOf(headers, "nationalid");
                    int emailIdx = Array.IndexOf(headers, "email");
                    int nameIdx = Array.IndexOf(headers, "name");
                    int facultyCodeIdx = Array.IndexOf(headers, "facultycode");
                    int deptCodeIdx = Array.IndexOf(headers, "deptcode");
                    int academicNumberIdx = Array.IndexOf(headers, "academicnumber");
                    int phoneIdx = Array.IndexOf(headers, "phone");

                    // Validate critical headers
                    if (nationalIdIdx < 0 || emailIdx < 0 || nameIdx < 0 || facultyCodeIdx < 0 || deptCodeIdx < 0)
                    {
                        throw new InvalidOperationException("CSV is missing one or more required headers: national_id, email, name, faculty_code, dept_code");
                    }

                    bool isStudent = "students".Equals(currentJob.Type, StringComparison.OrdinalIgnoreCase);
                    if (isStudent && academicNumberIdx < 0)
                    {
                        throw new InvalidOperationException("CSV is missing the 'academic_number' header required for student import");
                    }

                    // 4. Preload Lookups
                    var studentRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
                    var instructorRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Instructor");
                    if (studentRole == null || instructorRole == null)
                    {
                        throw new InvalidOperationException("Default Student or Instructor role not found in database");
                    }

                    var faculties = await db.Faculties.ToListAsync();
                    var departments = await db.Departments.ToListAsync();
                    
                    var existingEmails = (await db.Users.Select(u => u.Email.ToLower()).ToListAsync()).ToHashSet();
                    var existingNationalIds = (await db.Users.Select(u => u.NationalId).ToListAsync()).ToHashSet();
                    var existingAcademicNumbers = (await db.Students.Select(s => s.AcademicNumber).ToListAsync()).ToHashSet();

                    var processedEmails = new HashSet<string>(existingEmails);
                    var processedNationalIds = new HashSet<string>(existingNationalIds);
                    var processedAcademicNumbers = new HashSet<string>(existingAcademicNumbers);

                    var pepper = config["PASSWORD_PEPPER"] ?? "";
                    var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("123456" + pepper);

                    totalRowsCount = lines.Length - 1;

                    // 5. Process each row
                    for (int i = 1; i < lines.Length; i++)
                    {
                        int rowNum = i + 1;
                        var line = lines[i];
                        var rowData = SplitCsvLine(line);

                        if (rowData.Length < headers.Length)
                        {
                            failedRows.Add(new { row = rowNum, error = "Column count mismatch (fewer columns than header)" });
                            continue;
                        }

                        var email = emailIdx >= 0 && emailIdx < rowData.Length ? rowData[emailIdx].Trim().ToLower() : "";
                        var nationalId = nationalIdIdx >= 0 && nationalIdIdx < rowData.Length ? rowData[nationalIdIdx].Trim() : "";
                        var name = nameIdx >= 0 && nameIdx < rowData.Length ? rowData[nameIdx].Trim() : "";
                        var phone = phoneIdx >= 0 && phoneIdx < rowData.Length ? rowData[phoneIdx].Trim() : "";
                        var facultyCode = facultyCodeIdx >= 0 && facultyCodeIdx < rowData.Length ? rowData[facultyCodeIdx].Trim() : "";
                        var deptCode = deptCodeIdx >= 0 && deptCodeIdx < rowData.Length ? rowData[deptCodeIdx].Trim() : "";
                        var academicNumber = academicNumberIdx >= 0 && academicNumberIdx < rowData.Length ? rowData[academicNumberIdx].Trim() : "";

                        // Validation
                        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(nationalId) || string.IsNullOrEmpty(name))
                        {
                            failedRows.Add(new { row = rowNum, error = "Missing required fields (email, national_id, or name)" });
                            continue;
                        }

                        if (processedEmails.Contains(email))
                        {
                            failedRows.Add(new { row = rowNum, error = $"Duplicate email: '{email}'" });
                            continue;
                        }

                        if (processedNationalIds.Contains(nationalId))
                        {
                            failedRows.Add(new { row = rowNum, error = $"Duplicate National ID: '{nationalId}'" });
                            continue;
                        }

                        if (isStudent)
                        {
                            if (string.IsNullOrEmpty(academicNumber))
                            {
                                failedRows.Add(new { row = rowNum, error = "Missing academic number for student" });
                                continue;
                            }

                            if (processedAcademicNumbers.Contains(academicNumber))
                            {
                                failedRows.Add(new { row = rowNum, error = $"Duplicate Academic Number: '{academicNumber}'" });
                                continue;
                            }
                        }

                        var faculty = faculties.FirstOrDefault(f => f.Code.Equals(facultyCode, StringComparison.OrdinalIgnoreCase) || f.Name.Equals(facultyCode, StringComparison.OrdinalIgnoreCase));
                        if (faculty == null)
                        {
                            failedRows.Add(new { row = rowNum, error = $"Faculty '{facultyCode}' not found" });
                            continue;
                        }

                        var department = departments.FirstOrDefault(d => d.Code.Equals(deptCode, StringComparison.OrdinalIgnoreCase) || d.Name.Equals(deptCode, StringComparison.OrdinalIgnoreCase));
                        if (department == null)
                        {
                            failedRows.Add(new { row = rowNum, error = $"Department '{deptCode}' not found" });
                            continue;
                        }

                        // Create Database records
                        try
                        {
                            var user = new User
                            {
                                Name = name,
                                Email = email,
                                Phone = phone,
                                NationalId = nationalId,
                                PasswordHash = defaultPasswordHash,
                                RoleId = isStudent ? studentRole.Id : instructorRole.Id,
                                Status = UserStatus.Active,
                                FacultyId = faculty.Id,
                                DepartmentId = department.Id
                            };
                            db.Users.Add(user);

                            if (isStudent)
                            {
                                var student = new Student
                                {
                                    User = user,
                                    AcademicNumber = academicNumber,
                                    GPA = 0.00m,
                                    Level = 1,
                                    EnrollmentDate = DateTimeOffset.UtcNow,
                                    IsActive = true,
                                    BirthDate = DateTimeOffset.UtcNow.AddYears(-20),
                                    Gender = Gender.Male,
                                    Nationality = "Egyptian",
                                    DepartmentId = department.Id
                                };
                                db.Students.Add(student);
                                processedAcademicNumbers.Add(academicNumber);
                            }
                            else
                            {
                                var instructor = new Instructor
                                {
                                    User = user,
                                    Rank = "Professor",
                                    DepartmentId = department.Id
                                };
                                db.Instructors.Add(instructor);
                            }

                            await db.SaveChangesAsync();

                            processedEmails.Add(email);
                            processedNationalIds.Add(nationalId);
                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedRows.Add(new { row = rowNum, error = $"Database save failed: {ex.InnerException?.Message ?? ex.Message}" });
                        }
                    }

                    currentJob.Status = "done";
                }
                catch (Exception ex)
                {
                    currentJob.Status = "failed";
                    failedRows.Add(new { row = 0, error = $"Import job error: {ex.Message}" });
                }

                currentJob.Imported = importedCount;
                currentJob.Total = totalRowsCount;
                currentJob.FailedRowsJson = System.Text.Json.JsonSerializer.Serialize(failedRows);
                await db.SaveChangesAsync();
            });

            return Accepted(new { job_id = job.Id, status = job.Status });
        }

        private static string[] SplitCsvLine(string line)
        {
            var list = new List<string>();
            var inQuotes = false;
            var currentToken = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    list.Add(currentToken.ToString().Trim('\"', ' '));
                    currentToken.Clear();
                }
                else
                {
                    currentToken.Append(c);
                }
            }
            list.Add(currentToken.ToString().Trim('\"', ' '));
            return list.ToArray();
        }

        [HttpGet("import/status/{job_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Users_Import)]
        public async Task<IActionResult> GetBulkImportStatus(string job_id)
        {
            var job = await _context.BulkImportJobs.FindAsync(job_id);
            if (job == null) return NotFound(ApiResponse<object>.CreateError("JOB_NOT_FOUND", _localizer.GetMessage("JOB_NOT_FOUND")));

            return Ok(new 
            { 
                job_id = job.Id,
                status = job.Status, 
                total = job.Total, 
                imported = job.Imported,
                failed_rows = System.Text.Json.JsonSerializer.Deserialize<List<object>>(job.FailedRowsJson)
            });
        }
    }
}
