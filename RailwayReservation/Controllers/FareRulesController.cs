using System.Linq;
using System.Web.Mvc;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.Controllers
{
    /// <summary>Fare rule – distance slab wise rate per km for each class.</summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class FareRulesController : BaseController
    {
        // GET: /FareRules
        public ActionResult Index()
        {
            return View(Db.FareRules.OrderBy(f => f.TravelClass).ThenBy(f => f.FromKm).ToList());
        }

        // GET: /FareRules/Create
        public ActionResult Create()
        {
            return View("Form", new FareRule { FromKm = 0, ToKm = 500 });
        }

        // POST: /FareRules/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TravelClass,FromKm,ToKm,RatePerKm,MinimumFare")] FareRule rule)
        {
            ValidateRule(rule);
            if (!ModelState.IsValid) return View("Form", rule);

            Db.FareRules.Add(rule);
            Db.SaveChanges();
            ShowSuccess("Fare rule added.");
            return RedirectToAction("Index");
        }

        // GET: /FareRules/Edit/5
        public ActionResult Edit(int id)
        {
            var rule = Db.FareRules.Find(id);
            if (rule == null) return HttpNotFound();
            return View("Form", rule);
        }

        // POST: /FareRules/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FareRuleId,TravelClass,FromKm,ToKm,RatePerKm,MinimumFare")] FareRule rule)
        {
            var existing = Db.FareRules.Find(rule.FareRuleId);
            if (existing == null) return HttpNotFound();

            ValidateRule(rule);
            if (!ModelState.IsValid) return View("Form", rule);

            existing.TravelClass = rule.TravelClass;
            existing.FromKm = rule.FromKm;
            existing.ToKm = rule.ToKm;
            existing.RatePerKm = rule.RatePerKm;
            existing.MinimumFare = rule.MinimumFare;
            Db.SaveChanges();
            ShowSuccess("Fare rule updated.");
            return RedirectToAction("Index");
        }

        // POST: /FareRules/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var rule = Db.FareRules.Find(id);
            if (rule == null) return HttpNotFound();
            Db.FareRules.Remove(rule);
            Db.SaveChanges();
            ShowSuccess("Fare rule deleted.");
            return RedirectToAction("Index");
        }

        // From < To, and no overlapping slab for the same class
        private void ValidateRule(FareRule rule)
        {
            if (rule.FromKm >= rule.ToKm)
                ModelState.AddModelError("ToKm", "'To km' must be greater than 'From km'.");

            int id = rule.FareRuleId, from = rule.FromKm, to = rule.ToKm;
            TravelClass cls = rule.TravelClass;
            bool overlap = Db.FareRules.Any(f => f.FareRuleId != id && f.TravelClass == cls && f.FromKm <= to && f.ToKm >= from);
            if (overlap)
                ModelState.AddModelError("", "This distance range overlaps another slab of the same class.");
        }
    }
}