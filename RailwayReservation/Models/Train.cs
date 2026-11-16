using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwayReservation.Models
{
    /// <summary>Train master – every train is uniquely identified by its train number.</summary>
    public class Train
    {
        public Train()
        {
            Schedules = new HashSet<TrainSchedule>();
            IsActive = true;
        }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Range(10000, 99999, ErrorMessage = "Train number must be 5 digits.")]
        [Display(Name = "Train No")]
        public int TrainNo { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Train Name")]
        public string TrainName { get; set; }

        [Display(Name = "Up / Down")]
        public TrainDirection Direction { get; set; }

        [Display(Name = "Train Type")]
        public TrainType TrainType { get; set; }

        [StringLength(20)]
        [Display(Name = "Route ID")]
        public string RouteId { get; set; }

        // Number of coaches available per class
        [Range(0, 10)][Display(Name = "1A Coaches")] public int AC1Coaches { get; set; }
        [Range(0, 10)][Display(Name = "2A Coaches")] public int AC2Coaches { get; set; }
        [Range(0, 15)][Display(Name = "3A Coaches")] public int AC3Coaches { get; set; }
        [Range(0, 5)][Display(Name = "First Class Coaches")] public int FCCoaches { get; set; }
        [Range(0, 20)][Display(Name = "Sleeper Coaches")] public int SLCoaches { get; set; }
        [Range(0, 10)][Display(Name = "General Coaches")] public int GeneralCoaches { get; set; } // unreserved

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public virtual ICollection<TrainSchedule> Schedules { get; set; }
        public virtual TrainRunDay RunDay { get; set; }

        // Returns coach count for a reservable class
        public int GetCoachCount(TravelClass travelClass)
        {
            switch (travelClass)
            {
                case TravelClass.AC1: return AC1Coaches;
                case TravelClass.AC2: return AC2Coaches;
                case TravelClass.AC3: return AC3Coaches;
                case TravelClass.FC: return FCCoaches;
                default: return SLCoaches;
            }
        }

        // Total seats in a class = coaches x seats per coach
        public int GetTotalSeats(TravelClass travelClass)
        {
            return GetCoachCount(travelClass) * TravelClassInfo.SeatsPerCoach(travelClass);
        }

        [NotMapped]
        [Display(Name = "Total Coaches")]
        public int TotalCoaches
        {
            get { return AC1Coaches + AC2Coaches + AC3Coaches + FCCoaches + SLCoaches + GeneralCoaches; }
        }

        [NotMapped]
        public string DisplayText
        {
            get { return TrainNo + " - " + TrainName; }
        }
    }
}