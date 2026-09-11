using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models.DTOs
{
    public class PaymentHistoryDto
    {
        public int PaymentID { get; set; }
        public decimal AmountPaid { get; set; }

        public string PaymentMethod { get; set; }
        public string ReferenceNo { get; set; }
        public string Remarks { get; set; }

        public int? CreatedBy { get; set; }
    }

}