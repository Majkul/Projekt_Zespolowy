using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;

namespace ProjektZespolowyGr3.Controllers.User
{
    public class UserProfileController : Controller
    {
        public MyDBContext _context;
        public UserProfileController(MyDBContext context)
        {
            _context = context;
        }

        // GET: UserProfileController
        public async Task<IActionResult> Index(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                //TODO send to homepage or " no user found :( "
            }
            else
            {
                ViewBag.model = await FindUserProfile(username);
            }

            return View();
        }

        private async Task<List<Models.DbModels.UserProfile>> FindUserProfile(string username)
        {

            var user = _context.UserProfiles.AsQueryable();

            user = user.Where(c=>
            c._User.Username == username);

            return await user.ToListAsync();
            

        }

        // GET: UserProfileController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: UserProfileController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserProfileController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserProfileController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UserProfileController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserProfileController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UserProfileController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
