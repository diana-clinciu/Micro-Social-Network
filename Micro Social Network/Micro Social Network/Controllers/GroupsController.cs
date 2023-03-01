using Micro_Social_Network.Data;
using Micro_Social_Network.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Net;

namespace Micro_Social_Network.Controllers
{
    public class GroupsController : Controller
    {

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public GroupsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }
        public async Task<IActionResult> Index()
        {

            SetAccess();
            var groups = db.Groups
                         .Include("Users")
                         ;

            ViewBag.Groups = groups;
            ViewBag.curUser = await _userManager.GetUserAsync(User);
            if (TempData.ContainsKey("message"))
            {
                ViewBag.msg = TempData["message"];
            }

            return View();
        }
        // doar userii care apartin grupului si *adminul* pot vedea ce e in grup
        public IActionResult Show(int id)
        {
            SetAccess();

            Group group = db.Groups
                .Include("Users")
                .Include("Posts")
                .Include("Posts.Comments")
                .Include("Posts.User")
                .Where(group => group.Id == id)
                .First();

            var userId = _userManager.GetUserId(User);

            // var user = db.ApplicationUsers.First(u => u.Id == user.Id);
            bool userInGroup = group.Users.Any(u => u.Id == userId);
            ViewBag.Group = group;
            if (userInGroup || User.IsInRole("Admin"))
            {
                return View(group);
            }
            else
            {
                TempData["message"] = "Nu aveti acces la informatiile acestui grup";
                return RedirectToAction("Index");
            }

        }

        // oricine utilizator logat poate adauga un grup
        [Authorize]
        public IActionResult New()
        {
            return View();
        }

        // oricine utilizator logat poate adauga un grup
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> New(Group gr)
        {
            SetAccess();
            if (ModelState.IsValid)
            {
                var curUser = await _userManager.GetUserAsync(User);


                db.Groups.Add(gr);

                db.SaveChanges();

                var group = db.Groups
                    .Include("Users")
                    .First(g => g.Id == gr.Id);
                group.Users.Add(curUser);
                db.SaveChanges();
                TempData["message"] = "Grup creat cu succes";
                return RedirectToAction("Index");
            }
            else
            {
                return View(gr);
            }
        }

        // numai adminul poate edita grupurile
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {

            Group group = db.Groups.Where(group => group.Id == id)
                                .First();

            ViewBag.Group = group;

            return View(group);
        }

        //se adauga grupul modificat in baza de date
        //DOAR daca utilizatorul are rol de admin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id, Group requestGroup)
        {
            SetAccess();
            Group group = db.Groups.Find(id);



            if (ModelState.IsValid)
            {


                if (User.IsInRole("Admin"))
                {

                    group.Name = requestGroup.Name;
                    db.SaveChanges();

                    TempData["message"] = "Grup editat cu succes";

                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveti voie sa editati grupul. Acest privilegiu i se atrribuie sfantului Admin!";
                    return RedirectToAction("Index");
                }
            }

            else
            {
                return View(group);
            }
        }

        /* Se sterge un grup din baza de date
          doar daca are rol de admin
        */
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            SetAccess();

            Group group = db.Groups
            .Include("Posts")
            .Include("Posts.Comments")
            .First(g => g.Id == id);
            db.Groups.Remove(group);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // poate trebuie sa adaugam o restrictie..
        /*  public ActionResult ShowMembers1(int id)
          {
              SetAccess();
              var group = db.Groups
                            .Include("Users")
                            .Include("Posts")
                            .Include("Posts.Comments")
                            .Include("Posts.User")
                            .FirstOrDefault(g => g.Id == id);
              return View(group);
          }
        */

        [HttpPost]
        ///[Authorize(Roles = "Admin")]
        public IActionResult Join(int id)
        {
            if (_signInManager.IsSignedIn(User) == false)
            {
                TempData["message"] = "Cum ne spalam pe maini inainte de masa, tot asa ne logam inainte sa ne alaturam unui grup:)";
                return RedirectToAction("Index");
            }


            SetAccess();
            Group group = db.Groups
                        .Include("Users")
                        .First(g => g.Id == id);

            var userId = _userManager.GetUserId(User);
            //var user = await GetCurrentUser();
            var user = db.Users
                .Include("Groups")
                .First(u => u.Id == userId);

            if (!group.Users.Contains(user))
            {

                group.Users.Add(user);

                user.Groups.Add(group);

                db.Users.Update(user);
                db.Groups.Update(group);


                // db.Groups.Update(group);

                db.SaveChanges();
                TempData["message"] = user.UserName + " s-a alaturat grupului " + group.Name;
            }
            else
            {
                TempData["message"] = user.UserName + " deja apartine grupului " + group.Name;

            }//throw new Exception("it worked until here");

            return RedirectToAction("Index");

        }

        // numai userii din grp pot vedea membrii
        public IActionResult ShowMembers(int id)
        {
            var group = db.Groups
                          .Include("Users")
                          .Include("Posts")
                          .Include("Posts.Comments")
                          .Include("Posts.User")
                          .FirstOrDefault(g => g.Id == id);

            var userId = _userManager.GetUserId(User);

            bool userInGroup = db.Groups
                                .Include("Users")
                                .First(g => g.Id == id)
                                .Users
                                .Where(u => u.Id == userId)
                                .Any();

            if (userInGroup == false)
            {
                TempData["message"] = "Nu puteti vedea membrii unui grup in care nu apartineti!";
                return RedirectToAction("Index");
            }
            else
            {
                // int groupId = 3;
                if (TempData.ContainsKey("message"))
                {
                    ViewBag.msg = TempData["message"];
                }

                ViewBag.CurrentUserId = _userManager.GetUserId(User);

                var gr = db.Groups.
                        Include("Users")
                        .First(g => g.Id == id);
                var users = from user in gr.Users
                            select user;

                string search = "";

                if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
                {
                    search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();




                    users = from user in gr.Users
                            where (user.FirstName != null && user.FirstName.ToLower() == search.ToLower()) ||
                                  (user.LastName != null && user.LastName.ToLower() == search.ToLower()) ||
                                  user.UserName.Contains(search.ToLower())
                            select user;

                    //Console.WriteLine("Search: " + search);
                    //foreach (var u in users)
                    //{
                    //    Console.WriteLine(u.UserName);
                    //}

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

                return View(group);
            }


        }

        [NonAction]
        private void SetAccess()
        {
            ViewBag.UserId = _userManager.GetUserId(User);
            ViewBag.isLoggedIn = _signInManager.IsSignedIn(User);
            ViewBag.isAdmin = User.IsInRole("Admin");
        }

    }
}

