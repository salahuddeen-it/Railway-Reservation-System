using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.Controllers
{
    /// <summary>
    /// "Update seat no for unconfirmed passenger" – scheduled activity.
    /// Allocates vacant (cancelled) seats to waitlisted passengers for a journey date.
    /// </summary>
    [Authorize(Roles = AppRoles.AdminOrClerk)]
    public class WaitlistController : BaseController
    {
        // GET: /Waitlist?date=2026-11-20
        public ActionResult Index(DateTime? date)
        {
            DateTime d = (date ?? DateTime.Today.AddDays(1)).Date;
            ViewBag.Date = d;

            var pending = Db.Passengers.Include(p => p.Reservation.Train)
                .Where(p => p.Status == PassengerStatus.Waitlisted &&
                            p.Reservation.Status == ReservationStatus.Booked &&
                            p.Reservation.JourneyDate == d)
                .OrderBy(p => p.Reservation.TrainNo).ThenBy(p => p.WaitlistNo)
                .ToList();
            return View(pending);
        }

        // POST: /Waitlist/Allocate
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Allocate(DateTime date)
        {
            var service = new ReservationService(Db);
            List<Passenger> promoted;
            try
            {
                using (var tx = Db.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    promoted = service.AllocateWaitlist(date.Date);
                    tx.Commit();
                }
            }
            catch (DataException)
            {
                ShowError("Allocation failed. Please try again.");
                return RedirectToAction("Index", new { date = date.ToString("yyyy-MM-dd") });
            }

            if (promoted.Count == 0)
            {
                ShowInfo("No vacant seats available for waitlisted passengers on " + date.ToString("dd MMM yyyy") + ".");
            }
            else
            {
                var lines = promoted.Select(p => "PNR " + p.Reservation.Pnr + " – " + p.Name + " → " + p.CoachNo + "/" + p.SeatNo);
                ShowSuccess(promoted.Count + " passenger(s) confirmed: " + string.Join("; ", lines));
            }
            return RedirectToAction("Index", new { date = date.ToString("yyyy-MM-dd") });
        }
    }
}