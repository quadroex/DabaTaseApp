using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DabaTaseApp.Models;

public partial class Lab1Context : DbContext //IdentityDbContext<IdentityUser>
{
    public Lab1Context()
    {
    }

    public Lab1Context(DbContextOptions<Lab1Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<Instructor> Instructors { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PracticeSession> PracticeSessions { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<TheorySession> TheorySessions { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categories_pkey");

            entity.ToTable("categories");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("groups_pkey");

            entity.ToTable("groups");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.GroupName)
                .HasColumnType("character varying")
                .HasColumnName("group_name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.TheoryInstructorId).HasColumnName("theory_instructor_id");

            entity.HasOne(d => d.TheoryInstructor).WithMany(p => p.Groups)
                .HasForeignKey(d => d.TheoryInstructorId)
                .HasConstraintName("groups_theory_instructor_id_fkey");
        });

        modelBuilder.Entity<Instructor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("instructors_pkey");

            entity.ToTable("instructors");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FullName)
                .HasColumnType("character varying")
                .HasColumnName("full_name");
            entity.Property(e => e.LicenseSerial)
                .HasColumnType("character varying")
                .HasColumnName("license_serial");
            entity.Property(e => e.PhoneNumber)
                .HasColumnType("character varying")
                .HasColumnName("phone_number");

            entity.HasMany(d => d.CategoryNames).WithMany(p => p.Instructors)
                .UsingEntity<Dictionary<string, object>>(
                    "instructor_categories",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("category_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("instructor_categories_category_id_fkey"),
                    l => l.HasOne<Instructor>().WithMany()
                        .HasForeignKey("instructors_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("instructor_categories_instructors_id_fkey"),
                    j =>
                    {
                        j.HasKey("category_id", "instructors_id").HasName("instructor_categories_pkey");
                        j.ToTable("instructor_categories");
                        j.IndexerProperty<int>("category_id").HasColumnName("category_id");
                        j.IndexerProperty<int>("instructors_id").HasColumnName("instructors_id");
                    });
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("payment_date");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Student).WithMany(p => p.Payments)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("payments_student_id_fkey");
        });

        modelBuilder.Entity<PracticeSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("practice_sessions_pkey");

            entity.ToTable("practice_sessions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_time");
            entity.Property(e => e.InstructorId).HasColumnName("instructor_id");
            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.VehiclePlate)
                .HasColumnType("character varying")
                .HasColumnName("vehicle_plate");

            entity.Property(e => e.Status)
                .HasColumnType("character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Instructor).WithMany(p => p.PracticeSessions)
                .HasForeignKey(d => d.InstructorId)
                .HasConstraintName("practice_sessions_instructor_id_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.PracticeSessions)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("practice_sessions_student_id_fkey");

            entity.HasOne(d => d.VehiclePlateNavigation).WithMany(p => p.PracticeSessions)
                .HasForeignKey(d => d.VehiclePlate)
                .HasConstraintName("practice_sessions_vehicle_plate_fkey");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("students_pkey");
            entity.ToTable("students");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Balance).HasColumnName("balance");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.TargetCategory).HasColumnName("target_category");

            entity.HasOne(d => d.Group).WithMany(p => p.Students)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("students_group_id_fkey");
        });

        modelBuilder.Entity<TheorySession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("theory_sessions_pkey");

            entity.ToTable("theory_sessions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.InstructorId).HasColumnName("instructor_id");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_time");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Location)
                .HasColumnType("character varying")
                .HasColumnName("location");
            entity.Property(e => e.Status)
                .HasColumnType("character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Group).WithMany(p => p.TheorySessions)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("theory_sessions_group_id_fkey");

            entity.HasOne(d => d.Instructor).WithMany(p => p.TheorySessions)
                .HasForeignKey(d => d.InstructorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("theory_sessions_instructor_id_fkey");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.PlateNumber).HasName("vehicles_pkey");

            entity.ToTable("vehicles");

            entity.Property(e => e.PlateNumber)
                .HasColumnType("character varying")
                .HasColumnName("plate_number");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Mark)
                .HasColumnType("character varying")
                .HasColumnName("mark");
            entity.Property(e => e.Model)
                .HasColumnType("character varying")
                .HasColumnName("model");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

//using System;
//using System.Collections.Generic;
//using Microsoft.EntityFrameworkCore;

//namespace DabaTaseApp.Models;

//public partial class Lab1Context : DbContext
//{
//    public Lab1Context()
//    {
//    }

//    public Lab1Context(DbContextOptions<Lab1Context> options)
//        : base(options)
//    {
//    }

//    public virtual DbSet<Category> Categories { get; set; }

//    public virtual DbSet<Group> Groups { get; set; }

//    public virtual DbSet<Instructor> Instructors { get; set; }

//    public virtual DbSet<Payment> Payments { get; set; }

//    public virtual DbSet<PracticeSession> PracticeSessions { get; set; }

//    public virtual DbSet<Student> Students { get; set; }

//    public virtual DbSet<TheorySession> TheorySessions { get; set; }

//    public virtual DbSet<Vehicle> Vehicles { get; set; }

//    /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=lab1;Username=postgres;Password=1234");*/

//    protected override void OnModelCreating(ModelBuilder modelBuilder)
//    {
//        modelBuilder.Entity<Instructor>(entity =>
//        { 
//        .HasMany(i => i.CategoryNames)
//        .WithMany(c => c.Instructors)
//        .UsingEntity<Dictionary<string, object>>(
//            "instructor_categories",
//            j => j.HasOne<Category>().WithMany().HasForeignKey("category_id"),
//            j => j.HasOne<Instructor>().WithMany().HasForeignKey("instructors_id")
//        });

//        modelBuilder.Entity<TheorySession>(entity =>
//        {
//            entity.HasKey(e => e.Id);
//        });

//        modelBuilder.Entity<Group>(entity =>
//        {
//            entity.HasKey(e => e.Id).HasName("groups_pkey");

//            entity.ToTable("groups");

//            entity.Property(e => e.Id).HasColumnName("id");
//            entity.Property(e => e.EndDate).HasColumnName("end_date");
//            entity.Property(e => e.GroupName)
//                .HasColumnType("character varying")
//                .HasColumnName("group_name");
//            entity.Property(e => e.StartDate).HasColumnName("start_date");
//            entity.Property(e => e.TheoryInstructorId).HasColumnName("theory_instructor_id");

//            entity.HasOne(d => d.TheoryInstructor).WithMany(p => p.Groups)
//                .HasForeignKey(d => d.TheoryInstructorId)
//                .HasConstraintName("groups_theory_instructor_id_fkey");
//        });

//        modelBuilder.Entity<Instructor>(entity =>
//        {
//            entity.HasKey(e => e.Id).HasName("instructors_pkey");

//            entity.ToTable("instructors");

//            entity.Property(e => e.Id).HasColumnName("id");
//            entity.Property(e => e.FullName)
//                .HasColumnType("character varying")
//                .HasColumnName("full_name");
//            entity.Property(e => e.LicenseSerial)
//                .HasColumnType("character varying")
//                .HasColumnName("license_serial");
//            entity.Property(e => e.PhoneNumber)
//                .HasColumnType("character varying")
//                .HasColumnName("phone_number");
//        });

//        modelBuilder.Entity<Payment>(entity =>
//        {
//            entity.HasKey(e => e.Id).HasName("payments_pkey");

//            entity.ToTable("payments");

//            entity.Property(e => e.Id).HasColumnName("id");
//            entity.Property(e => e.Amount).HasColumnName("amount");
//            entity.Property(e => e.PaymentDate)
//                .HasColumnType("timestamp without time zone")
//                .HasColumnName("payment_date");
//            entity.Property(e => e.StudentId).HasColumnName("student_id");

//            entity.HasOne(d => d.Student).WithMany(p => p.Payments)
//                .HasForeignKey(d => d.StudentId)
//                .HasConstraintName("payments_student_id_fkey");
//        });

//        modelBuilder.Entity<PracticeSession>(entity =>
//        {
//            entity.HasKey(e => e.Id).HasName("practice_sessions_pkey");

//            entity.ToTable("practice_sessions");

//            entity.Property(e => e.Id).HasColumnName("id");
//            entity.Property(e => e.EndTime)
//                .HasColumnType("timestamp without time zone")
//                .HasColumnName("end_time");
//            entity.Property(e => e.InstructorId).HasColumnName("instructor_id");
//            entity.Property(e => e.StartTime)
//                .HasColumnType("timestamp without time zone")
//                .HasColumnName("start_time");
//            entity.Property(e => e.StudentId).HasColumnName("student_id");
//            entity.Property(e => e.VehiclePlate)
//                .HasColumnType("character varying")
//                .HasColumnName("vehicle_plate");

//            entity.Property(e => e.Status)
//                .HasColumnType("character varying")
//                .HasColumnName("status");

//            entity.HasOne(d => d.Instructor).WithMany(p => p.PracticeSessions)
//                .HasForeignKey(d => d.InstructorId)
//                .HasConstraintName("practice_sessions_instructor_id_fkey");

//            entity.HasOne(d => d.Student).WithMany(p => p.PracticeSessions)
//                .HasForeignKey(d => d.StudentId)
//                .HasConstraintName("practice_sessions_student_id_fkey");

//            entity.HasOne(d => d.VehiclePlateNavigation).WithMany(p => p.PracticeSessions)
//                .HasForeignKey(d => d.VehiclePlate)
//                .HasConstraintName("practice_sessions_vehicle_plate_fkey");
//        });

//        modelBuilder.Entity<Student>(entity =>
//        {
//            entity.HasKey(e => e.Id).HasName("students_pkey");

//            entity.ToTable("students");

//            entity.Property(e => e.Id).HasColumnName("id");
//            entity.Property(e => e.Balance).HasColumnName("balance");
//            entity.Property(e => e.FullName)
//                .HasColumnType("character varying")
//                .HasColumnName("full_name");
//            entity.Property(e => e.GroupId).HasColumnName("group_id");
//            entity.Property(e => e.TargetCategory)
//                .HasColumnType("character varying")
//                .HasColumnName("target_category");

//            entity.HasOne(d => d.Group).WithMany(p => p.Students)
//                .HasForeignKey(d => d.GroupId)
//                .HasConstraintName("students_group_id_fkey");
//        });

//        modelBuilder.Entity<TheorySession>(entity =>
//        {
//            entity.HasKey(e => e.Id).HasName("theory_sessions_pkey");

//            entity.ToTable("theory_sessions");

//            entity.Property(e => e.StartTime)
//                .HasColumnType("timestamp without time zone")
//                .HasColumnName("start_time");
//            entity.Property(e => e.InstructorId).HasColumnName("instructor_id");
//            entity.Property(e => e.EndTime)
//                .HasColumnType("timestamp without time zone")
//                .HasColumnName("end_time");
//            entity.Property(e => e.GroupId).HasColumnName("group_id");
//            entity.Property(e => e.Location)
//                .HasColumnType("character varying")
//                .HasColumnName("location");

//            entity.Property(e => e.Status)
//                .HasColumnType("character varying")
//                .HasColumnName("status");

//            entity.HasOne(d => d.Group).WithMany(p => p.TheorySessions)
//                .HasForeignKey(d => d.GroupId)
//                .HasConstraintName("theory_sessions_group_id_fkey");

//            entity.HasOne(d => d.Instructor).WithMany(p => p.TheorySessions)
//                .HasForeignKey(d => d.InstructorId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("theory_sessions_instructor_id_fkey");
//        });

//        modelBuilder.Entity<Vehicle>(entity =>
//        {
//            entity.HasKey(e => e.PlateNumber).HasName("vehicles_pkey");

//            entity.ToTable("vehicles");

//            entity.Property(e => e.PlateNumber)
//                .HasColumnType("character varying")
//                .HasColumnName("plate_number");
//            entity.Property(e => e.IsActive).HasColumnName("is_active");
//            entity.Property(e => e.Mark)
//                .HasColumnType("character varying")
//                .HasColumnName("mark");
//            entity.Property(e => e.Model)
//                .HasColumnType("character varying")
//                .HasColumnName("model");
//        });

//        OnModelCreatingPartial(modelBuilder);
//    }

//    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
//}
