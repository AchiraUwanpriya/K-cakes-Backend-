using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSystemAPI.Models
{
    public class Announcement
    {
        [Key]
        public int AnnouncementID { get; set; }

        public int CourseID { get; set; }

        public int TeacherID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime PostDate { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; }

        public bool IsImportant { get; set; } = false;

        [ForeignKey("CourseID")]
        public virtual Course Course { get; set; }

        [ForeignKey("TeacherID")]
        public virtual Teacher Teacher { get; set; }
    }
}
