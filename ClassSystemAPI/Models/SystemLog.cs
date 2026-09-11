using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSystemAPI.Models
{
    public class SystemLog
    {
        [Key]
        public int LogID { get; set; }

        public int? UserID { get; set; }   

        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; }

        public string Details { get; set; }

        [MaxLength(50)]
        public string IPAddress { get; set; }

        public DateTime LogTime { get; set; } = DateTime.Now;
    }
}