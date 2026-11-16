namespace RailwayReservation.ViewModels
{
    /// <summary>Numbers shown on the home dashboard</summary>
    public class DashboardViewModel
    {
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsStaff { get; set; }

        // Staff (Admin/Clerk) figures
        public int TotalTrains { get; set; }
        public int TotalStations { get; set; }
        public int TodaysBookings { get; set; }
        public decimal TodaysReceipts { get; set; }
        public decimal TodaysRefunds { get; set; }
        public int WaitlistedPassengers { get; set; }

        // Customer figure
        public int MyUpcomingTrips { get; set; }
    }
}