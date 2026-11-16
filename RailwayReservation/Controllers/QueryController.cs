using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.ViewModels;

namespace RailwayReservation.Controllers
{
    /// <summary>Query module – full reservation/cancellation details and current seat status by PNR.</summary>
    public class QueryController : BaseController
    {
        // GET: /Query?pnr=1234567890
        public ActionResult Index(string pnr)
        {
            ViewBag.Pnr = pnr;
            if (string.IsNullOrWhiteSpace(pnr)) return View((ReservationDetailsViewModel)null);

            var service = new ReservationService(Db);
            var r = service.FindByPnr(pnr);
            if (r == null || !(IsStaff || r.BookedByUserId == CurrentUserId))
            {
                ViewBag.NotFound = true;
                return View((ReservationDetailsViewModel)null);
            }
            return View(service.BuildDetails(r));
        }
    }
}