using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models.DTOs
{
    public class SubjectCreateDto
    {
        [Required]
        public string SubjectName { get; set; }

        public string SubjectCode { get; set; }

        public string Description { get; set; }

        public decimal? TotalFee { get; set; }

        public int? Duration_In_Months { get; set; }

        public decimal? MonthlyFee { get; set; }

        // Course(s) to link this subject to at creation time
        public List<int> CourseIDs { get; set; }
    }
}