using System.ComponentModel.DataAnnotations;

namespace RailwayReservation.Models
{
    /// <summary>
    /// Fare rule – distance slab based fare per class.
    /// Fare = distance x RatePerKm (of matching slab), but never below MinimumFare.
    /// </summary>
    public class FareRule
    {
        [Key]
        public int FareRuleId { get; set; }

        [Display(Name = "Class")]
        public TravelClass TravelClass { get; set; }

        [Range(0, 10000)]
        [Display(Name = "From (km)")]
        public int FromKm { get; set; }

        [Range(1, 10000)]
        [Display(Name = "To (km)")]
        public int ToKm { get; set; }

        [Range(typeof(decimal), "0.01", "100")]
        [Display(Name = "Rate / km")]
        public decimal RatePerKm { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        [Display(Name = "Minimum Fare")]
        public decimal MinimumFare { get; set; }
    }
}