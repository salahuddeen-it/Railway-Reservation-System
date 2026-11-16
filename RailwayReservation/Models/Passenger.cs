using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwayReservation.Models
{
    /// <summary>Passenger detail – each passenger gets a coach + seat or a waitlist number.</summary>
    public class Passenger
    {
        [Key]
        public int PassengerId { get; set; }

        [ForeignKey("Reservation")]
        public int ReservationId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Range(1, 120)]
        public int Age { get; set; }

        public Gender Gender { get; set; }

        public PassengerStatus Status { get; set; }

        [StringLength(5)]
        [Display(Name = "Coach")]
        public string CoachNo { get; set; }

        [Display(Name = "Seat")]
        public int? SeatNo { get; set; }

        [Display(Name = "WL No")]
        public int? WaitlistNo { get; set; }

        public virtual Reservation Reservation { get; set; }
    }
}