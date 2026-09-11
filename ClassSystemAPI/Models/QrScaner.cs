using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;

namespace ClassSystemAPI.Models
{
    public class QrScanner
    {
        [Required]
        public int SessionID { get; set; }
        [Required]
        public int CourseID { get; set; }
        [Required]
        public int TeacherID { get; set; }
        [Required]
        public int SubjectID { get; set; }
        [Required]
        public int StudentID { get; set; }
        [Required]
        public int AttendanceID { get; set; }
        [Required]
        public int SessionDate { get; set; }

        [Required]
        public int ScanTime { get; set; }

    }
}
