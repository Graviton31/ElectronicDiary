using System;
using System.Collections.Generic;
using ElectronicDiaryApi.Models;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace ElectronicDiaryApi.Data;

public partial class ElectronicDiaryContext : DbContext
{
    public ElectronicDiaryContext()
    {
    }

    public ElectronicDiaryContext(DbContextOptions<ElectronicDiaryContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EnrollmentRequest> EnrollmentRequests { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<Journal> Journals { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Parent> Parents { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<ScheduleChange> ScheduleChanges { get; set; }

    public virtual DbSet<StandardSchedule> StandardSchedules { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Visit> Visits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.IdEmployee).HasName("PRIMARY");

            entity.ToTable("employees");

            entity.HasIndex(e => e.IdPost, "fk_users_posts1_idx");

            entity.Property(e => e.IdEmployee).HasColumnName("id_employee");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.IdPost).HasColumnName("id_post");
            entity.Property(e => e.IsDelete)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_delete");
            entity.Property(e => e.Login)
                .HasMaxLength(20)
                .HasColumnName("login");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Patronymic)
                .HasMaxLength(20)
                .HasColumnName("patronymic");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasColumnType("enum('администратор','руководитель','учитель')")
                .HasColumnName("role");
            entity.Property(e => e.Surname)
                .HasMaxLength(20)
                .HasColumnName("surname");

            entity.HasOne(d => d.IdPostNavigation).WithMany(p => p.Employees)
                .HasForeignKey(d => d.IdPost)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_users_posts1");
        });

        modelBuilder.Entity<EnrollmentRequest>(entity =>
        {
            entity.HasKey(e => e.IdRequests).HasName("PRIMARY");

            entity.ToTable("enrollment_requests");

            entity.HasIndex(e => e.IdGroup, "fk_enrollment_requests_groups1_idx");

            entity.HasIndex(e => e.IdParent, "fk_enrollment_requests_parents1_idx");

            entity.HasIndex(e => e.IdStudent, "fk_enrollment_requests_students1_idx");

            entity.Property(e => e.IdRequests).HasColumnName("id_requests");
            entity.Property(e => e.IdGroup).HasColumnName("id_group");
            entity.Property(e => e.IdParent).HasColumnName("id_parent");
            entity.Property(e => e.IdStudent).HasColumnName("id_student");
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("request_date");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'ожидает'")
                .HasColumnType("enum('ожидает','одобрено','отклонено')")
                .HasColumnName("status");

            entity.HasOne(d => d.IdGroupNavigation).WithMany(p => p.EnrollmentRequests)
                .HasForeignKey(d => d.IdGroup)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_enrollment_requests_groups1");

            entity.HasOne(d => d.IdParentNavigation).WithMany(p => p.EnrollmentRequests)
                .HasForeignKey(d => d.IdParent)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_enrollment_requests_parents1");

            entity.HasOne(d => d.IdStudentNavigation).WithMany(p => p.EnrollmentRequests)
                .HasForeignKey(d => d.IdStudent)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_enrollment_requests_students1");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.IdGroup).HasName("PRIMARY");

            entity.ToTable("groups");

            entity.HasIndex(e => e.IdLocation, "fk_groups_locations1_idx");

            entity.HasIndex(e => e.IdSubject, "fk_groups_subjects1_idx");

            entity.Property(e => e.IdGroup).HasColumnName("id_group");
            entity.Property(e => e.Classroom)
                .HasMaxLength(20)
                .HasColumnName("classroom");
            entity.Property(e => e.IdLocation).HasColumnName("id_location");
            entity.Property(e => e.IdSubject).HasColumnName("id_subject");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");
            entity.Property(e => e.StudentCount).HasColumnName("student_count");

            entity.HasOne(d => d.IdLocationNavigation).WithMany(p => p.Groups)
                .HasForeignKey(d => d.IdLocation)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_groups_locations1");

            entity.HasOne(d => d.IdSubjectNavigation).WithMany(p => p.Groups)
                .HasForeignKey(d => d.IdSubject)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_groups_subjects1");

            entity.HasMany(d => d.IdEmployees).WithMany(p => p.IdGroups)
                .UsingEntity<Dictionary<string, object>>(
                    "GroupsHasEmployee",
                    r => r.HasOne<Employee>().WithMany()
                        .HasForeignKey("IdEmployee")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_groups_has_employees_employees1"),
                    l => l.HasOne<Group>().WithMany()
                        .HasForeignKey("IdGroup")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_groups_has_employees_groups1"),
                    j =>
                    {
                        j.HasKey("IdGroup", "IdEmployee")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("groups_has_employees");
                        j.HasIndex(new[] { "IdEmployee" }, "fk_groups_has_employees_employees1_idx");
                        j.HasIndex(new[] { "IdGroup" }, "fk_groups_has_employees_groups1_idx");
                        j.IndexerProperty<int>("IdGroup").HasColumnName("id_group");
                        j.IndexerProperty<int>("IdEmployee").HasColumnName("id_employee");
                    });

            entity.HasMany(d => d.IdStudents).WithMany(p => p.IdGroups)
                .UsingEntity<Dictionary<string, object>>(
                    "GroupsHasStudent",
                    r => r.HasOne<Student>().WithMany()
                        .HasForeignKey("IdStudent")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_groups_has_students_students1"),
                    l => l.HasOne<Group>().WithMany()
                        .HasForeignKey("IdGroup")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_groups_has_students_groups1"),
                    j =>
                    {
                        j.HasKey("IdGroup", "IdStudent")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("groups_has_students");
                        j.HasIndex(new[] { "IdGroup" }, "fk_groups_has_students_groups1_idx");
                        j.HasIndex(new[] { "IdStudent" }, "fk_groups_has_students_students1_idx");
                        j.IndexerProperty<int>("IdGroup").HasColumnName("id_group");
                        j.IndexerProperty<int>("IdStudent").HasColumnName("id_student");
                    });
        });

        modelBuilder.Entity<Journal>(entity =>
        {
            entity.HasKey(e => e.IdJournal).HasName("PRIMARY");

            entity.ToTable("journals");

            entity.HasIndex(e => e.IdGroup, "fk_journals_groups1_idx");

            entity.Property(e => e.IdJournal).HasColumnName("id_journal");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IdGroup).HasColumnName("id_group");
            entity.Property(e => e.LessonsCount).HasColumnName("lessons_count");
            entity.Property(e => e.StartDate).HasColumnName("start_date");

            entity.HasOne(d => d.IdGroupNavigation).WithMany(p => p.Journals)
                .HasForeignKey(d => d.IdGroup)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_journals_groups1");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.IdLesson).HasName("PRIMARY");

            entity.ToTable("lessons");

            entity.HasIndex(e => e.IdJournal, "fk_lessons_journals1_idx");

            entity.Property(e => e.IdLesson).HasColumnName("id_lesson");
            entity.Property(e => e.IdJournal).HasColumnName("id_journal");
            entity.Property(e => e.LessonDate).HasColumnName("lesson_date");

            entity.HasOne(d => d.IdJournalNavigation).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.IdJournal)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lessons_journals1");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.IdLocation).HasName("PRIMARY");

            entity.ToTable("locations");

            entity.Property(e => e.IdLocation).HasColumnName("id_location");
            entity.Property(e => e.Addres)
                .HasMaxLength(100)
                .HasColumnName("addres");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.IdMaterial).HasName("PRIMARY");

            entity.ToTable("materials");

            entity.HasIndex(e => e.IdMessage, "fk_materials_messages1_idx");

            entity.Property(e => e.IdMaterial).HasColumnName("id_material");
            entity.Property(e => e.IdMessage).HasColumnName("id_message");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.HasOne(d => d.IdMessageNavigation).WithMany(p => p.Materials)
                .HasForeignKey(d => d.IdMessage)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_materials_messages1");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.IdMessage).HasName("PRIMARY");

            entity.ToTable("messages");

            entity.HasIndex(e => e.IdLesson, "fk_messages_lessons1_idx");

            entity.Property(e => e.IdMessage).HasColumnName("id_message");
            entity.Property(e => e.IdLesson).HasColumnName("id_lesson");
            entity.Property(e => e.Text)
                .HasMaxLength(500)
                .HasColumnName("text");
            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .HasColumnName("title");

            entity.HasOne(d => d.IdLessonNavigation).WithMany(p => p.Messages)
                .HasForeignKey(d => d.IdLesson)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_messages_lessons1");
        });

        modelBuilder.Entity<Parent>(entity =>
        {
            entity.HasKey(e => e.IdParent).HasName("PRIMARY");

            entity.ToTable("parents");

            entity.Property(e => e.IdParent).HasColumnName("id_parent");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.IsDelete)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_delete");
            entity.Property(e => e.Login)
                .HasMaxLength(30)
                .HasColumnName("login");
            entity.Property(e => e.Name)
                .HasMaxLength(45)
                .HasColumnName("name");
            entity.Property(e => e.ParentRole)
                .HasColumnType("enum('отец','мать','отчим','мачеха','опекун','другое')")
                .HasColumnName("parent_role");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Patronymic)
                .HasMaxLength(45)
                .HasColumnName("patronymic");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Surname)
                .HasMaxLength(45)
                .HasColumnName("surname");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.IdPost).HasName("PRIMARY");

            entity.ToTable("posts");

            entity.HasIndex(e => e.PostName, "name_UNIQUE").IsUnique();

            entity.Property(e => e.IdPost).HasColumnName("id_post");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.PostName)
                .HasMaxLength(100)
                .HasColumnName("post_name");
        });

        modelBuilder.Entity<ScheduleChange>(entity =>
        {
            entity.HasKey(e => e.IdScheduleChange).HasName("PRIMARY");

            entity.ToTable("schedule_changes");

            entity.HasIndex(e => e.IdGroup, "fk_schedule_changes_groups1_idx");

            entity.HasIndex(e => e.IdSchedule, "fk_schedule_changes_standard_schedule1_idx");

            entity.Property(e => e.IdScheduleChange).HasColumnName("id_schedule_change");
            entity.Property(e => e.ChangeType)
                .HasColumnType("enum('перенос','отмена','дополнительное')")
                .HasColumnName("change_type");
            entity.Property(e => e.IdGroup).HasColumnName("id_group");
            entity.Property(e => e.IdSchedule).HasColumnName("id_schedule");
            entity.Property(e => e.NewDate).HasColumnName("new_date");
            entity.Property(e => e.NewEndTime)
                .HasColumnType("time")
                .HasColumnName("new_end_time");
            entity.Property(e => e.NewStartTime)
                .HasColumnType("time")
                .HasColumnName("new_start_time");
            entity.Property(e => e.OldDate).HasColumnName("old_date");

            entity.HasOne(d => d.IdGroupNavigation).WithMany(p => p.ScheduleChanges)
                .HasForeignKey(d => d.IdGroup)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_schedule_changes_groups1");

            entity.HasOne(d => d.IdScheduleNavigation).WithMany(p => p.ScheduleChanges)
                .HasForeignKey(d => d.IdSchedule)
                .HasConstraintName("fk_schedule_changes_standard_schedule1");
        });

        modelBuilder.Entity<StandardSchedule>(entity =>
        {
            entity.HasKey(e => e.IdStandardSchedule).HasName("PRIMARY");

            entity.ToTable("standard_schedule");

            entity.HasIndex(e => e.IdGroup, "fk_standard_schedule_groups1_idx");

            entity.Property(e => e.IdStandardSchedule).HasColumnName("id_standard_schedule");
            entity.Property(e => e.EndTime)
                .HasColumnType("time")
                .HasColumnName("end_time");
            entity.Property(e => e.IdGroup).HasColumnName("id_group");
            entity.Property(e => e.StartTime)
                .HasColumnType("time")
                .HasColumnName("start_time");
            entity.Property(e => e.WeekDay).HasColumnName("week_day");

            entity.HasOne(d => d.IdGroupNavigation).WithMany(p => p.StandardSchedules)
                .HasForeignKey(d => d.IdGroup)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_standard_schedule_groups1");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.IdStudent).HasName("PRIMARY");

            entity.ToTable("students");

            entity.Property(e => e.IdStudent).HasColumnName("id_student");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.IsDelete)
                .HasMaxLength(45)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_delete");
            entity.Property(e => e.Login)
                .HasMaxLength(30)
                .HasColumnName("login");
            entity.Property(e => e.Name)
                .HasMaxLength(45)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Patronymic)
                .HasMaxLength(45)
                .HasColumnName("patronymic");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Surname)
                .HasMaxLength(45)
                .HasColumnName("surname");

            entity.HasMany(d => d.IdParents).WithMany(p => p.IdStudents)
                .UsingEntity<Dictionary<string, object>>(
                    "StudentsHasParent",
                    r => r.HasOne<Parent>().WithMany()
                        .HasForeignKey("IdParent")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_students_has_parents_parents1"),
                    l => l.HasOne<Student>().WithMany()
                        .HasForeignKey("IdStudent")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_students_has_parents_students1"),
                    j =>
                    {
                        j.HasKey("IdStudent", "IdParent")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("students_has_parents");
                        j.HasIndex(new[] { "IdParent" }, "fk_students_has_parents_parents1_idx");
                        j.HasIndex(new[] { "IdStudent" }, "fk_students_has_parents_students1_idx");
                        j.IndexerProperty<int>("IdStudent").HasColumnName("id_student");
                        j.IndexerProperty<int>("IdParent").HasColumnName("id_parent");
                    });
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.IdSubject).HasName("PRIMARY");

            entity.ToTable("subjects");

            entity.Property(e => e.IdSubject).HasColumnName("id_subject");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.FullName)
                .HasMaxLength(40)
                .HasColumnName("full_name");
            entity.Property(e => e.IsDelete).HasColumnName("is_delete");
            entity.Property(e => e.LessonLength).HasColumnName("lesson_length");
            entity.Property(e => e.LessonsCount).HasColumnName("lessons_count");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");

            entity.HasMany(d => d.IdEmployees).WithMany(p => p.IdSubjects)
                .UsingEntity<Dictionary<string, object>>(
                    "SubjectsHasEmployee",
                    r => r.HasOne<Employee>().WithMany()
                        .HasForeignKey("IdEmployee")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_subjects_has_users_users1"),
                    l => l.HasOne<Subject>().WithMany()
                        .HasForeignKey("IdSubject")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_subjects_has_users_subjects1"),
                    j =>
                    {
                        j.HasKey("IdSubject", "IdEmployee")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("subjects_has_employee");
                        j.HasIndex(new[] { "IdSubject" }, "fk_subjects_has_users_subjects1_idx");
                        j.HasIndex(new[] { "IdEmployee" }, "fk_subjects_has_users_users1_idx");
                        j.IndexerProperty<int>("IdSubject").HasColumnName("id_subject");
                        j.IndexerProperty<int>("IdEmployee").HasColumnName("id_employee");
                    });
        });

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(e => e.IdVisit).HasName("PRIMARY");

            entity.ToTable("visits");

            entity.HasIndex(e => e.IdLesson, "fk_visits_lessons1_idx");

            entity.HasIndex(e => e.IdStudent, "fk_visits_students1_idx");

            entity.Property(e => e.IdVisit).HasColumnName("id_visit");
            entity.Property(e => e.Comment)
                .HasMaxLength(500)
                .HasColumnName("comment");
            entity.Property(e => e.IdLesson).HasColumnName("id_lesson");
            entity.Property(e => e.IdStudent).HasColumnName("id_student");
            entity.Property(e => e.UnvisitedStatuses)
                .HasColumnType("enum('н','б','у/п','к')")
                .HasColumnName("unvisited_statuses");

            entity.HasOne(d => d.IdLessonNavigation).WithMany(p => p.Visits)
                .HasForeignKey(d => d.IdLesson)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_visits_lessons1");

            entity.HasOne(d => d.IdStudentNavigation).WithMany(p => p.Visits)
                .HasForeignKey(d => d.IdStudent)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_visits_students1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
