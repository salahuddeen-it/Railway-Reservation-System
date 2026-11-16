using System.ComponentModel.DataAnnotations;
using RailwayReservation.Helpers;

namespace RailwayReservation.ViewModels
{
    /// <summary>Login form data</summary>
    public class LoginViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    /// <summary>Change password form data</summary>
    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required, DataType(DataType.Password)]
        [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.Message)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; }
    }
}