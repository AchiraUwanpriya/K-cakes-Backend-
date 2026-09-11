using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSystemAPI.Models
{
    public class Student
    {
        [Key]
        [ForeignKey("User")]
        public int StudentID { get; set; }

        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public string CurrentGrade { get; set; }
        public string ParentName { get; set; }
        public string ParentContact { get; set; }

        public virtual User User { get; set; }
        public object FullName { get; internal set; }
        public object Email { get; internal set; }
        //public DateTime EnrollDate { get; internal set; }
    }

}