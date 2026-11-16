using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwayReservation.Models
{
    /// <summary>Day master – on which days of the week a train runs (one row per train).</summary>
    public class TrainRunDay
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "Train No")]
        public int TrainNo { get; set; }

        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }

        public virtual Train Train { get; set; }

        // Checks whether the train runs on the given day
        public bool RunsOn(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return Monday;
                case DayOfWeek.Tuesday: return Tuesday;
                case DayOfWeek.Wednesday: return Wednesday;
                case DayOfWeek.Thursday: return Thursday;
                case DayOfWeek.Friday: return Friday;
                case DayOfWeek.Saturday: return Saturday;
                default: return Sunday;
            }
        }

        // Human readable text: "Daily" or "Mon, Wed, Fri"
        [NotMapped]
        [Display(Name = "Runs On")]
        public string DaysText
        {
            get
            {
                if (Monday && Tuesday && Wednesday && Thursday && Friday && Saturday && Sunday) return "Daily";
                var days = new List<string>();
                if (Monday) days.Add("Mon");
                if (Tuesday) days.Add("Tue");
                if (Wednesday) days.Add("Wed");
                if (Thursday) days.Add("Thu");
                if (Friday) days.Add("Fri");
                if (Saturday) days.Add("Sat");
                if (Sunday) days.Add("Sun");
                return days.Count == 0 ? "Not running" : string.Join(", ", days);
            }
        }

        // Factory for a train that runs every day
        public static TrainRunDay Daily(int trainNo)
        {
            return new TrainRunDay
            {
                TrainNo = trainNo,
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
                Saturday = true,
                Sunday = true
            };
        }
    }
}