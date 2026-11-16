using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.Controllers
{
    /// <summary>Reservation charge and super fast charge per class.</summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class ReservationFeesController : BaseController
    {
        // GET: /ReservationFees
        public ActionResult Index()
        {
            return View(Db.ReservationFees.OrderBy(f => f.TravelClass).ToList());
        }

        // GET: /ReservationFees/Create
        public ActionResult Create()
        {
            return View("Form", new ReservationFee());
        }

        // POST: /ReservationFees/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TravelClass,ReservationCharge,SuperFastCharge")] ReservationFee fee)
        {
            ValidateFee(fee);
            if (!ModelState.IsValid) return View("Form", fee);

            Db.ReservationFees.Add(fee);
            Db.SaveChanges();
            ShowSuccess("Fees added.");
            return RedirectToAction("Index");
        }

        // GET: /ReservationFees/Edit/5
        public ActionResult Edit(int id)
        {
            var fee = Db.ReservationFees.Find(id);
            if (fee == null) return HttpNotFound();
            return View("Form", fee);
        }

        // POST: /ReservationFees/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FeeId,TravelClass,ReservationCharge,SuperFastCharge")] ReservationFee fee)
        {
            var existing = Db.ReservationFees.Find(fee.FeeId);
            if (existing == null) return HttpNotFound();

            ValidateFee(fee);
            if (!ModelState.IsValid) return View("Form", fee);

            existing.TravelClass = fee.TravelClass;
            existing.ReservationCharge = fee.ReservationCharge;
            existing.SuperFastCharge = fee.SuperFastCharge;
            Db.SaveChanges();
            ShowSuccess("Fees updated.");
            return RedirectToAction("Index");
        }

        // Only one fee row per class
        private void ValidateFee(ReservationFee fee)
        {
            int id = fee.FeeId;
            TravelClass cls = fee.TravelClass;
            if (Db.ReservationFees.Any(f => f.FeeId != id && f.TravelClass == cls))
                ModelState.AddModelError("TravelClass", "Fees for this class already exist. Edit that row instead.");
        }
    }
}