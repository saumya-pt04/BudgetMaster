using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetMaster.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Minimum 6 characters required")]
        public string Password { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}