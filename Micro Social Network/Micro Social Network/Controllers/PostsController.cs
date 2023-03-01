using Micro_Social_Network.Data;
using Micro_Social_Network.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Micro_Social_Network.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PostsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // toti utilizatorii pot vedea postarile din aplicatie
        // (vezi ca am gandit ca nu exista grupuri cand ca facut constroller-ele,
        // deci dc ceva ce am scris nu se potriveste cu logica legata de grupuri,
        // poti sa modifici)

        // de exemplu, cred aici ca ar tb sa modifici sa afisezi doar posturile fara grup
        public IActionResult Index()
        {
            var posts = db.Posts
                .Include("User")
                .Where(p => p.GroupId == null);

            ViewBag.Posts = posts;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.msg = TempData["message"];
            }
            return View();
        }

        // toti utilizatorii pot vedea o postare anume
        // modificat
        public IActionResult Show(int id)
        {
            Post post = db.Posts.Include("Group")
                                 .Include("User")
                                .Include("Comments")
                                .Include("Comments.User")
                                .Where(post => post.Id == id)
                                .FirstOrDefault();

            SetAccess();
            ViewBag.Group = post.Group;
            ViewBag.Message = TempData["message"];
            return View(post);
        }


        // doar utilizatorii inregistrati pot posta postari
        [Authorize]
        public IActionResult New()
        {
            //adaugam grupurile utilizatorului(in cazul de fata toti sunt admini)
            //pentru dropdown
            ViewBag.Groups = GetAllGroups();

            return View();
        }

        // Se adauga postarea in baza de date
        // Doar utilizatorii inregistrati pot posta postari
        // Utilizatorii pot posta doar in feed-ul personal sau grupurile in care apartin
        [HttpPost]
        [Authorize]

        public IActionResult New(Post post)
        {
            post.Date = DateTime.Now;

            if (ModelState.IsValid)
            {

                // retinem Id-ul user-ului care face adaugarea 
                post.UserId = _userManager.GetUserId(User);
                //verificam daca modelul contine vreun group si daca user-ul apartine grupului respectiv
                if (post.GroupId != null)
                {
                    var userId = _userManager.GetUserId(User);

                    // var user = db.ApplicationUsers.First(u => u.Id == user.Id);
                    bool userInGroup = db.Groups
                                        .Include("Users")
                                        .First(g => g.Id == post.GroupId)
                                        .Users
                                        .Where(u => u.Id == userId)
                                        .Any();

                    // si adminul poate adauga in grup postari
                    if (userInGroup == false && User.IsInRole("Admin")==false)
                    {
                        TempData["message"] = "Nu puteti adauga un post intr-un grup in care nu sunteti inclus";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        db.Posts.Add(post);
                        db.SaveChanges();
                        return RedirectToAction("Show", "Groups", new { id = post.GroupId });

                    }

                }



                db.Posts.Add(post);
                db.SaveChanges();
                TempData["message"] = "Post creat cu succes";

                return RedirectToAction("Index");
            }

            else
            {
                ViewBag.Groups = GetAllGroups();
                return View(post);
            }
        }

        // utilizatorii isi pot edita DOAR propria postare
        // adminul poate edita orice postare
        // utilizatorii neinregistrati nu pot edita nicio postare

        [Authorize]
        public IActionResult Edit(int id)
        {
            Post post = db.Posts.Where(post => post.Id == id)
                                .First();

            if (User.IsInRole("Admin") || _userManager.GetUserId(User) == post.UserId)
            {
                return View(post);
            }
            else
            {
                TempData["message"] = "Nu aveti voie sa editati o postare care nu va apartine!";
                return RedirectToAction("Index");
            }
        }

        // Se adauga articolul modificat in baza de date
        // utilizatorii isi pot edita DOAR propria postare
        // adminul poate edita orice postare
        // utilizatorii neinregistrati nu pot edita nicio postare
        [Authorize]
        [HttpPost]
        public IActionResult Edit(int id, Post requestPost)
        {
            Post post = db.Posts.Find(id);

            requestPost.Date = DateTime.Now;

            if (ModelState.IsValid)
            {
                if (User.IsInRole("Admin") || _userManager.GetUserId(User) == post.UserId)
                {
                    post.Content = requestPost.Content;
                    post.Date = requestPost.Date;
                    db.SaveChanges();
                    TempData["message"] = "Post editat cu succes";
                }
                else
                {
                    TempData["message"] = "Nu aveti voie sa editati o postare care nu va apartine!";
                }
                // modificat !!!
                if (post.GroupId == null)
                    return RedirectToAction("Index");
                else
                {
                    // daca postarea face parte dintr-un grup se revine la pagina grupului
                    return RedirectToAction("Show", "Groups", new { id = post.GroupId });
                }
            }
            else
            {
                return View(requestPost);
            }
        }

        // Se sterge un articol din baza de date         
        // utilizatorii isi pot sterge DOAR propria postare
        // adminul poate sterge orice postare
        // utilizatorii neinregistrati nu pot sterge nicio postare

        // + are o mica prb (utilizator care se logeaza cand ii se afiseaza by default formul de login => nu merge sa faca delete)
        [HttpPost]
        [Authorize]
        public ActionResult Delete(int id)
        {
            Post post = db.Posts.Include("Comments")
                                .Where(p => p.Id == id)
                                .First();
            if (User.IsInRole("Admin") || _userManager.GetUserId(User) == post.UserId)
            {
                db.Posts.Remove(post);
                db.SaveChanges();
                TempData["message"] = "Post sters cu succes";
            }
            else
            {
                TempData["message"] = "Nu aveti voie sa stergeti o postare care nu va apartine!";
            }
            return RedirectToAction("Index");
        }

        // Adaugarea unui comentariu asociat unui articol in baza de date
        // Numai utilizatorii inregistrati pot posta comentarii
        [HttpPost]
        [Authorize]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date = DateTime.Now;
            comment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Posts/Show/" + comment.PostId);

            }
            else
            {
                Post post = db.Posts.Include("User")
                                .Include("Comments")
                                .Include("Comments.User")
                                .Where(post => post.Id == comment.PostId)
                                .First();

                ViewBag.Message = TempData["message"];

                SetAccess();

                return View(post);
            }
        }

        [NonAction]
        private void SetAccess()
        {
            ViewBag.UserId = _userManager.GetUserId(User);
            ViewBag.isLoggedIn = _signInManager.IsSignedIn(User);
            ViewBag.isAdmin = User.IsInRole("Admin");
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllGroups()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var groups = from cat in db.Groups
                         select cat;

            // iteram prin categorii
            foreach (var group in groups)
            {
                // adaugam in lista elementele necesare pentru dropdown
                // id-ul categoriei si denumirea acesteia
                selectList.Add(new SelectListItem
                {
                    Value = group.Id.ToString(),
                    Text = group.Name.ToString()
                });
            }

            selectList.Add(new SelectListItem
            {
                Value = null,
                Text = "Niciunul"
            });



            // returnam lista de grupuri
            return selectList;
        }
    }
}
