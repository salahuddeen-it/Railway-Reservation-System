using System.ComponentModel.DataAnnotations;

namespace RailwayReservation.Models
{
    // Type of user – decides access level / privileges
    public enum UserType
    {
        Admin = 1,
        Clerk = 2,
        Customer = 3
    }

    // Reservable travel classes
    public enum TravelClass
    {
        [Display(Name = "AC First Class (1A)")] AC1 = 1,
        [Display(Name = "AC 2 Tier (2A)")] AC2 = 2,
        [Display(Name = "AC 3 Tier (3A)")] AC3 = 3,
        [Display(Name = "First Class (FC)")] FC = 4,
        [Display(Name = "Sleeper Class (SL)")] SL = 5
    }

    public enum TrainDirection { Up = 1, Down = 2 }

    public enum TrainType
    {
        Express = 1,
        [Display(Name = "Super Fast")] SuperFast = 2,
        Mail = 3
    }

    public enum ReservationStatus { Booked = 1, Cancelled = 2 }

    public enum PassengerStatus { Confirmed = 1, Waitlisted = 2, Cancelled = 3 }

    public enum Gender { Male = 1, Female = 2, Other = 3 }

    public enum CashTransactionType { Receipt = 1, Refund = 2 }

    /// <summary>
    /// Fixed seat layout per class: seats in one coach and the coach prefix (e.g. B1, S3).
    /// </summary>
    public static class TravelClassInfo
    {
        // Number of berths/seats in one coach of the given class
        public static int SeatsPerCoach(TravelClass travelClass)
        {
            switch (travelClass)
            {
                case TravelClass.AC1: return 24;
                case TravelClass.AC2: return 48;
                case TravelClass.AC3: return 64;
                case TravelClass.FC: return 30;
                default: return 72; // Sleeper
            }
        }

        // Prefix used to build the coach number (H1, A1, B1, F1, S1)
        public static string CoachPrefix(TravelClass travelClass)
        {
            switch (travelClass)
            {
                case TravelClass.AC1: return "H";
                case TravelClass.AC2: return "A";
                case TravelClass.AC3: return "B";
                case TravelClass.FC: return "F";
                default: return "S";
            }
        }
    }
}