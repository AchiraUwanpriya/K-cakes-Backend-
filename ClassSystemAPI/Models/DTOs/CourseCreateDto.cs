using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClassSystemAPI.Models.DTOs
{
    public class CourseCreateDto
    {
        [Required]
        [StringLength(100)]
        public string CourseName { get; set; }

        [StringLength(20)]
        public string CourseCode { get; set; }

        public int TeacherID { get; set; }

        [StringLength(20)]
        public string AcademicYear { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        // Provide subject ids to link to this course
        public List<int> SubjectIDs { get; set; }
    }

    public class CourseUpdateDto : CourseCreateDto
    {
        [Required]
        public int CourseID { get; set; }
    }

    public class CourseDetailsDto
    {
        public int CourseID { get; set; }
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public int TeacherID { get; set; }
        public string AcademicYear { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public List<SubjectDto> Subjects { get; set; }
        public TeacherDto Teacher { get; set; }
    }

    public class SubjectDto
    {
        public int SubjectID { get; set; }
        public string SubjectName { get; set; }
        public string SubjectCode { get; set; }
    }

    public class TeacherDto
    {
        public int TeacherID { get; set; }
        public UserDto User { get; set; } // uses UserDto defined in StudentDto.cs
    }
}