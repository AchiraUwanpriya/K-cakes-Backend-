using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSystemAPI.Models
{
   
    public class PaymentHistory
    {
        [Key]
        public int PaymentHistoryID { get; set; }

        public int PaymentID { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public decimal AmountPaid { get; set; }

        public string PaymentMethod { get; set; }
        public string ReferenceNo { get; set; }
        public string Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
    }
}
