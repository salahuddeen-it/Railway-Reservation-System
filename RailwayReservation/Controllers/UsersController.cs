using System;
using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;
using RailwayReservation.ViewModels;

namespace RailwayReservation.Controllers
{
    /// <summary>
    /// User master – only Admin can create/edit users.
    /// System assigns a random alphanumeric default password; user must change it at first login.
    /// </summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class UsersController : BaseController
    {
        // GET: /Users
        public ActionResult Index()
        {
            return View(Db.Users.OrderBy(u => u.UserType).ThenBy(u => u.UserName).ToList());
        }

        // GET: /Users/Create
        public ActionResult Create()
        {
            return View("Form", new UserFormViewModel { IsActive = true, UserType = UserType.Clerk });
        }

        // POST: /Users/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(UserFormViewModel model)
        {
            string userName = (model.UserName ?? string.Empty).Trim();
            if (Db.Users.Any(u => u.UserName == userName))
                ModelState.AddModelError("UserName", "This user name is already taken.");

            if (!ModelState.IsValid) return View("Form", model);

            // System generated default password
            string defaultPassword = PasswordHasher.GenerateDefaultPassword();
            string hash, salt;
            PasswordHasher.CreateHash(defaultPassword, out hash, out salt);

            var user = new AppUser
            {
                UserName = userName,
                FullName = model.FullName.Trim(),
                UserType = model.UserType,
                IsActive = model.IsActive,
                PasswordHash = hash,
                PasswordSalt = salt,
                MustChangePassword = true,
                CreatedOn = DateTime.Now
            };
            Db.Users.Add(user);
            Db.SaveChanges();

            ShowSuccess("User '" + userName + "' created. Default password: " + defaultPassword +
                        " — note it down; the user must change it at first login.");
            return RedirectToAction("Index");
        }

        // GET: /Users/Edit/5
        public ActionResult Edit(int id)
        {
            var user = Db.Users.Find(id);
            if (user == null) return HttpNotFound();

            return View("Form", new UserFormViewModel
            {
                UserId = user.UserId,
                UserName = user.UserName,
                FullName = user.FullName,
                UserType = user.UserType,
                IsActive = user.IsActive
            });
        }

        // POST: /Users/Edit – user name cannot be changed
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(UserFormViewModel model)
        {
            var user = Db.Users.Find(model.UserId);
            if (user == null) return HttpNotFound();

            ModelState.Remove("UserName");
            model.UserName = user.UserName;

            // Admin cannot lock himself out
            if (user.UserId == CurrentUserId && (model.UserType != UserType.Admin || !model.IsActive))
                ModelState.AddModelError("", "You cannot remove your own Admin rights or deactivate yourself.");

            if (!ModelState.IsValid) return View("Form", model);

            user.FullName = model.FullName.Trim();
            user.UserType = model.UserType;
            user.IsActive = model.IsActive;
            Db.SaveChanges();

            ShowSuccess("User '" + user.UserName + "' updated. Role changes apply from the user's next login.");
            return RedirectToAction("Index");
        }

        // POST: /Users/ResetPassword/5 – new default password + unlock account
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ResetPassword(int id)
        {
            var user = Db.Users.Find(id);
            if (user == null) return HttpNotFound();

            string newPassword = PasswordHasher.GenerateDefaultPassword();
            string hash, salt;
            PasswordHasher.CreateHash(newPassword, out hash, out salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.MustChangePassword = true;
            user.FailedLoginCount = 0;
            user.LockoutEndUtc = null;
            Db.SaveChanges();

            ShowSuccess("Password reset for '" + user.UserName + "'. New default password: " + newPassword);
            return RedirectToAction("Index");
        }
    }
}