using System;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using RailwayReservation.DAL;
using RailwayReservation.Helpers;

namespace RailwayReservation
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Create database + seed master data on first run
            Database.SetInitializer(new RailwayDbInitializer());
            using (var db = new RailwayDbContext())
            {
                db.Database.Initialize(false);
            }
        }

        // Replace default identity with our RailwayPrincipal (UserId + Role from cookie)
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            HttpCookie cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null || string.IsNullOrEmpty(cookie.Value)) return;

            FormsAuthenticationTicket ticket;
            try
            {
                ticket = FormsAuthentication.Decrypt(cookie.Value);
            }
            catch (Exception)
            {
                return; // tampered / invalid cookie -> treat as anonymous
            }
            if (ticket == null || ticket.Expired) return;

            RailwayPrincipal principal = RailwayPrincipal.FromTicket(ticket);
            if (principal != null)
            {
                HttpContext.Current.User = principal;
                System.Threading.Thread.CurrentPrincipal = principal;
            }
        }
    }
}