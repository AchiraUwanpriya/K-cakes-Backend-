using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSystemAPI.Models
{
    public class Teacher
    {
        [Key]
        [ForeignKey("User")]
        public int TeacherID { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeID { get; set; }

        [MaxLength(100)]
        public string Department { get; set; }

        [MaxLength(100)]
        public string Qualification { get; set; }

        public DateTime? JoiningDate { get; set; }

        public string Bio { get; set; }

        public virtual User User { get; set; }
    }
}