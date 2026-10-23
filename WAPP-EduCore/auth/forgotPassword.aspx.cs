using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Web.UI;
using Npgsql;

namespace WAPP_EduCore.pages.Auth
{
    public partial class forgotPassword : Page
    {
        protected void btnSendOtp_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                lblMessage.ForeColor = System.Drawing.Color.Red;
                lblMessage.Text = "Please enter your email address.";
                return;
            }

            // 1. Check if email exists in Supabase
            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;
            bool emailExists = false;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                string checkQuery = "SELECT COUNT(1) FROM public.users WHERE email = @email";
                using (var cmd = new NpgsqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    emailExists = (long)cmd.ExecuteScalar() > 0;
                }
            }

            if (!emailExists)
            {
                // For security, you can show a generic message or exact message
                lblMessage.ForeColor = System.Drawing.Color.Red;
                lblMessage.Text = "Email address not found in our system.";
                return;
            }

            // 2. Generate 6-digit random OTP
            string otp = new Random().Next(100000, 999999).ToString();
            DateTime expiry = DateTime.UtcNow.AddMinutes(1); // OTP valid for 1 minutes

            // 3. Save OTP to database
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                string updateQuery = "UPDATE public.users SET reset_otp = @otp, otp_expiry = @expiry WHERE email = @email";
                using (var cmd = new NpgsqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@otp", otp);
                    cmd.Parameters.AddWithValue("@expiry", expiry);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.ExecuteNonQuery();
                }
            }

            // 4. Send Email via SMTP
            try
            {
                string smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
                int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                string smtpUser = ConfigurationManager.AppSettings["SmtpUser"];
                string smtpPass = ConfigurationManager.AppSettings["SmtpPass"];
                string senderEmail = ConfigurationManager.AppSettings["SenderEmail"];

                var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "EduCore Security"),
                    Subject = "Your EduCore Password Reset OTP",
                    Body = $"Hello,\n\nYour OTP code for resetting your password is: {otp}\n\nThis code will expire in 10 minutes.\n\nIf you did not request this, please ignore this email.",
                    IsBodyHtml = false
                };
                mailMessage.To.Add(email);

                client.Send(mailMessage);

                // Force modern TLS security protocols required by Gmail
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                client.Send(mailMessage);

                // Pass email via QueryString to the verification page
                Session["ResetEmail"] = email;
                Response.Redirect("verifyOtp.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                lblMessage.ForeColor = System.Drawing.Color.Red;

                // Get the detailed inner exception message if it exists
                string detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                lblMessage.Text = "Error sending email: " + detailedError;

                // Optional: Log it to Visual Studio Output window for debugging
                System.Diagnostics.Debug.WriteLine("SMTP Error: " + ex.ToString());
            }
        }
    }
}