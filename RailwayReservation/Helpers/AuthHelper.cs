using System;
using System.Web;
using System.Web.Security;
using RailwayReservation.Models;

namespace RailwayReservation.Helpers
{
    /// <summary>Creates the encrypted forms-authentication cookie after successful login.</summary>
    public static class AuthHelper
    {
        public static void SignIn(HttpResponseBase response, AppUser user, bool persistent)
        {
            // '|' is our separator, so remove it from the name
            string safeName = (user.FullName ?? string.Empty).Replace("|", " ");
            string userData = user.UserId + "|" + user.UserType + "|" + safeName;

            var ticket = new FormsAuthenticationTicket(
                1,
                user.UserName,
                DateTime.Now,
                DateTime.Now.Add(FormsAuthentication.Timeout),
                persistent,
                userData,
                FormsAuthentication.FormsCookiePath);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,                         // not readable by JavaScript
                Secure = FormsAuthentication.RequireSSL, // true when requireSSL="true" in Web.config
                Path = FormsAuthentication.FormsCookiePath,
                SameSite = SameSiteMode.Lax              // CSRF protection
            };
            if (persistent) cookie.Expires = ticket.Expiration;

            response.Cookies.Add(cookie);
        }
    }
}