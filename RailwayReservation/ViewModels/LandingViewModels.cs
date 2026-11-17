using System;
using System.ComponentModel.DataAnnotations;

namespace RailwayReservation.ViewModels
{
    /// <summary>Contact form on the landing page.</summary>
    public class ContactViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Your Name")]
        public string Name { get; set; }

        [Required, StringLength(150), EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required, StringLength(150)]
        public string Subject { get; set; }

        [Required, StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be at least 10 characters.")]
        public string Message { get; set; }
    }

    /// <summary>One train shown on the landing page (data comes from the database).</summary>
    public class LandingTrainItem
    {
        public int TrainNo { get; set; }
        public string TrainName { get; set; }
        public string TrainType { get; set; }
        public string FromCode { get; set; }
        public string FromName { get; set; }
        public string ToCode { get; set; }
        public string ToName { get; set; }
        public TimeSpan? Departure { get; set; }
        public int DistanceKm { get; set; }
        public string RunsOn { get; set; }
    }
}