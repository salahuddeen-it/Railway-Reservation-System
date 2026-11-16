namespace RailwayReservation.Helpers
{
    /// <summary>Role names used in [Authorize(Roles = ...)] – match UserType enum names.</summary>
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Clerk = "Clerk";
        public const string Customer = "Customer";
        public const string AdminOrClerk = "Admin,Clerk";
    }
}