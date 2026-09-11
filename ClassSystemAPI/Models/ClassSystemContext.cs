//using System.Data.Entity;

//namespace ClassSystemAPI.Models
//{
//    public class ClassSystemContext : DbContext
//    {
//        public ClassSystemContext() : base("name=ClassSystemDB") { }

//        public DbSet<User> Users { get; set; }
//        public DbSet<UserType> UserTypes { get; set; }
//        public DbSet<Teacher> Teachers { get; set; }
//        public DbSet<Student> Students { get; set; }
//        public DbSet<Subject> Subjects { get; set; }
//        public DbSet<Course> Courses { get; set; }
//        public DbSet<CourseSubjects> CourseSubjects { get; set; }
//        public DbSet<ClassSchedule> ClassSchedules { get; set; }
//        public DbSet<Enrollment> Enrollments { get; set; }
//        public DbSet<Announcement> Announcements { get; set; }
//        public DbSet<StudyMaterial> StudyMaterials { get; set; }
//        public DbSet<SystemLog> SystemLogs { get; set; }

//        public DbSet<QRSession> QRSessions { get; set; }
//        public DbSet<Attendance> Attendances { get; set; }

//        protected override void OnModelCreating(DbModelBuilder modelBuilder)
//        {
//            modelBuilder.Entity<SystemLog>()
//                .HasOptional(s => s.User)
//                .WithMany() // or .WithMany(u => u.SystemLogs) if you add collection navigation
//                .HasForeignKey(s => s.UserID)
//                .WillCascadeOnDelete(true);

//            // Configure CourseSubjects composite key and relationships
//            modelBuilder.Entity<CourseSubjects>()
//                .HasKey(cs => new { cs.CourseID, cs.SubjectID });

//            modelBuilder.Entity<CourseSubjects>()
//                .HasRequired(cs => cs.Course)
//                .WithMany(c => c.CourseSubjects)
//                .HasForeignKey(cs => cs.CourseID)
//                .WillCascadeOnDelete(false);

//            modelBuilder.Entity<CourseSubjects>()
//                .HasRequired(cs => cs.Subject)
//                .WithMany(s => s.CourseSubjects)
//                .HasForeignKey(cs => cs.SubjectID)
//                .WillCascadeOnDelete(false);

//            base.OnModelCreating(modelBuilder);
//        }
//    }
//}

using System.Collections.Generic;
using System.Data.Entity;

namespace ClassSystemAPI.Models
{
    public class ClassSystemContext : DbContext
    {
        public ClassSystemContext() : base("name=ClassSystemDB") { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserType> UserTypes { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseSubjects> CourseSubjects { get; set; }
        public DbSet<ClassSchedule> ClassSchedules { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<StudyMaterial> StudyMaterials { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        public DbSet<QRSession> QRSessions { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Payment> Payment { get; set; }
        public DbSet<PaymentHistory> PaymentHistory { get; set; }

        //public IEnumerable<object> QRSessionAttendances { get; internal set; }

        //  public object QRSessionAttendances { get; internal set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SystemLog>()
                .HasOptional(s => s.User)
                .WithMany() // or .WithMany(u => u.SystemLogs) if you add collection navigation
                .HasForeignKey(s => s.UserID)
                .WillCascadeOnDelete(true);

            // Configure CourseSubjects composite key and relationships
            modelBuilder.Entity<CourseSubjects>()
                .HasKey(cs => new { cs.CourseID, cs.SubjectID });

            modelBuilder.Entity<CourseSubjects>()
                .HasRequired(cs => cs.Course)
                .WithMany(c => c.CourseSubjects)
                .HasForeignKey(cs => cs.CourseID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CourseSubjects>()
                .HasRequired(cs => cs.Subject)
                .WithMany(s => s.CourseSubjects)
                .HasForeignKey(cs => cs.SubjectID)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
