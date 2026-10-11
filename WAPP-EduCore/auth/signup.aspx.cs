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

            // 1. Gather form input values
            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string dobStr = txtDob.Text.Trim();
            string gender = ddlGender.SelectedValue;
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;
            string role = hfRole.Value; // 'Student' or 'Tutor'

            // 2. Basic validation checks
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                lblErrorMessage.Text = "Please fill in all required fields.";
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
            DateTime? dob = null;
            if (DateTime.TryParse(dobStr, out DateTime parsedDob))
            {
                dob = parsedDob;
            }

            // 3. Insert into Supabase Database via Npgsql
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
                        cmd.Parameters.AddWithValue("@dob", (object)dob ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@gender", string.IsNullOrEmpty(gender) ? (object)DBNull.Value : gender);

                        // Note: For production security, hash the password here (e.g., using BCrypt). 
                        // Storing plain text password for initial testing phase if desired.
                        cmd.Parameters.AddWithValue("@password_hash", password);
                        cmd.Parameters.AddWithValue("@role", role);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Redirect to login or a success page upon successful registration
                Response.Redirect("login.aspx?registered=success", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                // Catch unique constraint violations (e.g. email already exists) or connection errors
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