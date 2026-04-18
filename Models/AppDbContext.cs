using IbnElgm3a.Enums;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;
using IbnElgm3a.Models.Converters;
using IbnElgm3a.Services;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace IbnElgm3a.Models
{
    public class AppDbContext : DbContext
    {
        private readonly IAesEncryptionService _encryptionService;

        public AppDbContext(DbContextOptions<AppDbContext> options, IAesEncryptionService encryptionService) : base(options) 
        { 
            _encryptionService = encryptionService;
        }

        // Core Users
        public DbSet<User> Users => Set<User>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Instructor> Instructors => Set<Instructor>();
        public DbSet<Guardian> Guardians => Set<Guardian>();
        public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
        public DbSet<UserDevice> Devices => Set<UserDevice>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<Feature> Features => Set<Feature>();

        // Structure
        public DbSet<Faculty> Faculties => Set<Faculty>();
        public DbSet<Department> Departments => Set<Department>();
        
        public DbSet<Room> Rooms => Set<Room>();

        // Academic & Logistics
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Semester> Semesters => Set<Semester>();
        public DbSet<Section> Sections => Set<Section>();
        public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();
        
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Grade> Grades => Set<Grade>();

        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<ExamInvigilator> ExamInvigilators => Set<ExamInvigilator>();

        // Features
        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<ComplaintNote> ComplaintNotes => Set<ComplaintNote>();
        public DbSet<SubAdmin> SubAdmins => Set<SubAdmin>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

        // Infrastructure
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Token> Tokens => Set<Token>();
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
        public DbSet<BulkImportJob> BulkImportJobs => Set<BulkImportJob>();

        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);

            // Register PostgreSQL Enums (Npgsql mapping)
            model.HasPostgresEnum<UserRole>();
            model.HasPostgresEnum<UserStatus>();
            model.HasPostgresEnum<ComplaintStatus>();
            model.HasPostgresEnum<ComplaintType>();
            model.HasPostgresEnum<DayOfWeekEnum>();
            model.HasPostgresEnum<ClassType>();
            model.HasPostgresEnum<ScheduleRecurrence>();
            model.HasPostgresEnum<ExamType>();
            model.HasPostgresEnum<ExamStatus>();
            model.HasPostgresEnum<SubAdminScopeType>();
            model.HasPostgresEnum<SeatingStrategy>();
            model.HasPostgresEnum<AnnouncementTargetType>();
            model.HasPostgresEnum<AnnouncementPriority>();
            model.HasPostgresEnum<CalendarEventType>();
            model.HasPostgresEnum<Gender>();
            model.HasPostgresEnum<EnrollmentStatus>();
            model.HasPostgresEnum<RelationType>();
            model.HasPostgresEnum<LetterGrade>();
            model.HasPostgresEnum<AccountType>();
            model.HasPostgresEnum<AppType>();
            model.HasPostgresEnum<RoomType>();

            // Unique Constraints
            model.Entity<User>().HasIndex(x => x.Email).IsUnique();
            model.Entity<Student>().HasIndex(x => x.AcademicNumber).IsUnique();
            model.Entity<Course>().HasIndex(x => x.CourseCode).IsUnique();
            model.Entity<Faculty>().HasIndex(x => x.Code).IsUnique();
            model.Entity<Department>().HasIndex(x => x.Code).IsUnique();

            // Precision and Config
            model.Entity<Student>().Property(x => x.GPA).HasPrecision(4, 2);
            
            // Encrypted Columns via Value Converters
            var encryptedString = new EncryptedStringConverter(_encryptionService);
            var encryptedDecimal = new EncryptedDecimalConverter(_encryptionService);

            model.Entity<User>().Property(x => x.NationalId).HasConversion(encryptedString);
            model.Entity<User>().Property(x => x.Phone).HasConversion(encryptedString);
            model.Entity<Guardian>().Property(x => x.NationalId).HasConversion(encryptedString);
            model.Entity<Guardian>().Property(x => x.Phone).HasConversion(encryptedString);
            model.Entity<Grade>().Property(x => x.Marks).HasConversion(encryptedDecimal);

            model.Entity<StudentGuardian>().HasKey(x => new { x.StudentId, x.GuardianId });

            // Explicit Relationships to avoid EF Core Ambiguity
            model.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId);

            model.Entity<Instructor>()
                .HasOne(i => i.User)
                .WithOne(u => u.Instructor)
                .HasForeignKey<Instructor>(i => i.UserId);

            model.Entity<User>()
                .HasOne(u => u.Faculty)
                .WithMany() // Assuming Faculty doesn't have an explicit ICollection of Users, or if it does, it's fine.
                .HasForeignKey(u => u.FacultyId);

            model.Entity<User>()
                .HasOne(u => u.Department)
                .WithMany()
                .HasForeignKey(u => u.DepartmentId);
                
            model.Entity<Faculty>()
                .HasOne(f => f.HeadOfFaculty)
                .WithMany()
                .HasForeignKey(f => f.HeadOfFacultyId);

            model.Entity<Enrollment>()
                .HasOne(e => e.Grade)
                .WithOne(g => g.Enrollment)
                .HasForeignKey<Grade>(g => g.EnrollmentId);

            model.Entity<Token>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            model.Entity<UserDevice>()
                .HasOne(d => d.User)
                .WithMany(u => u.Devices)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            model.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            model.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithMany(p => p.Roles)
                .UsingEntity(j => j.ToTable("RolePermissions"));

            model.Entity<Permission>()
                .HasOne(p => p.Feature)
                .WithMany(f => f.Permissions)
                .HasForeignKey(p => p.FeatureId);

            model.Entity<ComplaintNote>()
                .HasOne(n => n.Complaint)
                .WithMany(c => c.InternalNotes)
                .HasForeignKey(n => n.ComplaintId);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (BaseEntity)entityEntry.Entity;
                if (entityEntry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTimeOffset.UtcNow;
                }
                
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (BaseEntity)entityEntry.Entity;
                if (entityEntry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTimeOffset.UtcNow;
                }

                entity.UpdatedAt = DateTimeOffset.UtcNow;
            }

            return base.SaveChanges();
        }
    }
}
