using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetMaster.Models
{
    public class Expense
    {
        public int ExpenseId { get; set; }
        public int UserId { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Expense title is required")]
        public string Title { get; set; }

        [Required]
        [Range(1, 10000000, ErrorMessage = "Enter a valid amount")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        public string Notes { get; set; }
    }
}