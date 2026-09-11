//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Data.Entity.Spatial;

//namespace ClassSystemAPI.Models
//{
//    public class QRSession
//    {
//        [Key]
//        public int SessionID { get; set; }

//        [Required]
//        public int CourseID { get; set; }

//        [Required]
//        public int TeacherID { get; set; }

//        [Required]
//        [MaxLength(255)]
//        public string QRCodeData { get; set; } = GenerateDefaultQRCodeData();

//        private static string GenerateDefaultQRCodeData()
//        {
//            return $"CLASS-{DateTime.Now.Ticks}-{Guid.NewGuid().ToString().Substring(0, 8)}";
//        }

//        public byte[] QRCodeImage { get; set; }

//        [Required]
//        public DateTime SessionDate { get; set; }

//        [Required]
//        public DateTime StartTime { get; set; }

//        [Required]
//        public DateTime EndTime { get; set; }

//        [Required]
//        public DateTime ExpiryTime { get; set; }

//        public bool IsActive { get; set; } = true;

//        public DbGeography Location { get; set; } // For geolocation tracking

//        public DateTime CreatedAt { get; set; } = DateTime.Now;

//        // Navigation properties
//        [ForeignKey("CourseID")]
//        public virtual Course Course { get; set; }

//        [ForeignKey("TeacherID")]
//        public virtual Teacher Teacher { get; set; }

//        public virtual ICollection<Attendance> Attendances { get; set; }
//    }
//}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;

namespace ClassSystemAPI.Models
{
    public class QRSession
    {
        [Key]
        public int SessionID { get; set; }

        [Required]
        public int CourseID { get; set; }

        [Required]
        public int TeacherID { get; set; }

        [Required]
        [MaxLength(255)]
        public string QRCodeData { get; set; } = GenerateDefaultQRCodeData();

        private static string GenerateDefaultQRCodeData()
        {
            return $"CLASS-{DateTime.Now.Ticks}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        public byte[] QRCodeImage { get; set; }

        [Required]
        public DateTime SessionDate { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        public DateTime ExpiryTime { get; set; }

        public bool IsActive { get; set; } = true;
        //  public bool Status { get; set; } = true;

        public DbGeography Location { get; set; } // For geolocation tracking

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CourseID")]
        public virtual Course Course { get; set; }

        [ForeignKey("TeacherID")]
        public virtual Teacher Teacher { get; set; }

        public virtual ICollection<Attendance> Attendances { get; set; }
    }
}