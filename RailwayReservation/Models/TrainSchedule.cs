using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwayReservation.Models
{
    /// <summary>
    /// Train schedule – one row per stop (start, in-between and end station)
    /// with arrival/departure time and distance from the source station.
    /// </summary>
    public class TrainSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [ForeignKey("Train")]
        [Index("IX_Train_Stop", 1, IsUnique = true)]
        [Display(Name = "Train No")]
        public int TrainNo { get; set; }

        [Index("IX_Train_Stop", 2, IsUnique = true)]
        [Range(1, 200)]
        [Display(Name = "Stop No")]
        public int StopNo { get; set; }

        [ForeignKey("Station")]
        [Display(Name = "Station")]
        public int StationId { get; set; }

        // Null for the first station (no arrival)
        [DataType(DataType.Time)]
        [Display(Name = "Arrival")]
        public TimeSpan? ArrivalTime { get; set; }

        // Null for the last station (no departure)
        [DataType(DataType.Time)]
        [Display(Name = "Departure")]
        public TimeSpan? DepartureTime { get; set; }

        [Range(0, 10000)]
        [Display(Name = "Distance from Source (km)")]
        public int DistanceFromSource { get; set; }

        // 0 = same day as journey start, 1 = next day, ...
        [Range(0, 7)]
        [Display(Name = "Day")]
        public int DayOffset { get; set; }

        public virtual Train Train { get; set; }
        public virtual Station Station { get; set; }
    }
}