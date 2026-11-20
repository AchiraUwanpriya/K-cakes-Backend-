using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models.DTOs
{
    public class VisitorRegisterDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
    }
}