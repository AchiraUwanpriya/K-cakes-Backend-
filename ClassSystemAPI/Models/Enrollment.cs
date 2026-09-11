using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSystemAPI.Models
{
    public class Enrollment
    {
        [Key]
        public int EnrollmentID { get; set; }

        public int CourseID { get; set; }

        public int StudentID { get; set; }
        public int SubjectID { get; set; }


        public DateTime EnrollmentDate { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("CourseID")]
        public virtual Course Course { get; set; }

        [ForeignKey("StudentID")]
        public virtual Student Student { get; set; }

        [ForeignKey("SubjectID")]
        public virtual Subject Subject { get; set; }
    }
}
