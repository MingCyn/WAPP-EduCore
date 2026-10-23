using System;
using System.Configuration;
using System.Web.UI;
using Npgsql;

namespace WAPP_EduCore.pages.Auth
{
    public partial class verifyOtp : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["ResetEmail"] == null)
            {
                Response.Redirect("forgotPassword.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            string enteredOtp = txtOtp.Text.Trim();
            string newPassword = txtNewPassword.Text.Trim();
            string email = Session["ResetEmail"]?.ToString();

            if (string.IsNullOrEmpty(enteredOtp) || string.IsNullOrEmpty(newPassword))
            {
                lblMessage.ForeColor = System.Drawing.Color.Red;
                lblMessage.Text = "Please fill in all fields.";
                return;
            }

            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                // Check if OTP matches and is not expired
                string query = "SELECT reset_otp, otp_expiry FROM public.users WHERE email = @email";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string dbOtp = reader["reset_otp"]?.ToString();
                            DateTime expiry = reader["otp_expiry"] != DBNull.Value ? Convert.ToDateTime(reader["otp_expiry"]) : DateTime.MinValue;

                            reader.Close();

                            if (dbOtp != enteredOtp)
                            {
                                lblMessage.ForeColor = System.Drawing.Color.Red;
                                lblMessage.Text = "Invalid OTP code.";
                                return;
                            }

                            if (DateTime.UtcNow > expiry)
                            {
                                lblMessage.ForeColor = System.Drawing.Color.Red;
                                lblMessage.Text = "OTP has expired. Please request a new one.";
                                return;
                            }
                        }
                        else
                        {
                            lblMessage.ForeColor = System.Drawing.Color.Red;
                            lblMessage.Text = "User session error.";
                            return;
                        }
                    }

                    // Signup and login currently store and compare the password value directly.
                    // Keep the reset flow consistent until a shared password-hashing service is added.
                    string passwordToStore = newPassword;

                    // Update password and clear OTP
                    string updateQuery = "UPDATE public.users SET password_hash = @password, reset_otp = NULL, otp_expiry = NULL WHERE email = @email";
                    using (var updateCmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@password", passwordToStore);
                        updateCmd.Parameters.AddWithValue("@email", email);
                        updateCmd.ExecuteNonQuery();
                    }

                    // Clear session and redirect to login
                    Session.Remove("ResetEmail");
                    Response.Redirect("login.aspx?reset=success", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
        }
    }
}