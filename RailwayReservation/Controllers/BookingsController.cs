using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.Controllers
{
    /// <summary>All bookings – every passenger of every train, with Train / Date / Status filters.</summary>
    [Authorize(Roles = AppRoles.AdminOrClerk)]
    public class BookingsController : BaseController
    {
        // GET: /Bookings?trainNo=12951&date=2026-11-17&status=Confirmed
        public ActionResult Index(int? trainNo, DateTime? date, string status)
        {
            IQueryable<Passenger> query = Db.Passengers
                .Include(p => p.Reservation.Train)
                .Include(p => p.Reservation.FromStation)
                .Include(p => p.Reservation.ToStation)
                .Include(p => p.Reservation.BookedBy);

            // Filters
            if (trainNo.HasValue)
            {
                int t = trainNo.Value;
                query = query.Where(p => p.Reservation.TrainNo == t);
            }
            if (date.HasValue)
            {
                DateTime d = date.Value.Date;
                query = query.Where(p => p.Reservation.JourneyDate == d);
            }
            PassengerStatus st;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse(status, out st))
            {
                query = query.Where(p => p.Status == st);
            }

            var list = query
                .OrderByDescending(p => p.Reservation.JourneyDate)
                .ThenBy(p => p.Reservation.TrainNo)
                .ThenBy(p => p.Reservation.Pnr)
                .ThenBy(p => p.PassengerId)
                .Take(1000)
                .ToList();

            ViewBag.TrainList = new SelectList(Db.Trains.OrderBy(t => t.TrainNo).ToList(), "TrainNo", "DisplayText", trainNo);
            ViewBag.StatusList = new SelectList(new[] { "Confirmed", "Waitlisted", "Cancelled" }, status);
            ViewBag.FilterDate = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : "";
            return View(list);
        }
    }
}