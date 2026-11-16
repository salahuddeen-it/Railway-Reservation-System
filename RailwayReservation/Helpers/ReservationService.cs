using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using RailwayReservation.DAL;
using RailwayReservation.Models;
using RailwayReservation.ViewModels;

namespace RailwayReservation.Helpers
{
    /// <summary>Fare + availability calculation result for a booking request.</summary>
    public class BookingQuote
    {
        public Train Train { get; set; }
        public TrainSchedule FromStop { get; set; }
        public TrainSchedule ToStop { get; set; }
        public int DistanceKm { get; set; }
        public int PassengerCount { get; set; }
        public decimal FarePerPassenger { get; set; }
        public decimal ReservationChargePerPassenger { get; set; }
        public decimal SuperFastChargePerPassenger { get; set; }
        public DateTime? DepartureDateTime { get; set; }
        public DateTime? ArrivalDateTime { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }

        public decimal BaseFare { get { return FarePerPassenger * PassengerCount; } }
        public decimal ReservationCharge { get { return ReservationChargePerPassenger * PassengerCount; } }
        public decimal SuperFastCharge { get { return SuperFastChargePerPassenger * PassengerCount; } }
        public decimal TotalAmount { get { return BaseFare + ReservationCharge + SuperFastCharge; } }
    }

    /// <summary>Cancellation fee / refund calculation result.</summary>
    public class CancellationQuote
    {
        public DateTime DepartureDateTime { get; set; }
        public double HoursLeft { get; set; }
        public CancellationRule Rule { get; set; }
        public decimal CancellationFee { get; set; }
        public decimal NonRefundableCharges { get; set; }
        public decimal RefundAmount { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// Business logic for reservation, seat allocation, cancellation and waitlist.
    /// Seats are segment-aware: a seat freed after a passenger's destination
    /// can be given to another passenger for a later part of the route.
    /// </summary>
    public class ReservationService
    {
        public const int MaxPassengers = 6;
        private readonly RailwayDbContext db;

        public ReservationService(RailwayDbContext db)
        {
            this.db = db;
        }

        // ================= QUOTE (validation + fare + availability) =================
        public BookingQuote BuildQuote(int trainNo, DateTime journeyDate, int fromStationId, int toStationId,
                                       TravelClass travelClass, int passengerCount, List<string> errors)
        {
            DateTime today = DateTime.Today;
            DateTime date = journeyDate.Date;

            // Journey date must be greater than system date and within 90 days
            if (date <= today) errors.Add("Journey date must be after today's date.");
            if (date > today.AddDays(90)) errors.Add("Journey date must be within 90 days from today.");
            if (passengerCount < 1 || passengerCount > MaxPassengers)
                errors.Add("Passengers must be between 1 and " + MaxPassengers + ".");

            var train = db.Trains.Include(t => t.RunDay).FirstOrDefault(t => t.TrainNo == trainNo);
            if (train == null || !train.IsActive)
            {
                errors.Add("Train not found or not active.");
                return null;
            }

            // Train must run on the selected day
            if (train.RunDay == null || !train.RunDay.RunsOn(date.DayOfWeek))
                errors.Add("Train " + trainNo + " does not run on " + date.ToString("dddd") + ".");

            // From / To station checks
            var stops = db.TrainSchedules.Include(s => s.Station).Where(s => s.TrainNo == trainNo).ToList();
            var from = stops.FirstOrDefault(s => s.StationId == fromStationId);
            var to = stops.FirstOrDefault(s => s.StationId == toStationId);
            if (from == null) errors.Add("Train does not stop at the selected From station.");
            if (to == null) errors.Add("Train does not stop at the selected To station.");
            if (from != null && to != null && from.StopNo >= to.StopNo)
                errors.Add("From station must come before To station on this train's route.");

            if (train.GetCoachCount(travelClass) == 0)
                errors.Add("This train has no " + travelClass.GetDisplayName() + " coaches.");

            if (errors.Count > 0) return null;

            // Fare by distance slab
            int distance = to.DistanceFromSource - from.DistanceFromSource;
            TravelClass cls = travelClass;
            var rule = db.FareRules.FirstOrDefault(f => f.TravelClass == cls && f.FromKm <= distance && f.ToKm >= distance);
            if (rule == null)
            {
                errors.Add("No fare rule defined for " + travelClass.GetDisplayName() + " for " + distance + " km.");
                return null;
            }
            decimal fare = Math.Max(Math.Round(distance * rule.RatePerKm, 0, MidpointRounding.AwayFromZero), rule.MinimumFare);

            // Reservation + super fast charges
            var fee = db.ReservationFees.FirstOrDefault(f => f.TravelClass == cls);
            decimal reservationCharge = fee != null ? fee.ReservationCharge : 0m;
            decimal superFastCharge = (fee != null && train.TrainType == TrainType.SuperFast) ? fee.SuperFastCharge : 0m;

            // Seat availability for this segment
            int totalSeats = train.GetTotalSeats(travelClass);
            var occupied = GetOccupiedSeats(trainNo, date, travelClass, from.DistanceFromSource, to.DistanceFromSource, stops);

            return new BookingQuote
            {
                Train = train,
                FromStop = from,
                ToStop = to,
                DistanceKm = distance,
                PassengerCount = passengerCount,
                FarePerPassenger = fare,
                ReservationChargePerPassenger = reservationCharge,
                SuperFastChargePerPassenger = superFastCharge,
                DepartureDateTime = StopTime(date, from, true),
                ArrivalDateTime = StopTime(date, to, false),
                TotalSeats = totalSeats,
                AvailableSeats = Math.Max(0, totalSeats - occupied.Count)
            };
        }

        // ================= BOOK (save reservation, PNR, coach/seat, cash receipt) =================
        public Reservation Book(int trainNo, DateTime journeyDate, int fromStationId, int toStationId, TravelClass travelClass,
                                List<PassengerInput> passengers, string contactMobile, int userId, List<string> errors)
        {
            // Serializable transaction: two clerks cannot get the same seat
            using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var quote = BuildQuote(trainNo, journeyDate, fromStationId, toStationId, travelClass, passengers.Count, errors);
                if (quote == null) return null;

                DateTime date = journeyDate.Date;
                var stops = db.TrainSchedules.Where(s => s.TrainNo == trainNo).ToList();
                var occupied = GetOccupiedSeats(trainNo, date, travelClass,
                    quote.FromStop.DistanceFromSource, quote.ToStop.DistanceFromSource, stops);
                int nextWaitlist = NextWaitlistNo(trainNo, date, travelClass);

                var reservation = new Reservation
                {
                    Pnr = GenerateUniquePnr(),
                    TrainNo = trainNo,
                    JourneyDate = date,
                    FromStationId = fromStationId,
                    ToStationId = toStationId,
                    TravelClass = travelClass,
                    DistanceKm = quote.DistanceKm,
                    PassengerCount = passengers.Count,
                    BaseFare = quote.BaseFare,
                    ReservationCharge = quote.ReservationCharge,
                    SuperFastCharge = quote.SuperFastCharge,
                    TotalAmount = quote.TotalAmount,
                    Status = ReservationStatus.Booked,
                    BookedOn = DateTime.Now,
                    BookedByUserId = userId,
                    ContactMobile = string.IsNullOrWhiteSpace(contactMobile) ? null : contactMobile.Trim()
                };

                // Allocate coach + seat, otherwise waitlist number
                foreach (var p in passengers)
                {
                    var passenger = new Passenger { Name = p.Name, Age = p.Age ?? 0, Gender = p.Gender };
                    string coachNo;
                    int seatNo;
                    if (TryAllocateSeat(quote.Train, travelClass, occupied, out coachNo, out seatNo))
                    {
                        passenger.Status = PassengerStatus.Confirmed;
                        passenger.CoachNo = coachNo;
                        passenger.SeatNo = seatNo;
                    }
                    else
                    {
                        passenger.Status = PassengerStatus.Waitlisted;
                        passenger.WaitlistNo = nextWaitlist++;
                    }
                    reservation.Passengers.Add(passenger);
                }

                db.Reservations.Add(reservation);

                // Daily cash: money received
                db.CashTransactions.Add(new CashTransaction
                {
                    Reservation = reservation,
                    Pnr = reservation.Pnr,
                    Type = CashTransactionType.Receipt,
                    Amount = reservation.TotalAmount,
                    UserId = userId,
                    TransactionDate = DateTime.Now,
                    Remarks = "Ticket booked"
                });

                db.SaveChanges();
                tx.Commit();
                return reservation;
            }
        }

        // ================= FIND / DETAILS =================
        public Reservation FindByPnr(string pnr)
        {
            if (string.IsNullOrWhiteSpace(pnr)) return null;
            string value = pnr.Trim();
            return db.Reservations
                .Include(r => r.Train)
                .Include(r => r.FromStation)
                .Include(r => r.ToStation)
                .Include(r => r.BookedBy)
                .Include(r => r.Passengers)
                .FirstOrDefault(r => r.Pnr == value);
        }

        public ReservationDetailsViewModel BuildDetails(Reservation r)
        {
            int trainNo = r.TrainNo, fromId = r.FromStationId, toId = r.ToStationId;
            var stops = db.TrainSchedules.Where(s => s.TrainNo == trainNo && (s.StationId == fromId || s.StationId == toId)).ToList();
            return new ReservationDetailsViewModel
            {
                Reservation = r,
                DepartureDateTime = StopTime(r.JourneyDate, stops.FirstOrDefault(s => s.StationId == fromId), true),
                ArrivalDateTime = StopTime(r.JourneyDate, stops.FirstOrDefault(s => s.StationId == toId), false)
            };
        }

        public DateTime GetDepartureDateTime(Reservation r)
        {
            int trainNo = r.TrainNo, stationId = r.FromStationId;
            var stop = db.TrainSchedules.FirstOrDefault(s => s.TrainNo == trainNo && s.StationId == stationId);
            return StopTime(r.JourneyDate, stop, true) ?? r.JourneyDate.Date;
        }

        // ================= CANCELLATION =================

        // Whole PNR (all passengers not yet cancelled)
        public CancellationQuote GetCancellationQuote(Reservation r, List<string> errors)
        {
            return GetCancellationQuote(r, null, errors);
        }

        // passengerId == null -> whole PNR, otherwise only that one passenger
        public CancellationQuote GetCancellationQuote(Reservation r, int? passengerId, List<string> errors)
        {
            if (r.Status == ReservationStatus.Cancelled)
            {
                errors.Add("This PNR is already cancelled.");
                return null;
            }

            var active = r.Passengers.Where(p => p.Status != PassengerStatus.Cancelled).ToList();
            List<Passenger> toCancel;
            if (passengerId.HasValue)
            {
                var one = active.FirstOrDefault(p => p.PassengerId == passengerId.Value);
                if (one == null)
                {
                    errors.Add("Passenger not found or already cancelled.");
                    return null;
                }
                toCancel = new List<Passenger> { one };
            }
            else
            {
                toCancel = active;
            }
            if (toCancel.Count == 0)
            {
                errors.Add("No active passengers on this PNR.");
                return null;
            }

            DateTime departure = GetDepartureDateTime(r);
            double hoursLeft = (departure - DateTime.Now).TotalHours;
            if (hoursLeft <= 0)
            {
                errors.Add("Journey date/time has already passed. This ticket cannot be cancelled.");
                return null;
            }

            // Amounts per passenger (based on original booking)
            int originalCount = Math.Max(1, r.PassengerCount);
            decimal farePerPassenger = r.BaseFare / originalCount;
            decimal chargesPerPassenger = (r.ReservationCharge + r.SuperFastCharge) / originalCount;
            int confirmedCount = toCancel.Count(p => p.Status == PassengerStatus.Confirmed);

            var quote = new CancellationQuote
            {
                DepartureDateTime = departure,
                HoursLeft = hoursLeft,
                NonRefundableCharges = Math.Round(chargesPerPassenger * toCancel.Count, 2)
            };

            decimal fee = 0m;
            if (confirmedCount == 0)
            {
                quote.Note = "Waitlisted passenger(s) – full fare refunded (only reservation/super fast charges are kept).";
            }
            else
            {
                int wholeHours = (int)Math.Floor(hoursLeft);
                TravelClass cls = r.TravelClass;
                quote.Rule = db.CancellationRules
                    .Where(c => c.TravelClass == cls && c.MinHoursBeforeDeparture <= wholeHours)
                    .OrderByDescending(c => c.MinHoursBeforeDeparture)
                    .FirstOrDefault();

                decimal confirmedFare = farePerPassenger * confirmedCount;
                if (quote.Rule == null)
                {
                    fee = confirmedFare;
                    quote.Note = "No cancellation rule found for this class – fare is not refundable.";
                }
                else
                {
                    decimal percentFee = Math.Round(confirmedFare * quote.Rule.DeductionPercent / 100m, 2);
                    decimal minimumFee = quote.Rule.MinimumCharge * confirmedCount;
                    fee = Math.Min(Math.Max(percentFee, minimumFee), confirmedFare);
                }
            }

            quote.CancellationFee = Math.Round(fee, 2);
            decimal paidForThese = (farePerPassenger + chargesPerPassenger) * toCancel.Count;
            quote.RefundAmount = Math.Max(0m, Math.Round(paidForThese - quote.NonRefundableCharges - quote.CancellationFee, 2));
            return quote;
        }

        // Whole PNR cancellation
        public CancellationQuote Cancel(Reservation r, int userId, List<string> errors, out int promotedCount)
        {
            return Cancel(r, null, userId, errors, out promotedCount);
        }

        // passengerId == null -> whole PNR, otherwise only that passenger
        public CancellationQuote Cancel(Reservation r, int? passengerId, int userId, List<string> errors, out int promotedCount)
        {
            promotedCount = 0;
            using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var quote = GetCancellationQuote(r, passengerId, errors);
                if (quote == null) return null;

                var toCancel = passengerId.HasValue
                    ? r.Passengers.Where(p => p.PassengerId == passengerId.Value).ToList()
                    : r.Passengers.Where(p => p.Status != PassengerStatus.Cancelled).ToList();

                foreach (var p in toCancel) p.Status = PassengerStatus.Cancelled; // coach/seat kept for history

                // Fees/refunds add up across partial cancellations
                r.CancellationFee += quote.CancellationFee;
                r.RefundAmount += quote.RefundAmount;

                // PNR becomes "Cancelled" only when every passenger is cancelled
                if (r.Passengers.All(p => p.Status == PassengerStatus.Cancelled))
                {
                    r.Status = ReservationStatus.Cancelled;
                    r.CancelledOn = DateTime.Now;
                }

                if (quote.RefundAmount > 0)
                {
                    db.CashTransactions.Add(new CashTransaction
                    {
                        ReservationId = r.ReservationId,
                        Pnr = r.Pnr,
                        Type = CashTransactionType.Refund,
                        Amount = quote.RefundAmount,
                        UserId = userId,
                        TransactionDate = DateTime.Now,
                        Remarks = passengerId.HasValue ? "Passenger cancelled: " + toCancel[0].Name : "Ticket cancelled"
                    });
                }
                db.SaveChanges();

                // Freed seats go to waitlisted passengers immediately
                promotedCount = AllocateWaitlist(r.JourneyDate, r.TrainNo, r.TravelClass).Count;

                tx.Commit();
                return quote;
            }
        }

        // ================= WAITLIST -> CONFIRMED =================
        public List<Passenger> AllocateWaitlist(DateTime journeyDate, int? trainNo = null, TravelClass? travelClass = null)
        {
            DateTime d = journeyDate.Date;
            IQueryable<Passenger> query = db.Passengers.Include(p => p.Reservation)
                .Where(p => p.Status == PassengerStatus.Waitlisted &&
                            p.Reservation.Status == ReservationStatus.Booked &&
                            p.Reservation.JourneyDate == d);
            if (trainNo.HasValue)
            {
                int tNo = trainNo.Value;
                query = query.Where(p => p.Reservation.TrainNo == tNo);
            }
            if (travelClass.HasValue)
            {
                TravelClass c = travelClass.Value;
                query = query.Where(p => p.Reservation.TravelClass == c);
            }

            var waiting = query.ToList();
            var promoted = new List<Passenger>();

            foreach (var group in waiting.GroupBy(p => new { p.Reservation.TrainNo, p.Reservation.TravelClass }))
            {
                var train = db.Trains.Find(group.Key.TrainNo);
                if (train == null) continue;
                var stops = db.TrainSchedules.Where(s => s.TrainNo == train.TrainNo).ToList();
                var kmByStation = stops.ToDictionary(s => s.StationId, s => s.DistanceFromSource);

                // Lowest waitlist number gets seat first
                foreach (var p in group.OrderBy(x => x.WaitlistNo).ThenBy(x => x.PassengerId))
                {
                    int fromKm, toKm;
                    if (!kmByStation.TryGetValue(p.Reservation.FromStationId, out fromKm) ||
                        !kmByStation.TryGetValue(p.Reservation.ToStationId, out toKm)) continue;

                    var occupied = GetOccupiedSeats(train.TrainNo, d, group.Key.TravelClass, fromKm, toKm, stops);
                    string coachNo;
                    int seatNo;
                    if (TryAllocateSeat(train, group.Key.TravelClass, occupied, out coachNo, out seatNo))
                    {
                        p.Status = PassengerStatus.Confirmed;
                        p.CoachNo = coachNo;
                        p.SeatNo = seatNo;
                        p.WaitlistNo = null;
                        db.SaveChanges(); // save now so next passenger sees this seat as taken
                        promoted.Add(p);
                    }
                }
            }
            return promoted;
        }

        // ================= PRIVATE HELPERS =================

        // Seats already taken by confirmed passengers whose journey overlaps [fromKm, toKm)
        private HashSet<string> GetOccupiedSeats(int trainNo, DateTime journeyDate, TravelClass travelClass,
                                                 int fromKm, int toKm, List<TrainSchedule> stops)
        {
            var kmByStation = stops.ToDictionary(s => s.StationId, s => s.DistanceFromSource);
            DateTime d = journeyDate.Date;
            TravelClass cls = travelClass;

            var booked = db.Passengers
                .Where(p => p.Status == PassengerStatus.Confirmed &&
                            p.Reservation.Status == ReservationStatus.Booked &&
                            p.Reservation.TrainNo == trainNo &&
                            p.Reservation.JourneyDate == d &&
                            p.Reservation.TravelClass == cls)
                .Select(p => new { p.CoachNo, p.SeatNo, p.Reservation.FromStationId, p.Reservation.ToStationId })
                .ToList();

            var occupied = new HashSet<string>();
            foreach (var b in booked)
            {
                int bFrom, bTo;
                bool known = kmByStation.TryGetValue(b.FromStationId, out bFrom) & kmByStation.TryGetValue(b.ToStationId, out bTo);
                // Unknown route data -> treat seat as occupied (safe side)
                if (!known || (fromKm < bTo && bFrom < toKm))
                    occupied.Add(SeatKey(b.CoachNo, b.SeatNo ?? 0));
            }
            return occupied;
        }

        // First free seat: coach B1 seat 1, 2 ... then B2 ...
        private static bool TryAllocateSeat(Train train, TravelClass travelClass, HashSet<string> occupied,
                                            out string coachNo, out int seatNo)
        {
            int perCoach = TravelClassInfo.SeatsPerCoach(travelClass);
            int coaches = train.GetCoachCount(travelClass);
            string prefix = TravelClassInfo.CoachPrefix(travelClass);

            for (int c = 1; c <= coaches; c++)
            {
                for (int s = 1; s <= perCoach; s++)
                {
                    string coach = prefix + c;
                    string key = SeatKey(coach, s);
                    if (!occupied.Contains(key))
                    {
                        occupied.Add(key);
                        coachNo = coach;
                        seatNo = s;
                        return true;
                    }
                }
            }
            coachNo = null;
            seatNo = 0;
            return false;
        }

        private static string SeatKey(string coachNo, int seatNo)
        {
            return coachNo + "-" + seatNo;
        }

        private int NextWaitlistNo(int trainNo, DateTime journeyDate, TravelClass travelClass)
        {
            DateTime d = journeyDate.Date;
            TravelClass cls = travelClass;
            int? max = db.Passengers
                .Where(p => p.Status == PassengerStatus.Waitlisted &&
                            p.Reservation.Status == ReservationStatus.Booked &&
                            p.Reservation.TrainNo == trainNo &&
                            p.Reservation.JourneyDate == d &&
                            p.Reservation.TravelClass == cls)
                .Max(p => p.WaitlistNo);
            return (max ?? 0) + 1;
        }

        // Random unique 10-digit PNR
        private string GenerateUniquePnr()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    byte[] bytes = new byte[8];
                    rng.GetBytes(bytes);
                    ulong number = BitConverter.ToUInt64(bytes, 0) % 9000000000UL + 1000000000UL;
                    string pnr = number.ToString();
                    if (!db.Reservations.Any(r => r.Pnr == pnr)) return pnr;
                }
            }
            throw new InvalidOperationException("Could not generate a unique PNR. Please try again.");
        }

        // Date-time of a stop = journey (train start) date + day offset + time
        private static DateTime? StopTime(DateTime journeyDate, TrainSchedule stop, bool departure)
        {
            if (stop == null) return null;
            TimeSpan? time = departure ? stop.DepartureTime : stop.ArrivalTime;
            if (!time.HasValue) return null;
            return journeyDate.Date.AddDays(stop.DayOffset).Add(time.Value);
        }
    }
}