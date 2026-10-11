using System;
using System.Configuration;
using System.Web.UI;
using Npgsql;

namespace WAPP_EduCore.auth
{
    public partial class signup : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Page load logic if needed
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
            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;

            string query = @"INSERT INTO public.users (full_name, email, dob, gender, password_hash, role) 
                     VALUES (@full_name, @email, @dob, @gender, @password_hash, @role)";

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@full_name", fullName);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@dob", parsedDob); // Guaranteed not null due to validation above
                        cmd.Parameters.AddWithValue("@gender", gender); // Guaranteed not null
                        cmd.Parameters.AddWithValue("@password_hash", password);
                        cmd.Parameters.AddWithValue("@role", string.IsNullOrEmpty(role) ? "Student" : role);

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