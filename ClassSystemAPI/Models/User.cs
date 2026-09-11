using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSystemAPI.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; }

        
        public string PasswordHash { get; set; }

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        public int UserTypeID { get; set; }

        [ForeignKey("UserTypeID")]
        public virtual UserType UserType { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }

        [MaxLength(255)]
        public string ProfilePicture { get; set; }
        public string HED_MOBILE_NO { get; set; }
    }

}
