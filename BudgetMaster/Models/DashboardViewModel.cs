using System.Collections.Generic;

namespace BudgetMaster.Models
{
    public class DashboardViewModel
    {
        public string UserName { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetSavings { get; set; }
        public decimal MonthlyBudgetLimit { get; set; }
        public decimal RemainingBudget { get; set; }
        public List<Income> RecentIncomes { get; set; } = new List<Income>();
        public List<Expense> RecentExpenses { get; set; } = new List<Expense>();
        public Dictionary<string, decimal> CategoryBreakdown { get; set; } = new Dictionary<string, decimal>();
    }
}