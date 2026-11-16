using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using RailwayReservation.Helpers;
using RailwayReservation.Models;

namespace RailwayReservation.DAL
{
    /// <summary>
    /// Creates the database on first run and fills master data
    /// (users, stations, trains, schedules, fare, fees, cancellation rules).
    /// </summary>
    public class RailwayDbInitializer : CreateDatabaseIfNotExists<RailwayDbContext>
    {
        protected override void Seed(RailwayDbContext db)
        {
            // ---------- Users (login: admin/Admin123, clerk/Clerk123, customer/Customer123) ----------
            db.Users.Add(NewUser("admin", "System Administrator", UserType.Admin, "Admin123"));
            db.Users.Add(NewUser("clerk", "Booking Clerk", UserType.Clerk, "Clerk123"));
            db.Users.Add(NewUser("customer", "Demo Customer", UserType.Customer, "Customer123"));
            db.SaveChanges();

            // ---------- Stations ----------
            var stations = new List<Station>
            {
                new Station { StationCode = "BCT",  StationName = "Mumbai Central",     DivisionName = "Mumbai (WR)" },
                new Station { StationCode = "BRC",  StationName = "Vadodara Junction",  DivisionName = "Vadodara" },
                new Station { StationCode = "KOTA", StationName = "Kota Junction",      DivisionName = "Kota" },
                new Station { StationCode = "NDLS", StationName = "New Delhi",          DivisionName = "Delhi" },
                new Station { StationCode = "AGC",  StationName = "Agra Cantt",         DivisionName = "Agra" },
                new Station { StationCode = "JHS",  StationName = "Jhansi Junction",    DivisionName = "Jhansi" },
                new Station { StationCode = "BPL",  StationName = "Bhopal Junction",    DivisionName = "Bhopal" },
                new Station { StationCode = "HWH",  StationName = "Howrah Junction",    DivisionName = "Howrah" },
                new Station { StationCode = "CNB",  StationName = "Kanpur Central",     DivisionName = "Prayagraj" },
                new Station { StationCode = "MAS",  StationName = "Chennai Central",    DivisionName = "Chennai" }
            };
            db.Stations.AddRange(stations);
            db.SaveChanges();

            // Helper: station code -> generated StationId
            Func<string, int> sid = code => stations.Single(s => s.StationCode == code).StationId;

            // ---------- Trains ----------
            db.Trains.Add(new Train
            {
                TrainNo = 12951,
                TrainName = "Mumbai Rajdhani Express",
                Direction = TrainDirection.Up,
                TrainType = TrainType.SuperFast,
                RouteId = "R-BCT-NDLS",
                AC1Coaches = 1,
                AC2Coaches = 3,
                AC3Coaches = 6,
                FCCoaches = 0,
                SLCoaches = 0,
                GeneralCoaches = 0
            });
            db.Trains.Add(new Train
            {
                TrainNo = 12002,
                TrainName = "Bhopal Shatabdi Express",
                Direction = TrainDirection.Down,
                TrainType = TrainType.SuperFast,
                RouteId = "R-NDLS-BPL",
                AC1Coaches = 1,
                AC2Coaches = 0,
                AC3Coaches = 4,
                FCCoaches = 1,
                SLCoaches = 0,
                GeneralCoaches = 0
            });
            db.Trains.Add(new Train
            {
                TrainNo = 12311,
                TrainName = "Howrah Kalka Mail",
                Direction = TrainDirection.Up,
                TrainType = TrainType.Mail,
                RouteId = "R-HWH-NDLS",
                AC1Coaches = 1,
                AC2Coaches = 2,
                AC3Coaches = 4,
                FCCoaches = 1,
                SLCoaches = 8,
                GeneralCoaches = 3
            });
            db.SaveChanges();

            // ---------- Day master ----------
            db.TrainRunDays.Add(TrainRunDay.Daily(12951));
            db.TrainRunDays.Add(new TrainRunDay   // Shatabdi: does not run on Sunday
            {
                TrainNo = 12002,
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
                Saturday = true,
                Sunday = false
            });
            db.TrainRunDays.Add(TrainRunDay.Daily(12311));
            db.SaveChanges();

            // ---------- Schedules ----------
            db.TrainSchedules.AddRange(new[]
            {
                Stop(12951, 1, sid("BCT"),  null,      T(17, 0),  0,    0),
                Stop(12951, 2, sid("BRC"),  T(21, 0),  T(21, 10), 392,  0),
                Stop(12951, 3, sid("KOTA"), T(3, 15),  T(3, 25),  920,  1),
                Stop(12951, 4, sid("NDLS"), T(8, 30),  null,      1386, 1),

                Stop(12002, 1, sid("NDLS"), null,      T(6, 0),   0,    0),
                Stop(12002, 2, sid("AGC"),  T(7, 50),  T(7, 55),  195,  0),
                Stop(12002, 3, sid("JHS"),  T(10, 30), T(10, 35), 403,  0),
                Stop(12002, 4, sid("BPL"),  T(14, 40), null,      704,  0),

                Stop(12311, 1, sid("HWH"),  null,      T(19, 40), 0,    0),
                Stop(12311, 2, sid("CNB"),  T(10, 50), T(11, 0),  999,  1),
                Stop(12311, 3, sid("NDLS"), T(17, 30), null,      1451, 1)
            });
            db.SaveChanges();

            // ---------- Fare rules (3 distance slabs per class) ----------
            var fareData = new[]
            {
                new { C = TravelClass.AC1, R1 = 4.50m, R2 = 4.00m, R3 = 3.50m, Min = 500m },
                new { C = TravelClass.AC2, R1 = 2.80m, R2 = 2.50m, R3 = 2.20m, Min = 300m },
                new { C = TravelClass.AC3, R1 = 2.00m, R2 = 1.80m, R3 = 1.60m, Min = 250m },
                new { C = TravelClass.FC,  R1 = 2.40m, R2 = 2.10m, R3 = 1.90m, Min = 280m },
                new { C = TravelClass.SL,  R1 = 0.80m, R2 = 0.70m, R3 = 0.60m, Min = 100m }
            };
            foreach (var f in fareData)
            {
                db.FareRules.Add(new FareRule { TravelClass = f.C, FromKm = 0, ToKm = 500, RatePerKm = f.R1, MinimumFare = f.Min });
                db.FareRules.Add(new FareRule { TravelClass = f.C, FromKm = 501, ToKm = 1500, RatePerKm = f.R2, MinimumFare = f.Min });
                db.FareRules.Add(new FareRule { TravelClass = f.C, FromKm = 1501, ToKm = 5000, RatePerKm = f.R3, MinimumFare = f.Min });
            }

            // ---------- Reservation + super fast fees ----------
            db.ReservationFees.AddRange(new[]
            {
                new ReservationFee { TravelClass = TravelClass.AC1, ReservationCharge = 60, SuperFastCharge = 75 },
                new ReservationFee { TravelClass = TravelClass.AC2, ReservationCharge = 50, SuperFastCharge = 45 },
                new ReservationFee { TravelClass = TravelClass.AC3, ReservationCharge = 40, SuperFastCharge = 45 },
                new ReservationFee { TravelClass = TravelClass.FC,  ReservationCharge = 50, SuperFastCharge = 45 },
                new ReservationFee { TravelClass = TravelClass.SL,  ReservationCharge = 20, SuperFastCharge = 30 }
            });

            // ---------- Cancellation rules (same slabs for each class, different minimum charge) ----------
            var minCharge = new Dictionary<TravelClass, decimal>
            {
                { TravelClass.AC1, 240 }, { TravelClass.AC2, 200 }, { TravelClass.AC3, 180 },
                { TravelClass.FC, 200 },  { TravelClass.SL, 120 }
            };
            foreach (TravelClass c in Enum.GetValues(typeof(TravelClass)))
            {
                db.CancellationRules.Add(new CancellationRule { TravelClass = c, MinHoursBeforeDeparture = 48, DeductionPercent = 10, MinimumCharge = minCharge[c] });
                db.CancellationRules.Add(new CancellationRule { TravelClass = c, MinHoursBeforeDeparture = 12, DeductionPercent = 25, MinimumCharge = minCharge[c] });
                db.CancellationRules.Add(new CancellationRule { TravelClass = c, MinHoursBeforeDeparture = 4, DeductionPercent = 50, MinimumCharge = minCharge[c] });
                db.CancellationRules.Add(new CancellationRule { TravelClass = c, MinHoursBeforeDeparture = 0, DeductionPercent = 100, MinimumCharge = minCharge[c] });
            }

            db.SaveChanges();
            base.Seed(db);
        }

        // Creates a user with a hashed password
        private static AppUser NewUser(string userName, string fullName, UserType type, string password)
        {
            string hash, salt;
            PasswordHasher.CreateHash(password, out hash, out salt);
            return new AppUser
            {
                UserName = userName,
                FullName = fullName,
                UserType = type,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = true,
                MustChangePassword = false,
                CreatedOn = DateTime.Now
            };
        }

        // Shorthand for a schedule row
        private static TrainSchedule Stop(int trainNo, int stopNo, int stationId,
            TimeSpan? arrival, TimeSpan? departure, int km, int dayOffset)
        {
            return new TrainSchedule
            {
                TrainNo = trainNo,
                StopNo = stopNo,
                StationId = stationId,
                ArrivalTime = arrival,
                DepartureTime = departure,
                DistanceFromSource = km,
                DayOffset = dayOffset
            };
        }

        // Shorthand for a time value
        private static TimeSpan T(int hours, int minutes)
        {
            return new TimeSpan(hours, minutes, 0);
        }
    }
}