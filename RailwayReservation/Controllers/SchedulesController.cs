using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.Controllers
{
    /// <summary>Train schedule – stations of a train with times and distance.</summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class SchedulesController : BaseController
    {
        private const string BindFields = "TrainNo,StopNo,StationId,ArrivalTime,DepartureTime,DistanceFromSource,DayOffset";

        // GET: /Schedules?trainNo=12951
        public ActionResult Index(int? trainNo)
        {
            ViewBag.TrainList = new SelectList(Db.Trains.OrderBy(t => t.TrainNo).ToList(), "TrainNo", "DisplayText", trainNo);
            ViewBag.Train = trainNo.HasValue ? Db.Trains.Find(trainNo.Value) : null;

            var stops = trainNo.HasValue
                ? Db.TrainSchedules.Include(s => s.Station).Where(s => s.TrainNo == trainNo.Value).OrderBy(s => s.StopNo).ToList()
                : new List<TrainSchedule>();
            return View(stops);
        }

        // GET: /Schedules/Create?trainNo=12951
        public ActionResult Create(int trainNo)
        {
            if (Db.Trains.Find(trainNo) == null) return HttpNotFound();
            int nextStop = (Db.TrainSchedules.Where(s => s.TrainNo == trainNo).Max(s => (int?)s.StopNo) ?? 0) + 1;
            var model = new TrainSchedule { TrainNo = trainNo, StopNo = nextStop };
            PrepareForm(model);
            return View("Form", model);
        }

        // POST: /Schedules/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = BindFields)] TrainSchedule stop)
        {
            ValidateStop(stop);
            if (!ModelState.IsValid)
            {
                PrepareForm(stop);
                return View("Form", stop);
            }
            Db.TrainSchedules.Add(stop);
            Db.SaveChanges();
            ShowSuccess("Stop added.");
            return RedirectToAction("Index", new { trainNo = stop.TrainNo });
        }

        // GET: /Schedules/Edit/5
        public ActionResult Edit(int id)
        {
            var stop = Db.TrainSchedules.Find(id);
            if (stop == null) return HttpNotFound();
            PrepareForm(stop);
            return View("Form", stop);
        }

        // POST: /Schedules/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ScheduleId," + BindFields)] TrainSchedule stop)
        {
            var existing = Db.TrainSchedules.Find(stop.ScheduleId);
            if (existing == null) return HttpNotFound();
            stop.TrainNo = existing.TrainNo; // train of a stop cannot change

            ValidateStop(stop);
            if (!ModelState.IsValid)
            {
                PrepareForm(stop);
                return View("Form", stop);
            }

            existing.StopNo = stop.StopNo;
            existing.StationId = stop.StationId;
            existing.ArrivalTime = stop.ArrivalTime;
            existing.DepartureTime = stop.DepartureTime;
            existing.DistanceFromSource = stop.DistanceFromSource;
            existing.DayOffset = stop.DayOffset;
            Db.SaveChanges();

            ShowSuccess("Stop updated.");
            return RedirectToAction("Index", new { trainNo = existing.TrainNo });
        }

        // POST: /Schedules/Delete/5 – blocked if active bookings use this station on this train
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var stop = Db.TrainSchedules.Find(id);
            if (stop == null) return HttpNotFound();

            int trainNo = stop.TrainNo, stationId = stop.StationId;
            bool used = Db.Reservations.Any(r => r.TrainNo == trainNo && r.Status == ReservationStatus.Booked &&
                                                 (r.FromStationId == stationId || r.ToStationId == stationId));
            if (used)
            {
                ShowError("Active reservations exist from/to this station on this train. Stop cannot be deleted.");
                return RedirectToAction("Index", new { trainNo });
            }

            Db.TrainSchedules.Remove(stop);
            Db.SaveChanges();
            ShowSuccess("Stop deleted.");
            return RedirectToAction("Index", new { trainNo });
        }

        // Dropdown data + train info for the form
        private void PrepareForm(TrainSchedule stop)
        {
            ViewBag.StationList = new SelectList(Db.Stations.OrderBy(s => s.StationCode).ToList(), "StationId", "DisplayText", stop.StationId);
            ViewBag.Train = Db.Trains.Find(stop.TrainNo);
        }

        // Business rules for a schedule stop
        private void ValidateStop(TrainSchedule stop)
        {
            int trainNo = stop.TrainNo, scheduleId = stop.ScheduleId;
            if (Db.Trains.Find(trainNo) == null)
            {
                ModelState.AddModelError("", "Train not found.");
                return;
            }

            var others = Db.TrainSchedules.Where(s => s.TrainNo == trainNo && s.ScheduleId != scheduleId).ToList();

            if (others.Any(s => s.StopNo == stop.StopNo))
                ModelState.AddModelError("StopNo", "This stop number already exists for the train.");
            if (others.Any(s => s.StationId == stop.StationId))
                ModelState.AddModelError("StationId", "This station is already in the train's route.");

            var prev = others.Where(s => s.StopNo < stop.StopNo).OrderByDescending(s => s.StopNo).FirstOrDefault();
            var next = others.Where(s => s.StopNo > stop.StopNo).OrderBy(s => s.StopNo).FirstOrDefault();

            if (prev == null && stop.DistanceFromSource != 0)
                ModelState.AddModelError("DistanceFromSource", "First station distance must be 0 km.");
            if (prev != null && stop.DistanceFromSource <= prev.DistanceFromSource)
                ModelState.AddModelError("DistanceFromSource", "Distance must be greater than previous stop (" + prev.DistanceFromSource + " km).");
            if (next != null && stop.DistanceFromSource >= next.DistanceFromSource)
                ModelState.AddModelError("DistanceFromSource", "Distance must be less than next stop (" + next.DistanceFromSource + " km).");

            if (prev == null && !stop.DepartureTime.HasValue)
                ModelState.AddModelError("DepartureTime", "First station must have a departure time.");
            if (prev != null && !stop.ArrivalTime.HasValue)
                ModelState.AddModelError("ArrivalTime", "Arrival time is required for in-between and last stations.");
            if (prev != null && stop.DayOffset < prev.DayOffset)
                ModelState.AddModelError("DayOffset", "Day cannot be earlier than previous stop's day.");
        }
    }
}