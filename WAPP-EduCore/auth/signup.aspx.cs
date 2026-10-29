using System;
using System.Configuration;
using System.Web.UI;
using Npgsql;

namespace WAPP_EduCore.auth
{
    public partial class signup : Page
    {
        private const string ConnectionName = "SupabasePostgres";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadPlatformStatistics();
            }
        }

        private void LoadPlatformStatistics()
        {
            string connString = ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    // 1. Get total number of active/registered students
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE LOWER(role) = 'student'", conn))
                    {
                        object studentCount = cmd.ExecuteScalar();
                        if (studentCount != null && studentCount != DBNull.Value)
                        {
                            LitActiveStudents.Text = Convert.ToInt32(studentCount).ToString("N0") + "+";
                        }
                    }

                    // 2. Get total number of courses (Covered Topics)
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM courses", conn))
                    {
                        object courseCount = cmd.ExecuteScalar();
                        if (courseCount != null && courseCount != DBNull.Value)
                        {
                            LitCoveredTopics.Text = Convert.ToInt32(courseCount).ToString("N0") + "+";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback graceful values if database stats cannot be read immediately
                System.Diagnostics.Debug.WriteLine("Failed to load statistics: " + ex.Message);
                LitActiveStudents.Text = "12,480+";
                LitCoveredTopics.Text = "50+";
            }
        }

        protected void btnSignUp_Click(object sender, EventArgs e)
        {
            lblErrorMessage.Text = "";

            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string dobStr = txtDob.Text.Trim();
            string gender = ddlGender.SelectedValue;
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;
            string role = hfRole.Value;

            // 1. Mandatory Field Validation
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(dobStr) || string.IsNullOrEmpty(gender))
            {
                lblErrorMessage.Text = "Please fill in all required fields, including Date of Birth and Gender.";
                return;
            }

            if (password != confirmPassword)
            {
                lblErrorMessage.Text = "Passwords do not match.";
                return;
            }

            if (password.Length < 8)
            {
                lblErrorMessage.Text = "Password must be at least 8 characters long.";
                return;
            }

            // Parse Date of Birth safely
            if (!DateTime.TryParse(dobStr, out DateTime parsedDob))
            {
                lblErrorMessage.Text = "Invalid Date of Birth format.";
                return;
            }

            // 2. Insert into Supabase Database
            string connString = ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString;

            string query = @"INSERT INTO public.users (full_name, email, dob, gender, password_hash, role) 
                     VALUES (@full_name, @email, @dob, @gender, @password_hash, @role)";

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("full_name", fullName);
                        cmd.Parameters.AddWithValue("email", email);
                        cmd.Parameters.AddWithValue("dob", parsedDob);
                        cmd.Parameters.AddWithValue("gender", gender);
                        cmd.Parameters.AddWithValue("password_hash", password);
                        cmd.Parameters.AddWithValue("role", string.IsNullOrEmpty(role) ? "Student" : role);

                        cmd.ExecuteNonQuery();
                    }
                }

                Response.Redirect("login.aspx?registered=success", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("users_email_key"))
                {
                    lblErrorMessage.Text = "This email address is already registered.";
                }
                else
                {
                    lblErrorMessage.Text = "Registration failed: " + ex.Message;
                }
            }
        }
    }
}