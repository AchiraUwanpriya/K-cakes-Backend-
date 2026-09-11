using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClassSystemAPI.Models
{
    public class UserType
    {
        [Key]
        public int UserTypeID { get; set; }

        [Required]
        [MaxLength(50)]
        public string TypeName { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}
