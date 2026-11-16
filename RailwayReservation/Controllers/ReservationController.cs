using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;
using RailwayReservation.ViewModels;

namespace RailwayReservation.Controllers
{
    /// <summary>Reservation: booking form → review/confirmation → save → ticket.</summary>
    public class ReservationController : BaseController
    {
        private ReservationService _service;
        private ReservationService Service
        {
            get { return _service ?? (_service = new ReservationService(Db)); }
        }

        // GET: /Reservation/Create
        public ActionResult Create()
        {
            var model = new BookingViewModel { JourneyDate = DateTime.Today.AddDays(1), TravelClass = TravelClass.AC3 };
            EnsureRows(model);
            PrepareLists(model);
            return View(model);
        }

        // POST: /Reservation/Create → validate, calculate fare, show confirmation page
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(BookingViewModel model)
        {
            var passengers = CleanPassengers(model);
            BookingQuote quote = null;

            if (ModelState.IsValid)
            {
                var errors = new List<string>();
                quote = Service.BuildQuote(model.TrainNo.Value, model.JourneyDate.Value, model.FromStationId.Value,
                                           model.ToStationId.Value, model.TravelClass, passengers.Count, errors);
                foreach (var e in errors) ModelState.AddModelError("", e);
            }

            if (!ModelState.IsValid || quote == null)
            {
                EnsureRows(model);
                PrepareLists(model);
                return View(model);
            }

            model.Passengers = passengers;
            return View("Review", new BookingReviewViewModel { Booking = model, Quote = quote });
        }

        // POST: /Reservation/Confirm → final save (re-validates everything)
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Confirm(BookingViewModel model)
        {
            var passengers = CleanPassengers(model);
            var errors = new List<string>();
            Reservation reservation = null;

            if (ModelState.IsValid)
            {
                try
                {
                    reservation = Service.Book(model.TrainNo.Value, model.JourneyDate.Value, model.FromStationId.Value,
                                               model.ToStationId.Value, model.TravelClass, passengers,
                                               model.ContactMobile, CurrentUserId, errors);
                }
                catch (DataException)
                {
                    errors.Add("Seat data changed while booking. Please try again.");
                }
            }

            if (reservation == null)
            {
                foreach (var e in errors) ModelState.AddModelError("", e);
                if (ModelState.IsValid) ModelState.AddModelError("", "Booking could not be completed.");
                EnsureRows(model);
                PrepareLists(model);
                return View("Create", model);
            }

            ShowSuccess("Ticket booked successfully. PNR: " + reservation.Pnr);
            return RedirectToAction("Ticket", new { pnr = reservation.Pnr });
        }

        // GET: /Reservation/Ticket?pnr=1234567890 – printable ticket
        public ActionResult Ticket(string pnr)
        {
            var r = Service.FindByPnr(pnr);
            if (r == null || !CanAccess(r)) return HttpNotFound();
            return View(Service.BuildDetails(r));
        }

        // GET: /Reservation/MyBookings – bookings made by current user
        public ActionResult MyBookings()
        {
            int uid = CurrentUserId;
            var list = Db.Reservations
                .Include(r => r.Train).Include(r => r.FromStation).Include(r => r.ToStation)
                .Where(r => r.BookedByUserId == uid)
                .OrderByDescending(r => r.JourneyDate).ThenByDescending(r => r.BookedOn)
                .ToList();
            return View(list);
        }

        // GET: /Reservation/Stops?trainNo=12951 – JSON for From/To dropdowns (AJAX)
        public JsonResult Stops(int trainNo)
        {
            var stops = Db.TrainSchedules.Include(s => s.Station)
                .Where(s => s.TrainNo == trainNo).OrderBy(s => s.StopNo).ToList()
                .Select(s => new { id = s.StationId, text = s.Station.StationCode + " - " + s.Station.StationName })
                .ToList();
            var runDay = Db.TrainRunDays.Find(trainNo);
            return Json(new { stops = stops, runsOn = runDay != null ? runDay.DaysText : "Not set" }, JsonRequestBehavior.AllowGet);
        }

        // ---------- helpers ----------

        // Staff can see every PNR, customers only their own
        private bool CanAccess(Reservation r)
        {
            return IsStaff || r.BookedByUserId == CurrentUserId;
        }

        // Keep only filled passenger rows and validate them
        private List<PassengerInput> CleanPassengers(BookingViewModel model)
        {
            if (model.Passengers == null) model.Passengers = new List<PassengerInput>();
            var result = new List<PassengerInput>();

            for (int i = 0; i < model.Passengers.Count; i++)
            {
                var p = model.Passengers[i];
                if (p == null || string.IsNullOrWhiteSpace(p.Name)) continue;

                p.Name = p.Name.Trim();
                if (p.Name.Length > 100)
                    ModelState.AddModelError("Passengers[" + i + "].Name", "Name is too long (max 100).");
                if (!p.Age.HasValue || p.Age.Value < 1 || p.Age.Value > 120)
                    ModelState.AddModelError("Passengers[" + i + "].Age", "Enter a valid age (1-120).");
                result.Add(p);
            }

            if (result.Count == 0) ModelState.AddModelError("", "Enter at least one passenger.");
            if (result.Count > ReservationService.MaxPassengers)
                ModelState.AddModelError("", "Maximum " + ReservationService.MaxPassengers + " passengers per ticket.");
            return result;
        }

        // Always show 6 passenger rows on the form
        private static void EnsureRows(BookingViewModel model)
        {
            if (model.Passengers == null) model.Passengers = new List<PassengerInput>();
            while (model.Passengers.Count < ReservationService.MaxPassengers)
                model.Passengers.Add(new PassengerInput());
        }

        // Dropdown data for the booking form
        private void PrepareLists(BookingViewModel model)
        {
            ViewBag.TrainList = new SelectList(Db.Trains.Where(t => t.IsActive).OrderBy(t => t.TrainNo).ToList(),
                                               "TrainNo", "DisplayText", model.TrainNo);

            var stops = new List<TrainSchedule>();
            string runDays = "";
            if (model.TrainNo.HasValue)
            {
                int trainNo = model.TrainNo.Value;
                stops = Db.TrainSchedules.Include(s => s.Station).Where(s => s.TrainNo == trainNo).OrderBy(s => s.StopNo).ToList();
                var runDay = Db.TrainRunDays.Find(trainNo);
                runDays = runDay != null ? "Runs on: " + runDay.DaysText : "";
            }

            var items = stops.Select(s => new { s.StationId, Text = s.Station.StationCode + " - " + s.Station.StationName }).ToList();
            ViewBag.FromList = new SelectList(items, "StationId", "Text", model.FromStationId);
            ViewBag.ToList = new SelectList(items, "StationId", "Text", model.ToStationId);
            ViewBag.RunDaysText = runDays;
        }
    }
}