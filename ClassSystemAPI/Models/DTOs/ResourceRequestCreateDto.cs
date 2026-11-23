using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models.DTOs
{
    public class ResourceRequestCreateDto
    {
        public string BuyerType { get; set; }   
        public int BuyerID { get; set; }        
        public List<int> ResourceIDs { get; set; }
    }
}