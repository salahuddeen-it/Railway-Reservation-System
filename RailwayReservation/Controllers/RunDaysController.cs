using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.Controllers
{
    /// <summary>Day master – days of the week on which each train runs.</summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class RunDaysController : BaseController
    {
        // GET: /RunDays
        public ActionResult Index()
        {
            return View(Db.Trains.Include(t => t.RunDay).OrderBy(t => t.TrainNo).ToList());
        }

        // GET: /RunDays/Edit/12951
        public ActionResult Edit(int id)
        {
            var train = Db.Trains.Include(t => t.RunDay).FirstOrDefault(t => t.TrainNo == id);
            if (train == null) return HttpNotFound();
            ViewBag.TrainText = train.DisplayText;
            return View(train.RunDay ?? new TrainRunDay { TrainNo = id });
        }

        // POST: /RunDays/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TrainNo,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday")] TrainRunDay model)
        {
            var train = Db.Trains.Find(model.TrainNo);
            if (train == null) return HttpNotFound();

            bool anyDay = model.Monday || model.Tuesday || model.Wednesday || model.Thursday ||
                          model.Friday || model.Saturday || model.Sunday;
            if (!anyDay) ModelState.AddModelError("", "Select at least one running day.");

            if (!ModelState.IsValid)
            {
                ViewBag.TrainText = train.DisplayText;
                return View(model);
            }

            var existing = Db.TrainRunDays.Find(model.TrainNo);
            if (existing == null)
            {
                Db.TrainRunDays.Add(model);
            }
            else
            {
                existing.Monday = model.Monday;
                existing.Tuesday = model.Tuesday;
                existing.Wednesday = model.Wednesday;
                existing.Thursday = model.Thursday;
                existing.Friday = model.Friday;
                existing.Saturday = model.Saturday;
                existing.Sunday = model.Sunday;
            }
            Db.SaveChanges();

            ShowSuccess("Running days saved for train " + model.TrainNo + ".");
            return RedirectToAction("Index");
        }
    }
}