using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using RailwayReservation.Helpers;
using RailwayReservation.ViewModels;

namespace RailwayReservation.Controllers
{
    /// <summary>Login, logout and change password (Security module).</summary>
    public class AccountController : BaseController
    {
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (Request.IsAuthenticated) return RedirectToAction("Dashboard", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            string userName = model.UserName.Trim();
            var user = Db.Users.FirstOrDefault(u => u.UserName == userName);

            // Same message for unknown user / wrong password (no user enumeration)
            const string invalidMsg = "Invalid user name or password.";

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", invalidMsg);
                return View(model);
            }

            // Account locked?
            if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
            {
                int minutesLeft = (int)Math.Ceiling((user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes);
                ModelState.AddModelError("", "Account locked due to repeated failed logins. Try again in " + minutesLeft + " minute(s).");
                return View(model);
            }

            // Wrong password -> count attempt, lock after MaxFailedAttempts
            if (!PasswordHasher.Verify(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= MaxFailedAttempts)
                {
                    user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    user.FailedLoginCount = 0;
                }
                Db.SaveChanges();
                ModelState.AddModelError("", invalidMsg);
                return View(model);
            }

            // Success
            user.FailedLoginCount = 0;
            user.LockoutEndUtc = null;
            user.LastLoginOn = DateTime.Now;
            Db.SaveChanges();

            AuthHelper.SignIn(Response, user, model.RememberMe);

            // First login with default password -> force change
            if (user.MustChangePassword)
            {
                ShowInfo("You are using a default password. Please set a new password.");
                return RedirectToAction("ChangePassword");
            }

            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl); // prevents open redirect
            return RedirectToAction("Dashboard", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        // GET: /Account/ChangePassword
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = Db.Users.Find(CurrentUserId);
            if (user == null)
            {
                FormsAuthentication.SignOut();
                return RedirectToAction("Login");
            }

            if (!PasswordHasher.Verify(model.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");

            if (model.NewPassword == model.CurrentPassword)
                ModelState.AddModelError("NewPassword", "New password must be different from the current password.");

            if (!ModelState.IsValid) return View(model);

            string hash, salt;
            PasswordHasher.CreateHash(model.NewPassword, out hash, out salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.MustChangePassword = false;
            Db.SaveChanges();

            ShowSuccess("Password changed successfully.");
            return RedirectToAction("Dashboard", "Home");
        }
    }
}