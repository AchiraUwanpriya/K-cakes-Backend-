using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models.DTOs
{
    public class PaymentCreateDto
    {
        public int EnrollmentID { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal FirstPaidAmount { get; set; } = 0;

        public string PaymentMethod { get; set; }
        public string ReferenceNo { get; set; }
        public string Remarks { get; set; }

        public int? CreatedBy { get; set; }
    }

}