using System.ComponentModel.DataAnnotations;

namespace Micro_Social_Network.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Continutul postarii este obligatoriu")]
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public int? GroupId { get; set; }
        public virtual Group? Group { get; set; }
        public virtual ICollection<Comment>? Comments { get; set; }
    }
}
