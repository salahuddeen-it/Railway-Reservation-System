using System;
using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Models;
using RailwayReservation.ViewModels;

namespace RailwayReservation.Controllers
{
    /// <summary>Dashboard – figures depend on the logged-in user's role.</summary>
    public class HomeController : BaseController
    {
        public ActionResult Index()
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
    }
}