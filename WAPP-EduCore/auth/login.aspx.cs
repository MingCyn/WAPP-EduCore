using System;
using System.Configuration;
using System.Web.UI;
using Npgsql;

namespace WAPP_EduCore.auth
{
    public partial class login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Request.QueryString["registered"] == "success")
            {
                lblErrorMessage.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129); // Green
                lblErrorMessage.Text = "Account created successfully! Please sign in.";
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            lblErrorMessage.Text = "";

            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                lblErrorMessage.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68);
                lblErrorMessage.Text = "Please enter both email and password.";
                return;
            }

            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;
            string query = "SELECT user_id, full_name, password_hash, role FROM public.users WHERE email = @email";

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedPassword = reader["password_hash"]?.ToString();
                                string role = reader["role"]?.ToString()?.Trim();
                                int userId = Convert.ToInt32(reader["user_id"]);
                                string fullName = reader["full_name"]?.ToString();

                                if (storedPassword == password)
                                {
                                    // Save User Session Details
                                    Session["UserID"] = userId;
                                    Session["UserEmail"] = email;
                                    Session["UserFullName"] = fullName;
                                    Session["UserRole"] = role;

                                    // Role-based Redirection with correct path tracking out of /auth/
                                    // 4. Role-based Redirection with correct folder paths
                                    if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Response.Redirect("../pages/Admin/adminDashboard.aspx", false);
                                    }
                                    else if (role.Equals("Tutor", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Response.Redirect("../pages/Tutor/tutorDashboard.aspx", false);
                                    }
                                    else if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Response.Redirect("../pages/Student/studentDashboard.aspx", false);
                                    }
                                    else
                                    {
                                        // Default fallback
                                        Response.Redirect("../pages/Student/studentDashboard.aspx", false);
                                    }

                                    Context.ApplicationInstance.CompleteRequest();
                                }
                                else
                                {
                                    lblErrorMessage.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68);
                                    lblErrorMessage.Text = "Invalid email or password.";
                                }
                            }
                            else
                            {
                                lblErrorMessage.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68);
                                lblErrorMessage.Text = "Invalid email or password.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblErrorMessage.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68);
                lblErrorMessage.Text = "Login error: " + ex.Message;
            }
        }
    }
}