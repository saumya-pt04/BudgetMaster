using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using BudgetMaster.Models;

namespace BudgetMaster.Controllers
{
    public class BudgetController : Controller
    {
        private string cs = ConfigurationManager.ConnectionStrings["BudgetDB"].ConnectionString;

        private bool IsAuthenticated() => Session["UserId"] != null;
        private int CurrentUserId => Convert.ToInt32(Session["UserId"]);

        // 1. DASHBOARD
        public ActionResult Index()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

            var model = new DashboardViewModel
            {
                UserName = Session["FullName"].ToString()
            };
            string currentMonth = DateTime.Now.ToString("yyyy-MM");

            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();

                SqlCommand incCmd = new SqlCommand("SELECT ISNULL(SUM(Amount),0) FROM Income WHERE UserId=@UId", conn);
                incCmd.Parameters.AddWithValue("@UId", CurrentUserId);
                model.TotalIncome = Convert.ToDecimal(incCmd.ExecuteScalar());

                SqlCommand expCmd = new SqlCommand("SELECT ISNULL(SUM(Amount),0) FROM Expenses WHERE UserId=@UId", conn);
                expCmd.Parameters.AddWithValue("@UId", CurrentUserId);
                model.TotalExpenses = Convert.ToDecimal(expCmd.ExecuteScalar());

                model.NetSavings = model.TotalIncome - model.TotalExpenses;

                SqlCommand budCmd = new SqlCommand("SELECT ISNULL(MonthlyLimit,0) FROM Budget WHERE UserId=@UId AND MonthYear=@MY", conn);
                budCmd.Parameters.AddWithValue("@UId", CurrentUserId);
                budCmd.Parameters.AddWithValue("@MY", currentMonth);
                object limitObj = budCmd.ExecuteScalar();
                model.MonthlyBudgetLimit = limitObj != null ? Convert.ToDecimal(limitObj) : 0;

                SqlCommand mExpCmd = new SqlCommand("SELECT ISNULL(SUM(Amount),0) FROM Expenses WHERE UserId=@UId AND FORMAT(ExpenseDate,'yyyy-MM')=@MY", conn);
                mExpCmd.Parameters.AddWithValue("@UId", CurrentUserId);
                mExpCmd.Parameters.AddWithValue("@MY", currentMonth);
                decimal monthSpent = Convert.ToDecimal(mExpCmd.ExecuteScalar());
                model.RemainingBudget = model.MonthlyBudgetLimit - monthSpent;

                SqlCommand recExpCmd = new SqlCommand(@"SELECT TOP 5 e.*, c.CategoryName FROM Expenses e 
                                                       JOIN Categories c ON e.CategoryId = c.CategoryId 
                                                       WHERE e.UserId=@UId ORDER BY e.ExpenseDate DESC", conn);
                recExpCmd.Parameters.AddWithValue("@UId", CurrentUserId);
                using (SqlDataReader dr = recExpCmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        model.RecentExpenses.Add(new Expense
                        {
                            ExpenseId = Convert.ToInt32(dr["ExpenseId"]),
                            Title = dr["Title"].ToString(),
                            CategoryName = dr["CategoryName"].ToString(),
                            Amount = Convert.ToDecimal(dr["Amount"]),
                            ExpenseDate = Convert.ToDateTime(dr["ExpenseDate"])
                        });
                    }
                }
            }
            return View(model);
        }

        // 2. ADD INCOME
        [HttpGet]
        public ActionResult AddIncome()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            return View(new Income());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddIncome(Income inc)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(inc);

            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "INSERT INTO Income (UserId, Source, Amount, IncomeDate, Notes) VALUES (@UId, @Source, @Amount, @Date, @Notes)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UId", CurrentUserId);
                cmd.Parameters.AddWithValue("@Source", inc.Source);
                cmd.Parameters.AddWithValue("@Amount", inc.Amount);
                cmd.Parameters.AddWithValue("@Date", inc.IncomeDate);
                cmd.Parameters.AddWithValue("@Notes", (object)inc.Notes ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["Success"] = "Income added successfully!";
            return RedirectToAction("Index");
        }

        // 3. ADD EXPENSE
        [HttpGet]
        public ActionResult AddExpense()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            ViewBag.Categories = GetCategories();
            return View(new Expense());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddExpense(Expense exp)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = GetCategories();
                return View(exp);
            }

            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "INSERT INTO Expenses (UserId, CategoryId, Title, Amount, ExpenseDate, Notes) VALUES (@UId, @CatId, @Title, @Amount, @Date, @Notes)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UId", CurrentUserId);
                cmd.Parameters.AddWithValue("@CatId", exp.CategoryId);
                cmd.Parameters.AddWithValue("@Title", exp.Title);
                cmd.Parameters.AddWithValue("@Amount", exp.Amount);
                cmd.Parameters.AddWithValue("@Date", exp.ExpenseDate);
                cmd.Parameters.AddWithValue("@Notes", (object)exp.Notes ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["Success"] = "Expense recorded successfully!";
            return RedirectToAction("Index");
        }

        // 4. CREATE MONTHLY BUDGET
        [HttpGet]
        public ActionResult SetBudget()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            return View(new Budget { MonthYear = DateTime.Now.ToString("yyyy-MM") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetBudget(Budget budget)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(budget);

            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();
                SqlCommand check = new SqlCommand("SELECT COUNT(*) FROM Budget WHERE UserId=@UId AND MonthYear=@MY", conn);
                check.Parameters.AddWithValue("@UId", CurrentUserId);
                check.Parameters.AddWithValue("@MY", budget.MonthYear);
                int count = (int)check.ExecuteScalar();

                string query = count > 0
                    ? "UPDATE Budget SET MonthlyLimit=@Limit WHERE UserId=@UId AND MonthYear=@MY"
                    : "INSERT INTO Budget (UserId, MonthYear, MonthlyLimit) VALUES (@UId, @MY, @Limit)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UId", CurrentUserId);
                cmd.Parameters.AddWithValue("@MY", budget.MonthYear);
                cmd.Parameters.AddWithValue("@Limit", budget.MonthlyLimit);
                cmd.ExecuteNonQuery();
            }
            TempData["Success"] = "Monthly budget saved!";
            return RedirectToAction("Index");
        }

        // 5. VIEW TRANSACTION HISTORY + SEARCH & FILTER
        public ActionResult Transactions(string search, int? categoryId, string month)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

            ViewBag.Categories = GetCategories();
            ViewBag.Search = search;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedMonth = month;

            var expenses = new List<Expense>();
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"SELECT e.*, c.CategoryName FROM Expenses e 
                                 JOIN Categories c ON e.CategoryId = c.CategoryId 
                                 WHERE e.UserId = @UId";

                if (!string.IsNullOrWhiteSpace(search))
                    query += " AND (e.Title LIKE @Search OR e.Notes LIKE @Search)";
                if (categoryId.HasValue)
                    query += " AND e.CategoryId = @CatId";
                if (!string.IsNullOrWhiteSpace(month))
                    query += " AND FORMAT(e.ExpenseDate, 'yyyy-MM') = @Month";

                query += " ORDER BY e.ExpenseDate DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UId", CurrentUserId);
                if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                if (categoryId.HasValue) cmd.Parameters.AddWithValue("@CatId", categoryId.Value);
                if (!string.IsNullOrWhiteSpace(month)) cmd.Parameters.AddWithValue("@Month", month);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    expenses.Add(new Expense
                    {
                        ExpenseId = Convert.ToInt32(dr["ExpenseId"]),
                        Title = dr["Title"].ToString(),
                        CategoryName = dr["CategoryName"].ToString(),
                        Amount = Convert.ToDecimal(dr["Amount"]),
                        ExpenseDate = Convert.ToDateTime(dr["ExpenseDate"]),
                        Notes = dr["Notes"].ToString()
                    });
                }
            }
            return View(expenses);
        }

        // 6. MONTHLY REPORTS
        public ActionResult Reports(string month)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            string selectedMonth = string.IsNullOrEmpty(month) ? DateTime.Now.ToString("yyyy-MM") : month;
            ViewBag.Month = selectedMonth;

            var model = new DashboardViewModel();
            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();
                SqlCommand incCmd = new SqlCommand("SELECT ISNULL(SUM(Amount),0) FROM Income WHERE UserId=@UId AND FORMAT(IncomeDate,'yyyy-MM')=@MY", conn);
                incCmd.Parameters.AddWithValue("@UId", CurrentUserId);
                incCmd.Parameters.AddWithValue("@MY", selectedMonth);
                model.TotalIncome = Convert.ToDecimal(incCmd.ExecuteScalar());

                SqlCommand expCmd = new SqlCommand("SELECT ISNULL(SUM(Amount),0) FROM Expenses WHERE UserId=@UId AND FORMAT(ExpenseDate,'yyyy-MM')=@MY", conn);
                expCmd.Parameters.AddWithValue("@UId", CurrentUserId);
                expCmd.Parameters.AddWithValue("@MY", selectedMonth);
                model.TotalExpenses = Convert.ToDecimal(expCmd.ExecuteScalar());

                model.NetSavings = model.TotalIncome - model.TotalExpenses;

                SqlCommand catCmd = new SqlCommand(@"SELECT c.CategoryName, SUM(e.Amount) AS Total 
                                                     FROM Expenses e JOIN Categories c ON e.CategoryId = c.CategoryId 
                                                     WHERE e.UserId=@UId AND FORMAT(e.ExpenseDate,'yyyy-MM')=@MY 
                                                     GROUP BY c.CategoryName", conn);
                catCmd.Parameters.AddWithValue("@UId", CurrentUserId);
                catCmd.Parameters.AddWithValue("@MY", selectedMonth);
                using (SqlDataReader dr = catCmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        model.CategoryBreakdown[dr["CategoryName"].ToString()] = Convert.ToDecimal(dr["Total"]);
                    }
                }
            }
            return View(model);
        }

        private List<Category> GetCategories()
        {
            var list = new List<Category>();
            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Categories", conn);
                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new Category
                    {
                        CategoryId = Convert.ToInt32(dr["CategoryId"]),
                        CategoryName = dr["CategoryName"].ToString()
                    });
                }
            }
            return list;
        }
    }
}