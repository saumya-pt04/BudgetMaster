using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using BudgetMaster.Models;

namespace BudgetMaster.Controllers
{
    public class AccountController : Controller
    {
        private string cs = ConfigurationManager.ConnectionStrings["BudgetDB"].ConnectionString;

        [HttpGet]
        public ActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(User user)
        {
            if (!ModelState.IsValid) return View(user);

            try
            {
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                    SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@Email", user.Email);
                    conn.Open();
                    int exists = (int)checkCmd.ExecuteScalar();
                    if (exists > 0)
                    {
                        ViewBag.Error = "Email is already registered.";
                        return View(user);
                    }

                    string query = "INSERT INTO Users (FullName, Email, Password) VALUES (@FullName, @Email, @Password)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@FullName", user.FullName);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", user.Password);
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Database error: " + ex.Message;
                return View(user);
            }
        }

        [HttpGet]
        public ActionResult Login() => View();

        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    string query = "SELECT UserId, FullName, Email FROM Users WHERE Email = @Email AND Password = @Password";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        Session["UserId"] = dr["UserId"];
                        Session["FullName"] = dr["FullName"];
                        Session["Email"] = dr["Email"];
                        return RedirectToAction("Index", "Budget");
                    }
                }
                ViewBag.Error = "Invalid email or password.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Database error: " + ex.Message;
                return View();
            }
        }

        public ActionResult Profile()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login");
            int userId = Convert.ToInt32(Session["UserId"]);
            User user = new User();

            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "SELECT * FROM Users WHERE UserId = @UserId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    user.UserId = Convert.ToInt32(dr["UserId"]);
                    user.FullName = dr["FullName"].ToString();
                    user.Email = dr["Email"].ToString();
                    user.CreatedAt = Convert.ToDateTime(dr["CreatedAt"]);
                }
            }
            return View(user);
        }

        [HttpPost]
        public ActionResult UpdateProfile(string fullName)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login");
            int userId = Convert.ToInt32(Session["UserId"]);

            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "UPDATE Users SET FullName = @FullName WHERE UserId = @UserId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            Session["FullName"] = fullName;
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}