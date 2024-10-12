using ExamProcessManage.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Data
{
    public partial class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AcademicYear> AcademicYears { get; set; } = null!;
        public virtual DbSet<Course> Courses { get; set; } = null!;
        public virtual DbSet<Department> Departments { get; set; } = null!;
        public virtual DbSet<Exam> Exams { get; set; } = null!;
        public virtual DbSet<ExamSet> ExamSets { get; set; } = null!;
        public virtual DbSet<Major> Majors { get; set; } = null!;
        public virtual DbSet<Permission> Permissions { get; set; } = null!;
        public virtual DbSet<Proposal> Proposals { get; set; } = null!;
        public virtual DbSet<Role> Roles { get; set; } = null!;
        public virtual DbSet<RolesPermission> RolesPermissions { get; set; } = null!;
        public virtual DbSet<Teacher> Teachers { get; set; } = null!;
        public virtual DbSet<TeacherProposal> TeacherProposals { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql("name=ConnectionStrings:DefaultConnection", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.36-mysql"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8mb4_0900_ai_ci")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<AcademicYear>(entity =>
            {
                entity.ToTable("academic_years");

                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");

                entity.Property(e => e.EndYear).HasColumnName("end_year");

                entity.Property(e => e.StartYear).HasColumnName("start_year");

                entity.Property(e => e.YearName)
                    .HasMaxLength(255)
                    .HasColumnName("year_name");
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("courses");

                entity.HasIndex(e => e.MajorId, "major_id");

                entity.Property(e => e.CourseId).HasColumnName("course_id");

                entity.Property(e => e.CourseCode)
                    .HasMaxLength(20)
                    .HasColumnName("course_code");

                entity.Property(e => e.CourseCredit).HasColumnName("course_credit");

                entity.Property(e => e.CourseName)
                    .HasMaxLength(255)
                    .HasColumnName("course_name");

                entity.Property(e => e.MajorId).HasColumnName("major_id");

                entity.HasOne(d => d.Major)
                    .WithMany(p => p.Courses)
                    .HasForeignKey(d => d.MajorId)
                    .HasConstraintName("courses_ibfk_1");
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("departments");

                entity.Property(e => e.DepartmentId).HasColumnName("department_id");

                entity.Property(e => e.DepartmentName)
                    .HasMaxLength(255)
                    .HasColumnName("department_name");
            });

            modelBuilder.Entity<Exam>(entity =>
            {
                entity.ToTable("exams");

                entity.HasIndex(e => e.AcademicYearId, "academic_year_id");

                entity.HasIndex(e => e.CreatorId, "creator_id");

                entity.HasIndex(e => e.ExamSetId, "exam_set_id");

                entity.Property(e => e.ExamId).HasColumnName("exam_id");

                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
                entity.Property(e => e.CreatorId).HasColumnName("creator_id");

                entity.Property(e => e.AttachedFile)
                    .HasMaxLength(255)
                    .HasColumnName("attached_file");

                entity.Property(e => e.Comment)
                    .HasColumnType("text")
                    .HasColumnName("comment");

                entity.Property(e => e.Description)
                    .HasColumnType("text")
                    .HasColumnName("description");

                entity.Property(e => e.ExamCode)
                    .HasMaxLength(50)
                    .HasColumnName("exam_code");

                entity.Property(e => e.ExamName)
                    .HasMaxLength(255)
                    .HasColumnName("exam_name");

                entity.Property(e => e.ExamSetId).HasColumnName("exam_set_id");

                entity.Property(e => e.Status)
                    .HasColumnType("enum('in_progress','pending_approval','approved','rejected')")
                    .HasColumnName("status")
                    .HasDefaultValueSql("'in_progress'");

                entity.Property(e => e.UploadDate).HasColumnName("upload_date");

                entity.HasOne(d => d.AcademicYear)
                    .WithMany(p => p.Exams)
                    .HasForeignKey(d => d.AcademicYearId)
                    .HasConstraintName("exams_ibfk_2");

                entity.HasOne(d => d.ExamSet)
                    .WithMany(p => p.Exams)
                    .HasForeignKey(d => d.ExamSetId)
                    .HasConstraintName("exams_ibfk_1");

                // Ngăn trùng lặp
                entity.HasIndex(e => e.ExamCode)
                    .IsUnique()
                    .HasDatabaseName("IX_Unique_ExamCode");

                entity.HasIndex(e => e.ExamName)
                    .IsUnique()
                    .HasDatabaseName("IX_Unique_ExamName");

                entity.HasIndex(e => e.AttachedFile)
                    .IsUnique()
                    .HasDatabaseName("IX_Unique_AttachedFile");
            });

            modelBuilder.Entity<ExamSet>(entity =>
            {
                entity.ToTable("exam_sets");

                entity.HasIndex(e => e.CourseId, "course_id");

                entity.HasIndex(e => e.CreatorId, "creator_id");

                entity.HasIndex(e => e.ProposalId, "proposal_id");

                entity.Property(e => e.ExamSetId).HasColumnName("exam_set_id");

                entity.Property(e => e.CourseId).HasColumnName("course_id");

                entity.Property(e => e.CreatorId).HasColumnName("creator_id");

                entity.Property(e => e.DepartmentId)
                    .HasMaxLength(50)
                    .HasColumnName("department_id");

                entity.Property(e => e.Description)
                    .HasColumnType("text")
                    .HasColumnName("description");

                entity.Property(e => e.ExamQuantity).HasColumnName("exam_quantity");

                entity.Property(e => e.ExamSetName)
                    .HasMaxLength(255)
                    .HasColumnName("exam_set_name");

                entity.Property(e => e.MajorId)
                    .HasMaxLength(50)
                    .HasColumnName("major_id");

                entity.Property(e => e.ProposalId).HasColumnName("proposal_id");

                entity.Property(e => e.Status)
                    .HasColumnType("enum('in_progress','pending_approval','approved','rejected')")
                    .HasColumnName("status")
                    .HasDefaultValueSql("'in_progress'");

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.ExamSets)
                    .HasForeignKey(d => d.CourseId)
                    .HasConstraintName("exam_sets_ibfk_1");

                entity.HasOne(d => d.Proposal)
                    .WithMany(p => p.ExamSets)
                    .HasForeignKey(d => d.ProposalId)
                    .HasConstraintName("exam_sets_ibfk_2");
            });

            modelBuilder.Entity<Major>(entity =>
            {
                entity.ToTable("majors");

                entity.HasIndex(e => e.DepartmentId, "department_id");

                entity.Property(e => e.MajorId).HasColumnName("major_id");

                entity.Property(e => e.DepartmentId).HasColumnName("department_id");

                entity.Property(e => e.MajorName)
                    .HasMaxLength(255)
                    .HasColumnName("major_name");

                entity.HasOne(d => d.Department)
                    .WithMany(p => p.Majors)
                    .HasForeignKey(d => d.DepartmentId)
                    .HasConstraintName("majors_ibfk_1");
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("permissions");

                entity.UseCollation("utf8mb4_unicode_ci");

                entity.HasIndex(e => e.Slug, "permissions_slug_unique")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamp")
                    .HasColumnName("created_at");

                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");

                entity.Property(e => e.Slug).HasColumnName("slug");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp")
                    .HasColumnName("updated_at");
            });

            modelBuilder.Entity<Proposal>(entity =>
            {
                entity.ToTable("proposals");

                entity.Property(e => e.ProposalId).HasColumnName("proposal_id");

                entity.Property(e => e.AcademicYear)
                    .HasMaxLength(10)
                    .HasColumnName("academic_year");

                entity.Property(e => e.Content)
                    .HasColumnType("text")
                    .HasColumnName("content");

                entity.Property(e => e.EndDate).HasColumnName("end_date");

                entity.Property(e => e.PlanCode)
                    .HasMaxLength(50)
                    .HasColumnName("plan_code");

                entity.Property(e => e.Semester)
                    .HasMaxLength(10)
                    .HasColumnName("semester");

                entity.Property(e => e.StartDate).HasColumnName("start_date");

                entity.Property(e => e.Status)
                    .HasColumnType("enum('in_progress','pending_approval','approved','rejected')")
                    .HasColumnName("status")
                    .HasDefaultValueSql("'in_progress'");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");

                entity.UseCollation("utf8mb4_unicode_ci");

                entity.HasIndex(e => e.Slug, "roles_slug_unique")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamp")
                    .HasColumnName("created_at");

                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");

                entity.Property(e => e.Slug).HasColumnName("slug");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp")
                    .HasColumnName("updated_at");
            });

            modelBuilder.Entity<RolesPermission>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("roles_permissions");

                entity.UseCollation("utf8mb4_unicode_ci");

                entity.HasIndex(e => e.PermissionId, "roles_permissions_permission_id_foreign");

                entity.HasIndex(e => e.RoleId, "roles_permissions_role_id_foreign");

                entity.Property(e => e.PermissionId).HasColumnName("permission_id");

                entity.Property(e => e.RoleId).HasColumnName("role_id");

                entity.HasOne(d => d.Permission)
                    .WithMany()
                    .HasForeignKey(d => d.PermissionId)
                    .HasConstraintName("roles_permissions_permission_id_foreign");

                entity.HasOne(d => d.Role)
                    .WithMany()
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("roles_permissions_role_id_foreign");
            });

            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.ToTable("teachers");

                entity.UseCollation("utf8mb4_unicode_ci");

                entity.HasIndex(e => e.Slug, "teachers_slug_unique")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Address)
                    .HasMaxLength(255)
                    .HasColumnName("address");

                entity.Property(e => e.CoverPath)
                    .HasMaxLength(255)
                    .HasColumnName("cover_path");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamp")
                    .HasColumnName("created_at");

                entity.Property(e => e.DeletedAt)
                    .HasColumnType("timestamp")
                    .HasColumnName("deleted_at");

                entity.Property(e => e.IsPinned).HasColumnName("is_pinned");

                entity.Property(e => e.LinkFacebook)
                    .HasMaxLength(255)
                    .HasColumnName("link_facebook");

                entity.Property(e => e.LinkInsta)
                    .HasMaxLength(255)
                    .HasColumnName("link_insta");

                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");

                entity.Property(e => e.Position)
                    .HasMaxLength(255)
                    .HasColumnName("position");

                entity.Property(e => e.ProgressTeach).HasColumnName("progress_teach");

                entity.Property(e => e.ReseachArea).HasColumnName("reseach_area");

                entity.Property(e => e.Slug).HasColumnName("slug");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp")
                    .HasColumnName("updated_at");
            });

            modelBuilder.Entity<TeacherProposal>(entity =>
            {
                entity.ToTable("teacher_proposal");

                entity.HasIndex(e => e.ProposalId, "proposal_id");

                entity.HasIndex(e => e.UserId, "user_id");

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("id");

                entity.Property(e => e.ProposalId).HasColumnName("proposal_id");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.Proposal)
                    .WithMany(p => p.TeacherProposals)
                    .HasForeignKey(d => d.ProposalId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("teacher_proposal_ibfk_2");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.TeacherProposals)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("teacher_proposal_ibfk_1");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.UseCollation("utf8mb4_unicode_ci");

                entity.HasIndex(e => e.Email, "users_email_unique")
                    .IsUnique();

                entity.HasIndex(e => e.RoleId, "users_role_id_foreign");

                entity.HasIndex(e => e.TeacherId, "users_teacher_id_foreign");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AvatarPath)
                    .HasMaxLength(255)
                    .HasColumnName("avatar_path");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamp")
                    .HasColumnName("created_at");

                entity.Property(e => e.DeletedAt)
                    .HasColumnType("timestamp")
                    .HasColumnName("deleted_at");

                entity.Property(e => e.Email).HasColumnName("email");

                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");

                entity.Property(e => e.Password)
                    .HasMaxLength(255)
                    .HasColumnName("password");

                entity.Property(e => e.RememberToken)
                    .HasMaxLength(100)
                    .HasColumnName("remember_token");

                entity.Property(e => e.RoleId).HasColumnName("role_id");

                entity.Property(e => e.TeacherId).HasColumnName("teacher_id");

                entity.Property(e => e.TokenProcess)
                    .HasMaxLength(255)
                    .HasColumnName("token_process");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp")
                    .HasColumnName("updated_at");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("users_role_id_foreign");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.TeacherId)
                    .HasConstraintName("users_teacher_id_foreign");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
