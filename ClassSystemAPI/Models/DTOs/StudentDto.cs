using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

//namespace ClassSystemAPI.Models.DTOs
//{
//    public class StudentDto
//    {
//        public int StudentID { get; set; }
//        public string RollNumber { get; set; }
//        public DateTime? EnrollmentDate { get; set; }
//        public string CurrentGrade { get; set; }
//        public string ParentName { get; set; }
//        public string ParentContact { get; set; }

//        // Optional: include basic user reference if needed
//        public int UserID { get; set; }
//        public string Username { get; set; }
//    }

//}



namespace ClassSystemAPI.Models.DTOs
{
    public class StudentCreateDto
    {
        public int UserID { get; set; }  // This should match an existing user ID
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public string CurrentGrade { get; set; }
        public string ParentName { get; set; }
        public string ParentContact { get; set; }
    }

    public class StudentUpdateDto
    {
        public int StudentID { get; set; }
        public string RollNumber { get; set; }
        public string CurrentGrade { get; set; }
        public string ParentName { get; set; }
        public string ParentContact { get; set; }
    }

    public class StudentDetailsDto
    {
        public int StudentID { get; set; }
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public string CurrentGrade { get; set; }
        public string ParentName { get; set; }
        public string ParentContact { get; set; }
        public UserDto UserDetails { get; set; }
    }

    public class UserDto
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string HED_MOBILE_NO { get; set; }
        public bool IsActive { get; set; }
    }
}