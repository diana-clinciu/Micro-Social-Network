using Micro_Social_Network.Data;
using Micro_Social_Network.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Micro_Social_Network.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public CommentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Stergerea unui comentariu asociat unui articol din baza de date
        // Adminul poate sterge orice comentariu
        // Un utilizator isi poate sterge doar propriile comentarii
        // Utilizatorii neinregistrati nu pot sterge nicio postare
        [HttpPost]
        public IActionResult Delete(int id)
        {
            Comment comm = db.Comments.Find(id);
            if(User.IsInRole("Admin") || _userManager.GetUserId(User) == comm.UserId)
            {
                db.Comments.Remove(comm);
                db.SaveChanges();
                TempData["message"] = "Comentariul a fost sters cu succes";
                return Redirect("/Posts/Show/" + comm.PostId);
            }
            else
            {
                TempData["message"] = "Nu aveti voie sa stergeti un comentariu care nu va apartine!";
                return RedirectToAction("Index", "Posts");
            }
        }

        // In acest moment vom implementa editarea intr-o pagina View separata
        // Se editeaza un comentariu existent
        // Adminul poate edita orice comentariu
        // Un utilizator isi poate edita doar propriile comentarii
        // Utilizatorii neinregistrati nu pot edita nicio postare
        public IActionResult Edit(int id)
        {
            Comment comm = db.Comments.Find(id);

            if (User.IsInRole("Admin") || _userManager.GetUserId(User) == comm.UserId) 
            {
                return View(comm);
            }
            else
            {
                TempData["message"] = "Nu aveti voie sa editati un comentariu care nu va apartine!";
                return RedirectToAction("Index", "Posts");
            }
        }

        // Adminul poate edita orice comentariu
        // Un utilizator isi poate edita doar propriile comentarii
        // Utilizatorii neinregistrati nu pot edita nicio postare
        [HttpPost]
        public IActionResult Edit(int id, Comment requestComment)
        {
            Comment comm = db.Comments.Find(id);

            if(ModelState.IsValid)
            {
                if (User.IsInRole("Admin") || _userManager.GetUserId(User) == comm.UserId)
                {
                    comm.Content = requestComment.Content;

                    db.SaveChanges();
                    TempData["message"] = "Comentariul a fost editat cu succes";

                    return Redirect("/Posts/Show/" + comm.PostId);
                }
                else
                {
                    TempData["message"] = "Nu aveti voie sa editati un comentariu care nu va apartine!";
                    return RedirectToAction("Index", "Posts");
                }
            }
            else
            {
                return View(requestComment);
            }
        }
    }
}

