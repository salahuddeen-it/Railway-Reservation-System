using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwayReservation.Models
{
    /// <summary>Reservation header – one PNR, many passengers.</summary>
    public class Reservation
    {
        public Reservation()
        {
            Passengers = new HashSet<Passenger>();
        }

        [Key]
        public int ReservationId { get; set; }

        [Required, StringLength(10)]
        [Index(IsUnique = true)]
        [Display(Name = "PNR No")]
        public string Pnr { get; set; }

        [ForeignKey("Train")]
        [Display(Name = "Train No")]
        public int TrainNo { get; set; }

        [Column(TypeName = "date")]
        [DataType(DataType.Date)]
        [Display(Name = "Journey Date")]
        public DateTime JourneyDate { get; set; }

        public int FromStationId { get; set; }
        public int ToStationId { get; set; }

        [Display(Name = "Class")]
        public TravelClass TravelClass { get; set; }

        [Display(Name = "Distance (km)")]
        public int DistanceKm { get; set; }

        [Display(Name = "Passengers")]
        public int PassengerCount { get; set; }

        // Fare break-up (totals for all passengers)
        [Display(Name = "Base Fare")] public decimal BaseFare { get; set; }
        [Display(Name = "Reservation Charge")] public decimal ReservationCharge { get; set; }
        [Display(Name = "Super Fast Charge")] public decimal SuperFastCharge { get; set; }
        [Display(Name = "Total Amount")] public decimal TotalAmount { get; set; }

        public ReservationStatus Status { get; set; }

        [Display(Name = "Booked On")]
        public DateTime BookedOn { get; set; }

        public int BookedByUserId { get; set; }

        [StringLength(15)]
        [Display(Name = "Contact Mobile")]
        public string ContactMobile { get; set; }

        // Cancellation details
        [Display(Name = "Cancelled On")] public DateTime? CancelledOn { get; set; }
        [Display(Name = "Cancellation Fee")] public decimal CancellationFee { get; set; }
        [Display(Name = "Refund Amount")] public decimal RefundAmount { get; set; }

        public virtual Train Train { get; set; }
        public virtual Station FromStation { get; set; }
        public virtual Station ToStation { get; set; }
        public virtual AppUser BookedBy { get; set; }
        public virtual ICollection<Passenger> Passengers { get; set; }
    }
}