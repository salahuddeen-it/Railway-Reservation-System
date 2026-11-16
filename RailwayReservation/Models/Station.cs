using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwayReservation.Models
{
    /// <summary>Station master – StationId is system generated (identity).</summary>
    public class Station
    {
        [Key]
        [Display(Name = "Station ID")]
        public int StationId { get; set; }

        [Required, StringLength(10)]
        [Index(IsUnique = true)]
        [RegularExpression("^[A-Z]{2,10}$", ErrorMessage = "Station code must be 2-10 capital letters (e.g. NDLS).")]
        [Display(Name = "Station Code")]
        public string StationCode { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Station Name")]
        public string StationName { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Railway Division")]
        public string DivisionName { get; set; }

        // Used in dropdowns: "NDLS - New Delhi"
        [NotMapped]
        public string DisplayText
        {
            get { return StationCode + " - " + StationName; }
        }
    }
}