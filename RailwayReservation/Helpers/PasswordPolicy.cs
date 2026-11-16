namespace RailwayReservation.Helpers
{
    /// <summary>Password must be alphanumeric: 8-20 chars, at least one letter and one digit.</summary>
    public static class PasswordPolicy
    {
        public const string Pattern = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,20}$";
        public const string Message = "Password must be 8-20 characters, alphanumeric only, with at least one letter and one digit.";
    }
}