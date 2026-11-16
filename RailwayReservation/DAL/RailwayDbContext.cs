using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using RailwayReservation.Models;

namespace RailwayReservation.DAL
{
    /// <summary>Entity Framework 6 database context (Code First).</summary>
    public class RailwayDbContext : DbContext
    {
        // "RailwayDb" = connection string name in Web.config
        public RailwayDbContext() : base("name=RailwayDb")
        {
        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<Train> Trains { get; set; }
        public DbSet<TrainRunDay> TrainRunDays { get; set; }
        public DbSet<TrainSchedule> TrainSchedules { get; set; }
        public DbSet<FareRule> FareRules { get; set; }
        public DbSet<CancellationRule> CancellationRules { get; set; }
        public DbSet<ReservationFee> ReservationFees { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<CashTransaction> CashTransactions { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Singular table names (Station, Train ...)
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // No cascade deletes – protects transaction history and avoids multiple cascade paths
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            // All money columns = decimal(12,2)
            modelBuilder.Properties<decimal>().Configure(p => p.HasPrecision(12, 2));

            // One-to-one: Train <-> TrainRunDay (TrainRunDay.TrainNo is PK + FK)
            modelBuilder.Entity<TrainRunDay>()
                .HasRequired(d => d.Train)
                .WithOptional(t => t.RunDay);

            // Reservation has two FKs to Station – configure explicitly
            modelBuilder.Entity<Reservation>()
                .HasRequired(r => r.FromStation).WithMany()
                .HasForeignKey(r => r.FromStationId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Reservation>()
                .HasRequired(r => r.ToStation).WithMany()
                .HasForeignKey(r => r.ToStationId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Reservation>()
                .HasRequired(r => r.BookedBy).WithMany()
                .HasForeignKey(r => r.BookedByUserId).WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}