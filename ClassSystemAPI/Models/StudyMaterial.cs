using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSystemAPI.Models
{
    public class StudyMaterial
    {
        [Key]
        public int MaterialID { get; set; }

        [Required]
        public int CourseID { get; set; }

        [ForeignKey("CourseID")]
        public virtual Course Course { get; set; }

        [Required]
        public int TeacherID { get; set; }

        [ForeignKey("TeacherID")]
        public virtual Teacher Teacher { get; set; }

        [Required]
        public int SubjectID { get; set; }

        [ForeignKey("SubjectID")]
        public virtual Subject Subjects { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        [MaxLength(255)]
        public string FilePath { get; set; }

        [MaxLength(50)]
        public string FileType { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        public bool IsVisible { get; set; } = true;
    }
}