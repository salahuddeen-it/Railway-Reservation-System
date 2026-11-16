using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.Controllers
{
    /// <summary>Station master – add / edit / delete / search stations.</summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class StationsController : BaseController
    {
        // GET: /Stations
        public ActionResult Index(string search)
        {
            var query = Db.Stations.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(x => x.StationCode.Contains(s) || x.StationName.Contains(s) || x.DivisionName.Contains(s));
            }
            ViewBag.Search = search;
            return View(query.OrderBy(x => x.StationCode).ToList());
        }

        // GET: /Stations/Create
        public ActionResult Create()
        {
            return View("Form", new Station());
        }

        // POST: /Stations/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "StationCode,StationName,DivisionName")] Station station)
        {
            Normalize(station);
            string code = station.StationCode;
            if (code != null && Db.Stations.Any(s => s.StationCode == code))
                ModelState.AddModelError("StationCode", "This station code already exists.");

            if (!ModelState.IsValid) return View("Form", station);

            Db.Stations.Add(station);
            Db.SaveChanges();
            ShowSuccess("Station " + station.StationCode + " added (ID " + station.StationId + ").");
            return RedirectToAction("Index");
        }

        // GET: /Stations/Edit/5
        public ActionResult Edit(int id)
        {
            var station = Db.Stations.Find(id);
            if (station == null) return HttpNotFound();
            return View("Form", station);
        }

        // POST: /Stations/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "StationId,StationCode,StationName,DivisionName")] Station station)
        {
            var existing = Db.Stations.Find(station.StationId);
            if (existing == null) return HttpNotFound();

            Normalize(station);
            string code = station.StationCode;
            int id = station.StationId;
            if (code != null && Db.Stations.Any(s => s.StationCode == code && s.StationId != id))
                ModelState.AddModelError("StationCode", "This station code already exists.");

            if (!ModelState.IsValid) return View("Form", station);

            existing.StationCode = station.StationCode;
            existing.StationName = station.StationName;
            existing.DivisionName = station.DivisionName;
            Db.SaveChanges();
            ShowSuccess("Station updated.");
            return RedirectToAction("Index");
        }

        // POST: /Stations/Delete/5 – blocked if station is used anywhere
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var station = Db.Stations.Find(id);
            if (station == null) return HttpNotFound();

            bool inUse = Db.TrainSchedules.Any(s => s.StationId == id) ||
                         Db.Reservations.Any(r => r.FromStationId == id || r.ToStationId == id);
            if (inUse)
            {
                ShowError("Station " + station.StationCode + " is used in train schedules or reservations and cannot be deleted.");
                return RedirectToAction("Index");
            }

            Db.Stations.Remove(station);
            Db.SaveChanges();
            ShowSuccess("Station deleted.");
            return RedirectToAction("Index");
        }

        // Trim text and force station code to upper case
        private static void Normalize(Station station)
        {
            if (station.StationCode != null) station.StationCode = station.StationCode.Trim().ToUpperInvariant();
            if (station.StationName != null) station.StationName = station.StationName.Trim();
            if (station.DivisionName != null) station.DivisionName = station.DivisionName.Trim();
        }
    }
}