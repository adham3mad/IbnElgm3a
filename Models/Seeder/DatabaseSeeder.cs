using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Models.Seeder
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAllAsync(AppDbContext context, IConfiguration config)
        {
            var pepper = config["PASSWORD_PEPPER"] ?? "";

            // 1. Seed Permissions & Features
            await PermissionSeeder.SeedAsync(context);

            // 2. Seed Roles
            var roles = await SeedRolesAsync(context);
            var adminRole = roles.First(r => r.Name == "Admin");
            var studentRole = roles.First(r => r.Name == "Student");
            var instructorRole = roles.First(r => r.Name == "Instructor");
            var guardianRole = roles.First(r => r.Name == "Guardian");

            // 3. Seed Faculties & Departments
            var faculties = await SeedFacultiesAsync(context);
            var departments = await SeedDepartmentsAsync(context, faculties);

            // 4. Seed Rooms
            var rooms = await SeedRoomsAsync(context);

            // 5. Seed Users
            var adminUser = await SeedUserAsync(context, adminRole, "Admin User", "admin@ibnelgm3a.com", "1234567890", "1234567890", pepper);
            var instructors = await SeedInstructorsAsync(context, instructorRole, departments, pepper);
            var students = await SeedStudentsAsync(context, studentRole, faculties, departments, pepper);
            var guardians = await SeedGuardiansAsync(context, students);

            // 6. Seed Semesters & Courses
            var currentSemester = await SeedSemestersAsync(context);
            var courses = await SeedCoursesAsync(context, departments, instructors, currentSemester);

            // 7. Seed Sections & Schedule
            var sections = await SeedSectionsAsync(context, courses, instructors, currentSemester, rooms);

            // 8. Seed Enrollments & Grades
            await SeedEnrollmentsAndGradesAsync(context, students, sections);

            // 9. Seed Exams
            await SeedExamsAsync(context, courses, currentSemester, rooms, instructors);

            // 10. Seed Features Data
            await SeedComplaintsAsync(context, students, adminUser);
            await SeedAnnouncementsAsync(context, adminUser);
            await SeedCalendarEventsAsync(context, currentSemester);
            await SeedSystemSettingsAsync(context);

            await context.SaveChangesAsync();
        }

        private static async Task<List<Role>> SeedRolesAsync(AppDbContext context)
        {
            var roleNames = new[] { "Admin", "Student", "Instructor", "Guardian" };
            var roles = new List<Role>();

            foreach (var name in roleNames)
            {
                var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == name);
                if (role == null)
                {
                    role = new Role
                    {
                        Name = name,
                        NameAr = name switch {
                            "Admin" => "مدير النظام",
                            "Student" => "طالب",
                            "Instructor" => "محاضر",
                            "Guardian" => "ولي أمر",
                            _ => name
                        },
                        Type = AppType.Dashboard,
                        IsActive = true
                    };
                    context.Roles.Add(role);
                }
                roles.Add(role);
            }
            await context.SaveChangesAsync();
            return roles;
        }

        private static async Task<List<Faculty>> SeedFacultiesAsync(AppDbContext context)
        {
            if (await context.Faculties.AnyAsync()) return await context.Faculties.ToListAsync();

            var faculties = new List<Faculty>
            {
                new Faculty { Name = "Faculty of Engineering", NameAr = "كلية الهندسة", Code = "ENG", CreatedAt = DateTimeOffset.UtcNow },
                new Faculty { Name = "Faculty of Commerce", NameAr = "كلية التجارة", Code = "COM", CreatedAt = DateTimeOffset.UtcNow },
                new Faculty { Name = "Faculty of Science", NameAr = "كلية العلوم", Code = "SCI", CreatedAt = DateTimeOffset.UtcNow }
            };

            context.Faculties.AddRange(faculties);
            await context.SaveChangesAsync();
            return faculties;
        }

        private static async Task<List<Department>> SeedDepartmentsAsync(AppDbContext context, List<Faculty> faculties)
        {
            if (await context.Departments.AnyAsync()) return await context.Departments.ToListAsync();

            var depts = new List<Department>();
            foreach (var faculty in faculties)
            {
                depts.Add(new Department { Name = $"{faculty.Code} Dept 1", NameAr = $"قسم 1 - {faculty.NameAr}", Code = $"{faculty.Code}01", FacultyId = faculty.Id });
                depts.Add(new Department { Name = $"{faculty.Code} Dept 2", NameAr = $"قسم 2 - {faculty.NameAr}", Code = $"{faculty.Code}02", FacultyId = faculty.Id });
            }

            context.Departments.AddRange(depts);
            await context.SaveChangesAsync();
            return depts;
        }

        private static async Task<List<Room>> SeedRoomsAsync(AppDbContext context)
        {
            if (await context.Rooms.AnyAsync()) return await context.Rooms.ToListAsync();

            var rooms = new List<Room>
            {
                new Room { Name = "Hall A", Code = "HALA", Capacity = 100, Type = RoomType.LectureHall },
                new Room { Name = "Lab 1", Code = "LAB1", Capacity = 30, Type = RoomType.Lab },
                new Room { Name = "Room 101", Code = "R101", Capacity = 50, Type = RoomType.TutorialRoom }
            };

            context.Rooms.AddRange(rooms);
            await context.SaveChangesAsync();
            return rooms;
        }

        private static async Task<User> SeedUserAsync(AppDbContext context, Role role, string name, string email, string phone, string nid, string pepper)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Name = name,
                    Email = email,
                    Phone = phone,
                    NationalId = nid,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456" + pepper),
                    RoleId = role.Id,
                    Status = UserStatus.Active,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }
            return user;
        }

        private static async Task<List<Instructor>> SeedInstructorsAsync(AppDbContext context, Role role, List<Department> departments, string pepper)
        {
            if (await context.Instructors.AnyAsync()) return await context.Instructors.ToListAsync();

            var instructors = new List<Instructor>();
            for (int i = 1; i <= 5; i++)
            {
                var user = new User
                {
                    Name = $"Instructor {i}",
                    Email = $"instructor{i}@example.com",
                    Phone = $"010000000{i}",
                    NationalId = $"2900000000000{i}",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456" + pepper),
                    RoleId = role.Id,
                    Status = UserStatus.Active,
                    DepartmentId = departments[i % departments.Count].Id,
                    FacultyId = departments[i % departments.Count].FacultyId
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                var instructor = new Instructor { UserId = user.Id, Rank = "Professor", DepartmentId = user.DepartmentId };
                context.Instructors.Add(instructor);
                instructors.Add(instructor);
            }
            await context.SaveChangesAsync();
            return instructors;
        }

        private static async Task<List<Student>> SeedStudentsAsync(AppDbContext context, Role role, List<Faculty> faculties, List<Department> departments, string pepper)
        {
            if (await context.Students.AnyAsync()) return await context.Students.ToListAsync();

            var students = new List<Student>();
            var random = new Random();
            for (int i = 1; i <= 20; i++) // Increased to 20 for better report variety
            {
                var faculty = faculties[i % faculties.Count];
                var deptSet = departments.Where(d => d.FacultyId == faculty.Id).ToList();
                var dept = deptSet[i % deptSet.Count];

                var user = new User
                {
                    Name = $"Student {i}",
                    Email = $"student{i}@example.com",
                    Phone = $"011000000{i:D2}",
                    NationalId = $"3900000000000{i:D2}",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456" + pepper),
                    RoleId = role.Id,
                    Status = UserStatus.Active,
                    FacultyId = faculty.Id,
                    DepartmentId = dept.Id
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Vary GPAs for "At Risk" report
                decimal gpa = (decimal)(random.NextDouble() * (4.0 - 1.5) + 1.5);

                var student = new Student
                {
                    UserId = user.Id,
                    AcademicNumber = $"2024{i:D4}",
                    GPA = Math.Round(gpa, 2),
                    Level = (i % 4) + 1,
                    EnrollmentDate = DateTimeOffset.UtcNow.AddYears(-(i % 4)),
                    IsActive = true,
                    BirthDate = DateTimeOffset.UtcNow.AddYears(-20),
                    Gender = i % 2 == 0 ? Gender.Male : Gender.Female,
                    Nationality = "Egyptian",
                    DepartmentId = dept.Id
                };
                context.Students.Add(student);
                students.Add(student);
            }
            await context.SaveChangesAsync();
            return students;
        }

        private static async Task<List<Guardian>> SeedGuardiansAsync(AppDbContext context, List<Student> students)
        {
            if (await context.Guardians.AnyAsync()) return await context.Guardians.ToListAsync();

            var guardians = new List<Guardian>();
            for (int i = 1; i <= 10; i++)
            {
                var guardian = new Guardian 
                { 
                    FullName = $"Guardian {i}",
                    Email = $"guardian{i}@example.com",
                    Phone = $"012000000{i:D2}",
                    NationalId = $"4900000000000{i:D2}",
                    Address = "Cairo, Egypt",
                    Job = "Engineer"
                };
                context.Guardians.Add(guardian);
                await context.SaveChangesAsync();
                guardians.Add(guardian);

                // Link to two students
                var student1 = students[(i * 2 - 2) % students.Count];
                var student2 = students[(i * 2 - 1) % students.Count];

                context.StudentGuardians.Add(new StudentGuardian { StudentId = student1.UserId, GuardianId = guardian.Id, RelationType = RelationType.Father });
                context.StudentGuardians.Add(new StudentGuardian { StudentId = student2.UserId, GuardianId = guardian.Id, RelationType = RelationType.Mother });
            }
            await context.SaveChangesAsync();
            return guardians;
        }

        private static async Task<Semester> SeedSemestersAsync(AppDbContext context)
        {
            var semester = await context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
            if (semester == null)
            {
                semester = new Semester
                {
                    Name = "Spring 2024",
                    NameAr = "ربيع 2024",
                    StartDate = DateTimeOffset.UtcNow.AddMonths(-2),
                    EndDate = DateTimeOffset.UtcNow.AddMonths(2),
                    IsActive = true
                };
                context.Semesters.Add(semester);
                await context.SaveChangesAsync();
            }
            return semester;
        }

        private static async Task<List<Course>> SeedCoursesAsync(AppDbContext context, List<Department> departments, List<Instructor> instructors, Semester semester)
        {
            if (await context.Courses.AnyAsync()) return await context.Courses.ToListAsync();

            var courses = new List<Course>();
            int idx = 0;
            foreach (var dept in departments)
            {
                courses.Add(new Course { Title = $"{dept.Code} Course 1", TitleAr = $"مادة 1 - {dept.NameAr}", CourseCode = $"{dept.Code}101", CreditHours = 3, DepartmentId = dept.Id, SemesterId = semester.Id, InstructorId = instructors[idx % instructors.Count].UserId });
                courses.Add(new Course { Title = $"{dept.Code} Course 2", TitleAr = $"مادة 2 - {dept.NameAr}", CourseCode = $"{dept.Code}102", CreditHours = 4, DepartmentId = dept.Id, SemesterId = semester.Id, InstructorId = instructors[(idx + 1) % instructors.Count].UserId });
                idx++;
            }

            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();
            return courses;
        }

        private static async Task<List<Section>> SeedSectionsAsync(AppDbContext context, List<Course> courses, List<Instructor> instructors, Semester semester, List<Room> rooms)
        {
            if (await context.Sections.AnyAsync()) return await context.Sections.ToListAsync();

            var sections = new List<Section>();
            for (int i = 0; i < courses.Count; i++)
            {
                var section = new Section
                {
                    CourseId = courses[i].Id,
                    InstructorId = instructors[i % instructors.Count].UserId,
                    SemesterId = semester.Id,
                    Name = "Section 01",
                    Capacity = 40,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0)
                };
                context.Sections.Add(section);
                sections.Add(section);

                context.ScheduleSlots.Add(new ScheduleSlot
                {
                    SectionId = section.Id,
                    RoomId = rooms[i % rooms.Count].Id,
                    Day = (DayOfWeekEnum)(i % 6),
                    StartTime = "10:00",
                    EndTime = "12:00",
                    Type = ClassType.Lecture,
                    SemesterId = semester.Id
                });
            }
            await context.SaveChangesAsync();
            return sections;
        }

        private static async Task SeedEnrollmentsAndGradesAsync(AppDbContext context, List<Student> students, List<Section> sections)
        {
            if (await context.Enrollments.AnyAsync()) return;

            var random = new Random();
            foreach (var student in students)
            {
                // Enroll in 4 random sections
                var enrolledSections = sections.OrderBy(x => Guid.NewGuid()).Take(4).ToList();
                foreach (var section in enrolledSections)
                {
                    var enrollment = new Enrollment
                    {
                        StudentId = student.UserId,
                        SectionId = section.Id,
                        Status = EnrollmentStatus.Enrolled,
                        EnrolledAt = DateTimeOffset.UtcNow
                    };
                    context.Enrollments.Add(enrollment);
                    await context.SaveChangesAsync();

                    // Vary Marks for Pass/Fail report
                    decimal marks = (decimal)(random.NextDouble() * 100);
                    LetterGrade letterGrade = marks switch
                    {
                        >= 90 => LetterGrade.A,
                        >= 85 => LetterGrade.AMinus,
                        >= 80 => LetterGrade.BPlus,
                        >= 75 => LetterGrade.B,
                        >= 70 => LetterGrade.BMinus,
                        >= 65 => LetterGrade.CPlus,
                        >= 60 => LetterGrade.C,
                        >= 50 => LetterGrade.D,
                        _ => LetterGrade.F
                    };

                    context.Grades.Add(new Grade
                    {
                        EnrollmentId = enrollment.Id,
                        Marks = Math.Round(marks, 2),
                        LetterGrade = letterGrade,
                        Remarks = marks >= 50 ? "Passed" : "Needs Improvement",
                        LastUpdated = DateTimeOffset.UtcNow
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedExamsAsync(AppDbContext context, List<Course> courses, Semester semester, List<Room> rooms, List<Instructor> instructors)
        {
            if (await context.Exams.AnyAsync()) return;

            foreach (var course in courses)
            {
                var exam = new Exam
                {
                    CourseId = course.Id,
                    SemesterId = semester.Id,
                    Type = ExamType.Midterm,
                    Date = DateTimeOffset.UtcNow.AddDays(30),
                    StartTime = "09:00",
                    Status = ExamStatus.Published,
                    DurationMinutes = 120,
                    HallId = rooms[0].Id
                };
                context.Exams.Add(exam);
                await context.SaveChangesAsync();

                context.ExamInvigilators.Add(new ExamInvigilator
                {
                    ExamId = exam.Id,
                    UserId = instructors[0].UserId
                });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedComplaintsAsync(AppDbContext context, List<Student> students, User admin)
        {
            if (await context.Complaints.AnyAsync()) return;

            int count = 1;
            foreach (var student in students.Take(10)) // Increased for variety
            {
                var status = (ComplaintStatus)(count % 3);
                var complaint = new Complaint
                {
                    StudentId = student.UserId,
                    TicketNumber = $"TKT-{count:D4}",
                    Title = count % 2 == 0 ? "Grade Review Request" : "Facility Issue",
                    Description = "Details of the complaint number " + count,
                    Type = count % 2 == 0 ? ComplaintType.Academic : ComplaintType.Facility,
                    Status = status,
                    LastResponseAt = DateTimeOffset.UtcNow
                };
                context.Complaints.Add(complaint);
                await context.SaveChangesAsync();

                context.ComplaintNotes.Add(new ComplaintNote
                {
                    ComplaintId = complaint.Id,
                    Text = "Initial investigation started for " + complaint.TicketNumber,
                    AuthorId = admin.Id
                });

                if (status == ComplaintStatus.Resolved)
                {
                    complaint.Response = "Issue resolved successfully.";
                    complaint.UpdatedAt = DateTimeOffset.UtcNow.AddHours(48); // For resolution time report
                }
                count++;
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedAnnouncementsAsync(AppDbContext context, User admin)
        {
            if (await context.Announcements.AnyAsync()) return;

            context.Announcements.Add(new Announcement
            {
                Title = "Welcome to the new academic year!",
                Body = "We are excited to have you back.",
                TargetType = AnnouncementTargetType.All,
                Priority = AnnouncementPriority.Normal,
                CreatedById = admin.Id
            });
            context.Announcements.Add(new Announcement
            {
                Title = "Urgent: Exam Schedule Update",
                Body = "Please check the updated schedule in the calendar.",
                TargetType = AnnouncementTargetType.All,
                Priority = AnnouncementPriority.Urgent,
                CreatedById = admin.Id
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedCalendarEventsAsync(AppDbContext context, Semester semester)
        {
            if (await context.CalendarEvents.AnyAsync()) return;

            context.CalendarEvents.Add(new CalendarEvent
            {
                Title = "Final Exams Period",
                Description = "Final exams will start on June 1st.",
                Date = DateTimeOffset.UtcNow.AddMonths(1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(1).AddDays(14),
                Type = CalendarEventType.Exam,
                IsPublic = true,
                SemesterId = semester.Id,
                ColorSeed = "red"
            });
            context.CalendarEvents.Add(new CalendarEvent
            {
                Title = "University Foundation Day",
                Description = "Annual celebration.",
                Date = DateTimeOffset.UtcNow.AddDays(15),
                Type = CalendarEventType.Holiday,
                IsPublic = true,
                SemesterId = semester.Id,
                ColorSeed = "green"
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedSystemSettingsAsync(AppDbContext context)
        {
            if (await context.SystemSettings.AnyAsync()) return;

            context.SystemSettings.Add(new SystemSetting { Key = "MaintenanceMode", ValueJson = "false", UpdatedAt = DateTimeOffset.UtcNow });
            context.SystemSettings.Add(new SystemSetting { Key = "MaxCreditsPerSemester", ValueJson = "21", UpdatedAt = DateTimeOffset.UtcNow });
            context.SystemSettings.Add(new SystemSetting { Key = "UniversityName", ValueJson = "\"Ibn Elgm3a University\"", UpdatedAt = DateTimeOffset.UtcNow });
            await context.SaveChangesAsync();
        }
    }
}
