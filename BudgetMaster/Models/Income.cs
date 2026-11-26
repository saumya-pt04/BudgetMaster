using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetMaster.Models
{
    public class Income
    {
        public int IncomeId { get; set; }
        public int UserId { get; set; }

        [Required(ErrorMessage = "Income source is required")]
        public string Source { get; set; }

        [Required]
        [Range(1, 10000000, ErrorMessage = "Enter a valid amount")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime IncomeDate { get; set; } = DateTime.Today;

        public string Notes { get; set; }
    }
}