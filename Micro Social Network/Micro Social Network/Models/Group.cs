using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micro_Social_Network.Models
{
    public class Group 
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele grupului este obligatoriu")]
        public string Name { get; set; }
  
        public virtual ICollection<ApplicationUser>? Users { get; set; }
        public virtual ICollection<Post>? Posts { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? groupSelectList { get; set; }
    }
}
