using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSystemAPI.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        public int EnrollmentID { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; } = 0;

        // Computed column in SQL Server
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal BalanceAmount { get; set; }

        public string Status { get; set; } = "PENDING"; // PENDING / PARTIAL / PAID

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
