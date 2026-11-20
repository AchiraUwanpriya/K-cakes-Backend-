using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models.DTOs
{
    public class VideoRequestCreateDto
    {
        public string BuyerType { get; set; }
        public string BuyerID { get; set; }
        public List<int> VideoIDs { get; set; }
    }
}