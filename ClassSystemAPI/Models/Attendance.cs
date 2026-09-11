//using System;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Data.Entity.Spatial;

//namespace ClassSystemAPI.Models
//{
//    public class Attendance
//    {
//        [Key]
//        public int AttendanceID { get; set; }

//        [Required]
//        public int SessionID { get; set; }

//        [Required]
//        public int StudentID { get; set; }

//        [Required]
//        public DateTime ScanTime { get; set; }

//        [MaxLength(255)]
//        public string DeviceInfo { get; set; }

//        [MaxLength(50)]
//        public string IPAddress { get; set; }

//        public DbGeography Location { get; set; } // For geolocation verification

//        [MaxLength(20)]
//        public string Status { get; set; } = "Present"; // Present, Late, Absent, Excused

//        [ForeignKey("StudentID")]
//        public virtual Student Student { get; set; }
//    }
//}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;

namespace ClassSystemAPI.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceID { get; set; }

        [Required]
        public int SessionID { get; set; }

        [Required]
        public int StudentID { get; set; }

        [Required]
        public DateTime ScanTime { get; set; }

        [MaxLength(255)]
        public string DeviceInfo { get; set; }

        [MaxLength(50)]
        public string IPAddress { get; set; }

        public DbGeography Location { get; set; } // For geolocation verification

        [MaxLength(20)]
        public string Status { get; set; } = "Present"; // Present, Late, Absent, Excused

        public bool IsActive { get; set; } = true;

        // Navigation properties
        //[ForeignKey("SessionID")]
        //public virtual QRSession QRSession { get; set; }

        [ForeignKey("StudentID")]
        public virtual Student Student { get; set; }
    }
}