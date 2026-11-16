using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;
using RailwayReservation.ViewModels;

namespace RailwayReservation.Controllers
{
    /// <summary>Cancellation: whole PNR, or one passenger at a time.</summary>
    public class CancellationController : BaseController
    {
        private ReservationService _service;
        private ReservationService Service
        {
            get { return _service ?? (_service = new ReservationService(Db)); }
        }

        // GET: /Cancellation?pnr=...
        public ActionResult Index(string pnr)
        {
            var vm = new CancellationViewModel { Pnr = pnr };
            var passengerQuotes = new Dictionary<int, CancellationQuote>();

            if (!string.IsNullOrWhiteSpace(pnr))
            {
                var r = Service.FindByPnr(pnr);
                if (r == null || !CanAccess(r))
                {
                    vm.Errors.Add("PNR not found. Please check the number.");
                }
                else
                {
                    vm.Details = Service.BuildDetails(r);
                    if (r.Status != ReservationStatus.Cancelled)
                    {
                        // Whole PNR quote
                        var errors = new List<string>();
                        vm.Quote = Service.GetCancellationQuote(r, errors);
                        vm.Errors.AddRange(errors);

                        // One quote per active passenger (for single cancellation)
                        foreach (var p in r.Passengers.Where(x => x.Status != PassengerStatus.Cancelled))
                        {
                            var q = Service.GetCancellationQuote(r, p.PassengerId, new List<string>());
                            if (q != null) passengerQuotes[p.PassengerId] = q;
                        }
                    }
                }
            }

            ViewBag.PassengerQuotes = passengerQuotes;
            return View(vm);
        }

        // POST: /Cancellation/Cancel – whole PNR
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Cancel(string pnr)
        {
            return DoCancel(pnr, null);
        }

        // POST: /Cancellation/CancelPassenger – only one passenger
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CancelPassenger(string pnr, int passengerId)
        {
            return DoCancel(pnr, passengerId);
        }

        // Shared logic for both cancellations
        private ActionResult DoCancel(string pnr, int? passengerId)
        {
            var r = Service.FindByPnr(pnr);
            if (r == null || !CanAccess(r))
            {
                ShowError("PNR not found.");
                return RedirectToAction("Index");
            }

            var errors = new List<string>();
            int promoted = 0;
            CancellationQuote quote = null;
            try
            {
                quote = Service.Cancel(r, passengerId, CurrentUserId, errors, out promoted);
            }
            catch (DataException)
            {
                errors.Add("Could not cancel right now. Please try again.");
            }

            if (quote == null)
            {
                ShowError(string.Join(" ", errors));
                return RedirectToAction("Index", new { pnr = r.Pnr });
            }

            string who = passengerId.HasValue ? "Passenger" : "PNR " + r.Pnr;
            string message = who + " cancelled. Cancellation fee: Rs. " + quote.CancellationFee.ToString("N2") +
                             ", Refund: Rs. " + quote.RefundAmount.ToString("N2") + ".";
            if (promoted > 0) message += " " + promoted + " waitlisted passenger(s) got confirmed seats.";
            ShowSuccess(message);
            return RedirectToAction("Index", new { pnr = r.Pnr });
        }

        private bool CanAccess(Reservation r)
        {
            return IsStaff || r.BookedByUserId == CurrentUserId;
        }
    }
}