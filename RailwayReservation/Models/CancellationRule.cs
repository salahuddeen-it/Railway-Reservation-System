using System.ComponentModel.DataAnnotations;

namespace RailwayReservation.Models
{
    /// <summary>
    /// Cancellation fee rule. The rule with the highest MinHoursBeforeDeparture
    /// that is &lt;= hours left before departure is applied.
    /// Fee = max(fare x DeductionPercent / 100, MinimumCharge), capped at fare.
    /// </summary>
    public class CancellationRule
    {
        [Key]
        public int CancellationRuleId { get; set; }

        [Display(Name = "Class")]
        public TravelClass TravelClass { get; set; }

        [Range(0, 720)]
        [Display(Name = "Min Hours Before Departure")]
        public int MinHoursBeforeDeparture { get; set; }

        [Range(typeof(decimal), "0", "100")]
        [Display(Name = "Deduction %")]
        public decimal DeductionPercent { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        [Display(Name = "Minimum Charge")]
        public decimal MinimumCharge { get; set; }
    }
}