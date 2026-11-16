using System;
using System.Security.Principal;
using System.Web.Security;

namespace RailwayReservation.Helpers
{
    /// <summary>
    /// Custom principal: carries UserId, Role and FullName from the encrypted auth cookie,
    /// so we don't hit the database on every request.
    /// </summary>
    public class RailwayPrincipal : IPrincipal
    {
        public IIdentity Identity { get; private set; }
        public int UserId { get; private set; }
        public string Role { get; private set; }
        public string FullName { get; private set; }

        public RailwayPrincipal(IIdentity identity, int userId, string role, string fullName)
        {
            Identity = identity;
            UserId = userId;
            Role = role;
            FullName = fullName;
        }

        // Used by [Authorize(Roles = "...")]
        public bool IsInRole(string role)
        {
            return string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
        }

        // Builds principal from ticket UserData = "userId|role|fullName"
        public static RailwayPrincipal FromTicket(FormsAuthenticationTicket ticket)
        {
            string[] parts = (ticket.UserData ?? string.Empty).Split(new[] { '|' }, 3);
            if (parts.Length < 2) return null;

            int userId;
            if (!int.TryParse(parts[0], out userId)) return null;

            string fullName = parts.Length > 2 ? parts[2] : ticket.Name;
            return new RailwayPrincipal(new FormsIdentity(ticket), userId, parts[1], fullName);
        }
    }
}