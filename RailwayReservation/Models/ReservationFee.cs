using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwayReservation.Models
{
    /// <summary>Reservation charge and super fast charge per class (per passenger).</summary>
    public class ReservationFee
    {
        [Key]
        public int FeeId { get; set; }

        [Index(IsUnique = true)]
        [Display(Name = "Class")]
        public TravelClass TravelClass { get; set; }

        [Range(typeof(decimal), "0", "10000")]
        [Display(Name = "Reservation Charge")]
        public decimal ReservationCharge { get; set; }

        [Range(typeof(decimal), "0", "10000")]
        [Display(Name = "Super Fast Charge")]
        public decimal SuperFastCharge { get; set; }
    }
}