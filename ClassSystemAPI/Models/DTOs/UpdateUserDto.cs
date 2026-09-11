using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models.DTOs
{
    public class UpdateUserDto
    {
        public int UserID { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // optional
        public string PasswordHash { get; set; }
        public string ProfilePicture { get; set; }

        public bool RemoveProfilePicture { get; set; }

        public string HED_MOBILE_NO { get; set; }
    }
}