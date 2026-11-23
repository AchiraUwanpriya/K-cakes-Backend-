using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models
{
    [Table("ResourceRequest")]
    public class ResourceRequest
    {
        [Key]
        public int ResourceRequestID { get; set; }
        public string BuyerType { get; set; }
        public int BuyerID { get; set; }
        public string BuyerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public decimal TotalAmount { get; set; }
        public string SlipPath { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? DecidedDate { get; set; }
    }
}