using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ClassSystemAPI.Models
{
    public class Course
    {
        [Key]
        public int CourseID { get; set; }

        [Required, StringLength(100)]
        public string CourseName { get; set; }

        [StringLength(20)]
        public string CourseCode { get; set; }

        public int TeacherID { get; set; }

        [StringLength(20)]
        public string AcademicYear { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<CourseSubjects> CourseSubjects { get; set; }

        [ForeignKey("TeacherID")]
        public virtual Teacher Teacher { get; set; }
    }
}
