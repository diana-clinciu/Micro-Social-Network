using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micro_Social_Network.Models
{
    public class FriendRequest
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { set; get; }
        public string? UserSendId { set; get; }
        public string? UserReceiveId { set; get; }
        virtual public ApplicationUser? UserSend { set; get; }
        virtual public ApplicationUser? UserReceive { set; get; }
        public DateTimeOffset ExpirationDate { set; get; }
        public string Status { set; get; }


    }
}
