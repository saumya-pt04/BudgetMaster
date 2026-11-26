using System.ComponentModel.DataAnnotations;

namespace BudgetMaster.Models
{
    public class Budget
    {
        public int BudgetId { get; set; }
        public int UserId { get; set; }

        [Required]
        public string MonthYear { get; set; }

        [Required]
        [Range(1, 100000000)]
        public decimal MonthlyLimit { get; set; }
    }
}