using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models
{

    [Table("Visitors")]
    public class Visitor
    {
        [Key]
        public int  VisitorID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}