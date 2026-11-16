using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.Controllers
{
    /// <summary>Train master – train details and number of coaches per class.</summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class TrainsController : BaseController
    {
        private const string BindFields =
            "TrainNo,TrainName,Direction,TrainType,RouteId,AC1Coaches,AC2Coaches,AC3Coaches,FCCoaches,SLCoaches,GeneralCoaches,IsActive";

        // GET: /Trains
        public ActionResult Index()
        {
            return View(Db.Trains.Include(t => t.RunDay).OrderBy(t => t.TrainNo).ToList());
        }

        // GET: /Trains/Create
        public ActionResult Create()
        {
            ViewBag.IsEdit = false;
            return View("Form", new Train { Direction = TrainDirection.Up, TrainType = TrainType.Express, IsActive = true });
        }

        // POST: /Trains/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = BindFields)] Train train)
        {
            int trainNo = train.TrainNo;
            if (Db.Trains.Any(t => t.TrainNo == trainNo))
                ModelState.AddModelError("TrainNo", "This train number already exists.");
            ValidateCoaches(train);

            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = false;
                return View("Form", train);
            }

            train.TrainName = train.TrainName.Trim();
            Db.Trains.Add(train);
            Db.TrainRunDays.Add(TrainRunDay.Daily(train.TrainNo)); // default: runs daily (change in Day Master)
            Db.SaveChanges();

            ShowSuccess("Train " + train.TrainNo + " added. Now add its schedule (stations) and set running days.");
            return RedirectToAction("Index");
        }

        // GET: /Trains/Edit/12951
        public ActionResult Edit(int id)
        {
            var train = Db.Trains.Find(id);
            if (train == null) return HttpNotFound();
            ViewBag.IsEdit = true;
            return View("Form", train);
        }

        // POST: /Trains/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = BindFields)] Train train)
        {
            var existing = Db.Trains.Find(train.TrainNo);
            if (existing == null) return HttpNotFound();
            ValidateCoaches(train);

            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = true;
                return View("Form", train);
            }

            existing.TrainName = train.TrainName.Trim();
            existing.Direction = train.Direction;
            existing.TrainType = train.TrainType;
            existing.RouteId = train.RouteId;
            existing.AC1Coaches = train.AC1Coaches;
            existing.AC2Coaches = train.AC2Coaches;
            existing.AC3Coaches = train.AC3Coaches;
            existing.FCCoaches = train.FCCoaches;
            existing.SLCoaches = train.SLCoaches;
            existing.GeneralCoaches = train.GeneralCoaches;
            existing.IsActive = train.IsActive;
            Db.SaveChanges();

            ShowSuccess("Train " + train.TrainNo + " updated.");
            return RedirectToAction("Index");
        }

        // POST: /Trains/Delete/12951 – only if no reservations exist
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var train = Db.Trains.Find(id);
            if (train == null) return HttpNotFound();

            if (Db.Reservations.Any(r => r.TrainNo == id))
            {
                ShowError("Train " + id + " has reservations. Deactivate it instead (Edit → untick Active).");
                return RedirectToAction("Index");
            }

            var stops = Db.TrainSchedules.Where(s => s.TrainNo == id).ToList();
            Db.TrainSchedules.RemoveRange(stops);
            var runDay = Db.TrainRunDays.Find(id);
            if (runDay != null) Db.TrainRunDays.Remove(runDay);
            Db.Trains.Remove(train);
            Db.SaveChanges();

            ShowSuccess("Train " + id + " deleted.");
            return RedirectToAction("Index");
        }

        // At least one reservable coach is required
        private void ValidateCoaches(Train train)
        {
            int reservable = train.AC1Coaches + train.AC2Coaches + train.AC3Coaches + train.FCCoaches + train.SLCoaches;
            if (reservable == 0)
                ModelState.AddModelError("", "Train must have at least one reservable coach (1A, 2A, 3A, FC or SL).");
        }
    }
}