using System.Web.Mvc;

namespace RailwayReservation
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // Every page requires login unless marked [AllowAnonymous]
            filters.Add(new AuthorizeAttribute());
        }
    }
}