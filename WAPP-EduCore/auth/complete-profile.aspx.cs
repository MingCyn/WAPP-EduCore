using System;
using System.Configuration;
using System.Web.UI;
using Npgsql;

namespace WAPP_EduCore.auth
{
    public partial class complete_profile : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Ensure the user went through the Google callback first and has a temporary session
                if (Session["UserID"] == null)
                {
                    Response.Redirect("login.aspx?error=session_expired", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
        }

        protected void btnComplete_Click(object sender, EventArgs e)
        {
            lblErrorMessage.Text = "";

            string dobStr = txtDob.Text.Trim();
            string gender = ddlGender.SelectedValue;
            string role = hfRole.Value;
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            if (string.IsNullOrEmpty(dobStr) || string.IsNullOrEmpty(gender))
            {
                lblErrorMessage.Text = "Please fill in your Date of Birth and Gender.";
                return;
            }

            if (!DateTime.TryParse(dobStr, out DateTime parsedDob))
            {
                lblErrorMessage.Text = "Invalid Date of Birth format.";
                return;
            }

            // If they typed a password, validate it
            if (!string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(confirmPassword))
            {
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
            }

            int userId = Convert.ToInt32(Session["UserID"]);
            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;

            // Build query depending on whether they provided a password
            string updateQuery = string.IsNullOrEmpty(password)
                ? "UPDATE public.users SET dob = @dob, gender = @gender, role = @role WHERE user_id = @user_id;"
                : "UPDATE public.users SET dob = @dob, gender = @gender, role = @role, password_hash = @password WHERE user_id = @user_id;";

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("dob", parsedDob);
                        cmd.Parameters.AddWithValue("gender", gender);
                        cmd.Parameters.AddWithValue("role", role);
                        cmd.Parameters.AddWithValue("user_id", userId);

                        if (!string.IsNullOrEmpty(password))
                        {
                            cmd.Parameters.AddWithValue("password", password); // (Note: Hash this if your standard signup hashes passwords!)
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                Session["UserRole"] = role;

                // Route to appropriate dashboard
                string target = role.Equals("Tutor", StringComparison.OrdinalIgnoreCase)
                    ? "../pages/Tutor/tutorDashboard.aspx"
                    : "../pages/Student/studentDashboard.aspx";

                Response.Redirect(target, false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                lblErrorMessage.Text = "Failed to update profile: " + ex.Message;
            }
        }
    }
}