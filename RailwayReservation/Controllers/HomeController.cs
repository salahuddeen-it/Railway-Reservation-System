using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;
using RailwayReservation.ViewModels;

namespace RailwayReservation.Controllers
{
    /// <summary>Public landing page (home, trains, about, contact) and the logged-in dashboard.</summary>
    public class HomeController : BaseController
    {
        private static readonly object ContactFileLock = new object();

        // GET: /  – public landing page (no login needed)
        [AllowAnonymous]
        public ActionResult Index()
        {
            LoadLandingData();
            return View(new ContactViewModel());
        }

        // POST: /Home/Contact – saves the message in App_Data/ContactMessages.csv
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public ActionResult Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadLandingData();
                ViewBag.ScrollToContact = true;
                return View("Index", model);
            }

            string path = Server.MapPath("~/App_Data/ContactMessages.csv");
            string line = string.Join(",",
                Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm")),
                Csv(model.Name), Csv(model.Email), Csv(model.Subject), Csv(model.Message));

            lock (ContactFileLock)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                if (!System.IO.File.Exists(path))
                    System.IO.File.WriteAllText(path, "Date,Name,Email,Subject,Message" + Environment.NewLine, new UTF8Encoding(true));
                System.IO.File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }

            ShowSuccess("Thank you, " + model.Name.Trim() + "! Your message has been received. We will get back to you soon.");
            return Redirect(Url.Action("Index") + "#contact");
        }

        // GET: /Home/Dashboard – role-based dashboard after login
        public ActionResult Dashboard()
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            var vm = new DashboardViewModel
            {
                FullName = CurrentUser != null ? CurrentUser.FullName : User.Identity.Name,
                Role = CurrentUser != null ? CurrentUser.Role : "",
                IsStaff = IsStaff
            };

            if (vm.IsStaff)
            {
                vm.TotalTrains = Db.Trains.Count(t => t.IsActive);
                vm.TotalStations = Db.Stations.Count();
                vm.TodaysBookings = Db.Reservations.Count(r => r.BookedOn >= today && r.BookedOn < tomorrow);
                vm.TodaysReceipts = Db.CashTransactions
                    .Where(c => c.Type == CashTransactionType.Receipt && c.TransactionDate >= today && c.TransactionDate < tomorrow)
                    .Select(c => (decimal?)c.Amount).Sum() ?? 0m;
                vm.TodaysRefunds = Db.CashTransactions
                    .Where(c => c.Type == CashTransactionType.Refund && c.TransactionDate >= today && c.TransactionDate < tomorrow)
                    .Select(c => (decimal?)c.Amount).Sum() ?? 0m;
                vm.WaitlistedPassengers = Db.Passengers
                    .Count(p => p.Status == PassengerStatus.Waitlisted && p.Reservation.JourneyDate >= today);
            }
            else
            {
                int uid = CurrentUserId;
                vm.MyUpcomingTrips = Db.Reservations.Count(r =>
                    r.BookedByUserId == uid && r.Status == ReservationStatus.Booked && r.JourneyDate >= today);
            }
            return View(vm);
        }

        // Trains + counters for the landing page
        private void LoadLandingData()
        {
            var trains = Db.Trains
                .Include(t => t.RunDay)
                .Include(t => t.Schedules.Select(s => s.Station))
                .Where(t => t.IsActive)
                .OrderBy(t => t.TrainNo)
                .Take(6)
                .ToList();

            var items = new List<LandingTrainItem>();
            foreach (var t in trains)
            {
                var stops = t.Schedules.OrderBy(s => s.StopNo).ToList();
                if (stops.Count < 2) continue;
                var first = stops[0];
                var last = stops[stops.Count - 1];
                items.Add(new LandingTrainItem
                {
                    TrainNo = t.TrainNo,
                    TrainName = t.TrainName,
                    TrainType = t.TrainType.GetDisplayName(),
                    FromCode = first.Station.StationCode,
                    FromName = first.Station.StationName,
                    ToCode = last.Station.StationCode,
                    ToName = last.Station.StationName,
                    Departure = first.DepartureTime,
                    DistanceKm = last.DistanceFromSource,
                    RunsOn = t.RunDay != null ? t.RunDay.DaysText : "-"
                });
            }

            ViewBag.Trains = items;
            ViewBag.TrainCount = Db.Trains.Count(t => t.IsActive);
            ViewBag.StationCount = Db.Stations.Count();
        }

        // Safe CSV value (quotes + formula-injection protection)
        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            value = value.Replace("\r", " ").Replace("\n", " ").Trim();
            if ("=+-@".IndexOf(value[0]) >= 0) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}