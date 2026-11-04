using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace ClassSystemAPI.Models
{
    public class Subject
    {
        [Key]
        public int SubjectID { get; set; }

        [Required]
        public string SubjectName { get; set; }

        public string SubjectCode { get; set; }

        public string Description { get; set; }
        public decimal? TotalFee { get; set; }

        public int? Duration_In_Months { get; set; }

        public decimal? MonthlyFee { get; set; }

        public int? PaymentDueDay { get; set; }
        public bool IsActive { get; set; }


        // Navigation for many-to-many
        public virtual ICollection<CourseSubjects> CourseSubjects { get; set; }
    }
}