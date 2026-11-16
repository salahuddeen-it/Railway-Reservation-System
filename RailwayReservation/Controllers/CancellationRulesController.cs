using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.Controllers
{
    /// <summary>Cancellation fee rules – deduction % based on hours left before departure.</summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class CancellationRulesController : BaseController
    {
        private const string BindFields = "TravelClass,MinHoursBeforeDeparture,DeductionPercent,MinimumCharge";

        // GET: /CancellationRules
        public ActionResult Index()
        {
            return View(Db.CancellationRules.OrderBy(c => c.TravelClass).ThenByDescending(c => c.MinHoursBeforeDeparture).ToList());
        }

        // GET: /CancellationRules/Create
        public ActionResult Create()
        {
            return View("Form", new CancellationRule());
        }

        // POST: /CancellationRules/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = BindFields)] CancellationRule rule)
        {
            ValidateRule(rule);
            if (!ModelState.IsValid) return View("Form", rule);

            Db.CancellationRules.Add(rule);
            Db.SaveChanges();
            ShowSuccess("Cancellation rule added.");
            return RedirectToAction("Index");
        }

        // GET: /CancellationRules/Edit/5
        public ActionResult Edit(int id)
        {
            var rule = Db.CancellationRules.Find(id);
            if (rule == null) return HttpNotFound();
            return View("Form", rule);
        }

        // POST: /CancellationRules/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CancellationRuleId," + BindFields)] CancellationRule rule)
        {
            var existing = Db.CancellationRules.Find(rule.CancellationRuleId);
            if (existing == null) return HttpNotFound();

            ValidateRule(rule);
            if (!ModelState.IsValid) return View("Form", rule);

            existing.TravelClass = rule.TravelClass;
            existing.MinHoursBeforeDeparture = rule.MinHoursBeforeDeparture;
            existing.DeductionPercent = rule.DeductionPercent;
            existing.MinimumCharge = rule.MinimumCharge;
            Db.SaveChanges();
            ShowSuccess("Cancellation rule updated.");
            return RedirectToAction("Index");
        }

        // POST: /CancellationRules/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var rule = Db.CancellationRules.Find(id);
            if (rule == null) return HttpNotFound();
            Db.CancellationRules.Remove(rule);
            Db.SaveChanges();
            ShowSuccess("Cancellation rule deleted.");
            return RedirectToAction("Index");
        }

        // Only one rule per class + hours combination
        private void ValidateRule(CancellationRule rule)
        {
            int id = rule.CancellationRuleId, hours = rule.MinHoursBeforeDeparture;
            TravelClass cls = rule.TravelClass;
            if (Db.CancellationRules.Any(c => c.CancellationRuleId != id && c.TravelClass == cls && c.MinHoursBeforeDeparture == hours))
                ModelState.AddModelError("MinHoursBeforeDeparture", "A rule for this class and hours already exists.");
        }
    }
}