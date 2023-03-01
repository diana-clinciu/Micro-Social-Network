using Micro_Social_Network.Data;
using Micro_Social_Network.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Micro_Social_Network.Controllers
{
    public class FriendRequestsController : Controller
    {
        private readonly double maxPendingTime = 3; // days
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public FriendRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize]
        [HttpPost]
        public IActionResult New(FriendRequest friendRequest)
        {
            DeleteExpiredFriendRequest();
            friendRequest.ExpirationDate = DateTimeOffset.Now.AddDays(maxPendingTime);
            friendRequest.Status = "Pending";


            // un user nu poate fi prieten cu sine

            if (friendRequest.UserSendId == friendRequest.UserReceiveId)
            {
                TempData["message"] = "Nu va puteti trimite cerere de prietenie.";

                return RedirectToAction("ListUsers", "Users");
            }

            // un user nu poate trimite o cerere de prietenie daca:
            // e deja prieten 
            // a trimis/primit deja o cerere care este inca in asteptare si neexpirata

            bool isFriend = AreFriends(friendRequest.UserReceiveId, friendRequest.UserSendId);

            bool stillPending = db.FriendRequests.Any(f => f.Status == "Pending" &&
                                                           f.ExpirationDate > DateTimeOffset.Now &&
                                                          (f.UserReceiveId == friendRequest.UserReceiveId &&
                                                           f.UserSendId == friendRequest.UserSendId ||
                                                          f.UserReceiveId == friendRequest.UserSendId &&
                                                           f.UserSendId == friendRequest.UserReceiveId));

            if (isFriend)
            {
                TempData["message"] = "Sunteti deja prieten cu acest utilizator.";

                return RedirectToAction("ListUsers", "Users");
            }

            if (stillPending)
            {
                TempData["message"] = "Exista deja cererea de prietenie.";

                return RedirectToAction("ListUsers", "Users");
            }

            db.FriendRequests.Add(friendRequest);
            db.SaveChanges();

            TempData["message"] = "Cerere de prietenie trimisa.";

            return RedirectToAction("ListUsers", "Users");
        }




        [Authorize]
        [HttpPost]
        public IActionResult ProcessFriendRequest(string senderId, string recieverId, string response)
        {


            FriendRequest friendRequest =
                  db.FriendRequests
                .Include("UserSend")
                .Include("UserReceive")
                .Where(freq => freq.Status.Equals("Pending"))
                 .Where(freq => freq.UserSendId.Equals(senderId))
                .Where(freq => freq.UserReceiveId.Equals(recieverId))
                .First();

            friendRequest.Status = response;
            db.SaveChanges();


            return RedirectToAction("ShowFriends", "Users", new { id = recieverId});
        }

        [NonAction]
        public void DeleteExpiredFriendRequest()
        {
            var expiredFriendRequests = db.FriendRequests.Where(req => req.ExpirationDate.CompareTo(DateTime.Now) < 0
            && req.Status.Equals("Pending"));


            db.FriendRequests.RemoveRange(expiredFriendRequests);

            db.SaveChanges();


        }



        [NonAction]
        private bool AreFriends(string user1, string user2)
        {
            return db.FriendRequests.Any(f => f.Status == "Accepted" &&
                                                 (f.UserReceiveId == user1 &&
                                                  f.UserSendId == user2 ||
                                                 f.UserReceiveId == user2 &&
                                                  f.UserSendId == user1));
        }

    }
}
