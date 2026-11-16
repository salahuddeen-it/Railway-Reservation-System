using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.ViewModels
{
    /// <summary>One passenger row on the booking form (validated manually in controller).</summary>
    public class PassengerInput
    {
        public string Name { get; set; }
        public int? Age { get; set; }
        public Gender Gender { get; set; }
    }

    /// <summary>Booking form: train no, journey date, from/to station, class, passengers.</summary>
    public class BookingViewModel
    {
        public BookingViewModel()
        {
            Passengers = new List<PassengerInput>();
        }

        [Required(ErrorMessage = "Select a train.")]
        [Display(Name = "Train")]
        public int? TrainNo { get; set; }

        [Required(ErrorMessage = "Select journey date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Journey Date (train start date)")]
        public DateTime? JourneyDate { get; set; }

        [Required(ErrorMessage = "Select boarding station.")]
        [Display(Name = "From Station")]
        public int? FromStationId { get; set; }

        [Required(ErrorMessage = "Select destination station.")]
        [Display(Name = "To Station")]
        public int? ToStationId { get; set; }

        [Display(Name = "Class")]
        public TravelClass TravelClass { get; set; }

        [RegularExpression(@"^[0-9+\- ]{7,15}$", ErrorMessage = "Enter a valid mobile number.")]
        [Display(Name = "Contact Mobile")]
        public string ContactMobile { get; set; }

        public List<PassengerInput> Passengers { get; set; }
    }

    /// <summary>Data for the confirmation (review) screen.</summary>
    public class BookingReviewViewModel
    {
        public BookingViewModel Booking { get; set; }
        public BookingQuote Quote { get; set; }
    }

    /// <summary>Reservation + calculated departure/arrival date-time (ticket, query, cancellation).</summary>
    public class ReservationDetailsViewModel
    {
        public Reservation Reservation { get; set; }
        public DateTime? DepartureDateTime { get; set; }
        public DateTime? ArrivalDateTime { get; set; }
    }

    /// <summary>Cancellation screen data.</summary>
    public class CancellationViewModel
    {
        public CancellationViewModel()
        {
            Errors = new List<string>();
        }

        public string Pnr { get; set; }
        public ReservationDetailsViewModel Details { get; set; }
        public CancellationQuote Quote { get; set; }
        public List<string> Errors { get; set; }
    }
}