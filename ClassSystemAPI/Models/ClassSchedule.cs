//using System;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace ClassSystemAPI.Models
//{
//    public class ClassSchedule
//    {
//        [Key]
//        public int ScheduleID { get; set; }

//        public int CourseID { get; set; }

//        // New FK column
//        public int? SubjectID { get; set; }   // nullable because the DB column is NULL

//        public byte DayOfWeek { get; set; } // 1 = Sunday, 2 = Monday...

//        public TimeSpan StartTime { get; set; }

//        public TimeSpan EndTime { get; set; }

//        [StringLength(20)]
//        public string RoomNumber { get; set; }

//        public bool IsRecurring { get; set; } = true;

//        // Navigation: Course
//        [ForeignKey("CourseID")]
//        public virtual Course Course { get; set; }

//        // Navigation: Subject (NEW)
//        [ForeignKey("SubjectID")]
//        public virtual Subject Subject { get; set; }
//    }
//}


using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSystemAPI.Models
{
    public class ClassSchedule
    {
        [Key]
        public int ScheduleID { get; set; }

        public int CourseID { get; set; }

        // New FK column
        public int? SubjectID { get; set; }   // nullable because the DB column is NULL

        public byte DayOfWeek { get; set; } // 1 = Sunday, 2 = Monday...

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        [StringLength(20)]
        public string RoomNumber { get; set; }

        public bool IsRecurring { get; set; } = true;

        // Navigation: Course
        [ForeignKey("CourseID")]
        public virtual Course Course { get; set; }

        // Navigation: Subject (NEW)
        [ForeignKey("SubjectID")]
        public virtual Subject Subject { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? ClassDate { get; set; }

    }
}
