using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace ClassSystemAPI.Models
{
    [Table("ResRequestItem")]
    public class ResRequestItem
    {
        [Key]
        public int ResRequestItemID { get; set; }
        public int ResRequestID { get; set; }
        public int ResID { get; set; }
        public decimal Price { get; set; }

    }
}