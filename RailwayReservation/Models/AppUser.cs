using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwayReservation.Models
{
    /// <summary>
    /// User master – each user is identified by user name and user type.
    /// Passwords are stored only as PBKDF2 hash + salt (never plain text).
    /// </summary>
    public class AppUser
    {
        [Key]
        public int UserId { get; set; }

        [Required, StringLength(50)]
        [Index(IsUnique = true)]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "User Type")]
        public UserType UserType { get; set; }

        [Required, StringLength(200)]
        public string PasswordHash { get; set; }

        [Required, StringLength(100)]
        public string PasswordSalt { get; set; }

        // True when admin created the user with a system default password
        [Display(Name = "Must Change Password")]
        public bool MustChangePassword { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        // Brute-force protection: lock account after 5 wrong attempts
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutEndUtc { get; set; }

        [Display(Name = "Created On")]
        public DateTime CreatedOn { get; set; }

        [Display(Name = "Last Login")]
        public DateTime? LastLoginOn { get; set; }
    }
}