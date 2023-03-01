using Micro_Social_Network.Data;
using Micro_Social_Network.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Micro_Social_Network.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly SignInManager<ApplicationUser> _signInManager;
        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;

            _signInManager = signInManager;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            var users = from user in db.Users
                        orderby user.UserName
                        select user;

            ViewBag.UsersList = users;

            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Show(string id)
        {
            ApplicationUser user = db.Users.Find(id);
            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.Roles = roles;
            ViewBag.CurrentUserId = _userManager.GetUserId(User);

            return View(user);
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            user.AllRoles = GetAllRoles();

            var roleNames = await _userManager.GetRolesAsync(user); // Lista de nume de roluri

            // Cautam ID-ul rolului in baza de date
            var currentUserRole = _roleManager.Roles
                                              .Where(r => roleNames.Contains(r.Name))
                                              .Select(r => r.Id)
                                              .First(); // Selectam 1 singur rol
            ViewBag.UserRole = currentUserRole;

            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> Edit(string id, ApplicationUser newData, [FromForm] string newRole)
        {
            ApplicationUser user = db.Users.Find(id);

            if (ModelState.IsValid)
            {
                user.Visible = newData.Visible;
                user.UserName = newData.UserName;
                user.Email = newData.Email;
                user.PhoneNumber = newData.PhoneNumber;
                user.FirstName = newData.FirstName;
                user.LastName = newData.LastName;
                user.BirthDate = newData.BirthDate;
                user.Gender = newData.Gender;
                user.About = newData.About;

                // Cautam toate rolurile din baza de date
                var roles = db.Roles.ToList();

                foreach (var role in roles)
                {
                    // Scoatem userul din rolurile anterioare
                    await _userManager.RemoveFromRoleAsync(user, role.Name);
                }
                // Adaugam noul rol selectat
                var roleName = await _roleManager.FindByIdAsync(newRole);
                await _userManager.AddToRoleAsync(user, roleName.ToString());

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                newData.AllRoles = GetAllRoles();
                ViewBag.UserRole = newRole;

                return View(newData);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Delete(string id)
        {
            // adminul nu are voie sa-si stearga contul
            if (_userManager.GetUserId(User) == id)
            {
                TempData["message"] = "Nu aveti voie sa va stergeti propriul cont!";
                return RedirectToAction("Index", "Posts");
            }
            else
            {
                var user = db.Users
                         .Include("Posts")
                         .Include("Comments")
                         .Include("FriendRequestsSent")
                         .Include("FriendRequestsReceived")
                         .Where(u => u.Id == id)
                         .First();

                // Delete user users
                if (user.Posts.Count > 0)
                {
                    foreach (var article in user.Posts)
                    {
                        db.Posts.Remove(article);
                    }
                }
                // Delete user comments
                if (user.Comments.Count > 0)
                {
                    foreach (var comment in user.Comments)
                    {
                        db.Comments.Remove(comment);
                    }
                }

                // Delete user friendreq
                if (user.FriendRequestsReceived.Count > 0)
                {
                    foreach (var comment in user.FriendRequestsReceived)
                    {
                        db.FriendRequests.Remove(comment);
                    }
                }

                if (user.FriendRequestsSent.Count > 0)
                {
                    foreach (var comment in user.FriendRequestsSent)
                    {
                        db.FriendRequests.Remove(comment);
                    }
                }

                db.ApplicationUsers.Remove(user);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

        // afisare profil
        // adminul poate vedea orice profil
        // utilizatorul isi poate vizualiza propriul profil
        // orice persoana poate vedea um profil daca este public
        // *** un profil privat poate fi vazut de prieteni
        public IActionResult ShowProfile(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            if (User.IsInRole("Admin") ||
                _userManager.GetUserId(User) == user.Id ||
                user.Visible ||
                AreFriends(_userManager.GetUserId(User), id))
            {
                // de verificat !!!!!
                var userPosts = db.Posts.Include("User")
                                        .Where(p=> p.GroupId == null &&
                                                p.UserId == id);

                ViewBag.userPosts = userPosts;

                SetAccess();
                return View(user);
            }
            else
            {
                TempData["message"] = "Nu aveti voie sa vizualizati acest profil!";
                return RedirectToAction("Index", "Posts");
            }
        }

        // adminul poate edita orice profil
        // un utilizator isi poate edita doar propriul profilul 
        [Authorize(Roles = "Admin,User")]
        public IActionResult EditProfile(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            if (User.IsInRole("Admin") || _userManager.GetUserId(User) == user.Id)
            {
                return View(user);
            }
            else
            {
                TempData["message"] = "Nu aveti voie sa editati un profil care nu va apartine!";
                return RedirectToAction("Index", "Posts");
            }
        }

        // adminul poate edita orice profil
        // un utilizator isi poate edita doar propriul profilul 
        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        public IActionResult EditProfile(string id, ApplicationUser newData)
        {
            ApplicationUser user = db.Users.Find(id);

            if (ModelState.IsValid)
            {
                if (User.IsInRole("Admin") || _userManager.GetUserId(User) == user.Id)
                {
                    user.Visible = newData.Visible;
                    user.UserName = newData.UserName;
                    user.Email = newData.Email;
                    user.PhoneNumber = newData.PhoneNumber;
                    user.FirstName = newData.FirstName;
                    user.LastName = newData.LastName;
                    user.BirthDate = newData.BirthDate;
                    user.Gender = newData.Gender;
                    user.About = newData.About;

                    db.SaveChanges();
                    return RedirectToAction("Index", "Posts");
                }
                else
                {
                    TempData["message"] = "Nu aveti voie sa editati un profil care nu va apartine!";
                    return RedirectToAction("Index", "Posts");
                }
            }
            else
            {
                return View(newData);
            }
        }

        public IActionResult ListUsers()
        {
            SetAccess();
            if (TempData.ContainsKey("message"))
            {
                ViewBag.msg = TempData["message"];
            }

            ViewBag.CurrentUserId = _userManager.GetUserId(User);

            var users = from user in db.Users
                        select user;

            string search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();

                users = from user in db.Users
                        where user.FirstName.ToLower() == search.ToLower() ||
                              user.LastName.ToLower() == search.ToLower() ||
                              user.UserName.Contains(search.ToLower())
                        select user;

                //Console.WriteLine("Search: " + search);
                //foreach (var u in users)
                //{
                //    Console.WriteLine(u.UserName);
                //}

            }

            // afisam butonul add friend doar daca nu sunt prieteni
            foreach (var user in users)
            {
                if (AreFriends(user.Id, _userManager.GetUserId(User)) || db.FriendRequests.Any(f => f.Status == "Pending" &&
                                                           f.ExpirationDate > DateTimeOffset.Now &&
                                                          (f.UserReceiveId == user.Id &&
                                                           f.UserSendId == _userManager.GetUserId(User) ||
                                                          f.UserReceiveId == _userManager.GetUserId(User) &&
                                                           f.UserSendId == user.Id)))
                    user.noFriendRequest = true;
                else
                    user.noFriendRequest = false;
            }

            ViewBag.SearchString = search;

            // AFISARE PAGINATA
            int _perPage = 6;

            int totalItems = users.Count();

            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /users/Index?page=valoare

            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);

            // Pentru prima pagina offsetul o sa fie zero
            // Pentru pagina 2 o sa fie 3
            // Asadar offsetul este egal cu numarul de articole care au fost deja afisate pe paginile anterioare

            var offset = 0;

            // Se calculeaza offsetul in functie de numarul paginii la care suntem
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            // Se preiau articolele corespunzatoare pentru fiecare pagina la care ne aflam
            // in functie de offset
            var paginatedUsers = users.Skip(offset).Take(_perPage);

            // Preluam numarul ultimei pagini
            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            // Trimitem articolele cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.users = paginatedUsers;

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/users/ListUsers/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/users/ListUsers/?page";
            }

            return View();
        }

        // afisare prieteni utilizator
        // aceleasi restrictii ca la vizualizare profil (sa fie vizibil sau admin sau userul 
        // de care apartine profilul)
        public IActionResult ShowFriends(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            if (User.IsInRole("Admin") || 
                _userManager.GetUserId(User) == user.Id || 
                user.Visible ||
                AreFriends(_userManager.GetUserId(User), id))
            {
                SetAccess();

                var friendIds1 = from f
                                 in db.FriendRequests
                                 where f.Status == "Accepted" &&
                                       f.UserSendId == user.Id
                                 select f.UserReceiveId;

                var friendIds2 = from f
                                 in db.FriendRequests
                                 where f.Status == "Accepted" &&
                                       f.UserReceiveId == user.Id
                                 select f.UserSendId;

                var friendIds = friendIds1.Union(friendIds2);

                var friends = db.Users.Where(u => friendIds.Contains(u.Id));

                ViewBag.friends = friends;

                return View(user);
            }
            else
            {
                TempData["message"] = "Nu aveti voie sa vizualizati acest profil!";
                return RedirectToAction("Index", "Posts");
            }
        }

        // afiseaza cererile de prietenie inca in pending si neexpirate pe care le-a
        // primit utilitatorul cu id-ul id
        public IActionResult ShowFriendRequests(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            if (_userManager.GetUserId(User) == user.Id)
            {
                SetAccess();

                var usersSend = db.FriendRequests.Include("UserSend")
                                                  .Where(f => f.Status == "Pending" &&
                                                              f.UserReceiveId == user.Id)
                                                  .Select(f => f.UserSend);

                ViewBag.usersSend = usersSend;
                ViewBag.currentUserId = _userManager.GetUserId(User);
                return View(user);
            }
            else
            {
                TempData["message"] = "Nu aveti voie sa vizualizati acest profil!";
                return RedirectToAction("Index", "Posts");
            }
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllRoles()
        {
            var selectList = new List<SelectListItem>();

            var roles = from role in db.Roles
                        select role;

            foreach (var role in roles)
            {
                selectList.Add(new SelectListItem
                {
                    Value = role.Id.ToString(),
                    Text = role.Name.ToString()
                });
            }
            return selectList;
        }

        [NonAction]
        private void SetAccess()
        {
            ViewBag.UserId = _userManager.GetUserId(User);
            ViewBag.isAdmin = User.IsInRole("Admin");
            ViewBag.isLoggedIn = _signInManager.IsSignedIn(User);
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
