//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Linq;
//using System.Web;

//namespace ClassSystemAPI.Models
//{
//    public class CourseSubjects
//    {

//        [Key, Column(Order = 0)]
//        public int CourseID { get; set; }
//        [Key, Column(Order = 1)]
//        public int SubjectID { get; set; }

//        [ForeignKey(nameof(CourseID))]
//        public virtual Course Course { get; set; }

//        [ForeignKey(nameof(SubjectID))]
//        public virtual Subject Subject { get; set; }

//    }
//}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ClassSystemAPI.Models
{
    public class CourseSubjects
    {

        [Key, Column(Order = 0)]
        public int CourseID { get; set; }
        [Key, Column(Order = 1)]
        public int SubjectID { get; set; }

        [ForeignKey(nameof(CourseID))]
        public virtual Course Course { get; set; }

        [ForeignKey(nameof(SubjectID))]
        public virtual Subject Subject { get; set; }
        public bool IsActive { get; set; } = true;

    }
}