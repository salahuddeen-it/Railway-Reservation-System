using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwayReservation.Models
{
    /// <summary>Daily cash transaction – money received (booking) or refunded (cancellation).</summary>
    public class CashTransaction
    {
        [Key]
        public int CashTransactionId { get; set; }

        [Display(Name = "Date / Time")]
        public DateTime TransactionDate { get; set; }

        [ForeignKey("Reservation")]
        public int ReservationId { get; set; }

        [Required, StringLength(10)]
        [Display(Name = "PNR No")]
        public string Pnr { get; set; }

        public CashTransactionType Type { get; set; }

        public decimal Amount { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [StringLength(200)]
        public string Remarks { get; set; }

        public virtual Reservation Reservation { get; set; }
        public virtual AppUser User { get; set; }
    }
}