using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models
{
    [Table("CartResources")]
    public class CartResource
    {
        [Key]
        public int ResourceID { get; set; }
        public string Title { get; set; }
        public string Tag { get; set; }
        public string Description { get; set; }
        public int LessonCount { get; set; }
        public string Duration { get; set; }
        public decimal Price { get; set; }
        public bool IncludeForStudents { get; set; }
        public string ThumbnailURL { get; set; }
        public string ResourceURL { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}