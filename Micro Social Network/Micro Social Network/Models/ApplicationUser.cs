using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micro_Social_Network.Models
{
    public class ApplicationUser : IdentityUser
    {
        // daca utilizatorul nu-si seteaza vizibilitatea profulului
        // consideram ca e vizibil by default
        public ApplicationUser()
        {
            Visible = true;
        }
        public bool Visible { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }

        // am adaugat si descriere - Diana
        public string? About { get; set; }
        public virtual ICollection<Comment>? Comments { get; set; }
        public virtual ICollection<Post>? Posts { get; set; }
        public virtual ICollection<Group>? Groups { get; set; }
        public virtual ICollection<FriendRequest>? FriendRequestsSent { get; set; }
        public virtual ICollection<FriendRequest>? FriendRequestsReceived { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }

        [NotMapped]
        public bool? noFriendRequest { get; set; }
    }
}
