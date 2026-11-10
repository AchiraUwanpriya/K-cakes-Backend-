using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models
{
    [Table("WebsiteCourses")]
    public class WebsiteCourse
    {
        [Key]
        public int WebsiteCourseID { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; }

        [StringLength(50)]
        public string Level { get; set; }

        public string Description { get; set; }

        [StringLength(100)]
        public string Duration { get; set; }

        [StringLength(200)]
        public string Schedule { get; set; }

        [StringLength(150)]
        public string TeacherName { get; set; }

        public decimal? WeeklyFee { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}