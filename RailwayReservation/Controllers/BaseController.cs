using System.Web.Mvc;
using RailwayReservation.DAL;
using RailwayReservation.Helpers;

namespace RailwayReservation.Controllers
{
    /// <summary>
    /// Common base for all controllers: shared DbContext, current user info,
    /// and helper methods for flash messages.
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected readonly RailwayDbContext Db = new RailwayDbContext();

        protected RailwayPrincipal CurrentUser
        {
            get { return User as RailwayPrincipal; }
        }

        protected int CurrentUserId
        {
            get { return CurrentUser != null ? CurrentUser.UserId : 0; }
        }

        // Admin or Clerk
        protected bool IsStaff
        {
            get
            {
                return CurrentUser != null &&
                       (CurrentUser.IsInRole(AppRoles.Admin) || CurrentUser.IsInRole(AppRoles.Clerk));
            }
        }

        // Flash messages shown once on the next page (_Alerts partial)
        protected void ShowSuccess(string message) { TempData["Success"] = message; }
        protected void ShowError(string message) { TempData["Error"] = message; }
        protected void ShowInfo(string message) { TempData["Info"] = message; }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Db.Dispose();
            base.Dispose(disposing);
        }
    }
}