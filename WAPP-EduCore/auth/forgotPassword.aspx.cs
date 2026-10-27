using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Web;
using System.Web.UI;
using Npgsql;

namespace WAPP_EduCore.pages.Auth
{
    public partial class forgotPassword : Page
    {
        private const string ConnectionName = "SupabasePostgres";

        // How long the code works. The email text uses the same number, so they can never disagree.
        private const int OtpValidMinutes = 10;

        // While developing, the technical reason is shown under the button. Set to false before you deploy.
        private const bool ShowTechnicalErrors = true;

        protected void btnSendOtp_Click(object sender, EventArgs e)
        {
            // ------------------------------------------------------------------
            // 1. The email the user typed
            // ------------------------------------------------------------------
            string typed = txtEmail.Text.Trim();
            if (typed.Length == 0)
            {
                ShowError("Please enter your email address.");
                return;
            }

            MailAddress toAddress;
            if (!TryParseAddress(typed, null, out toAddress))
            {
                ShowError("Please enter a valid email address.");
                return;
            }
            string email = toAddress.Address;

            // ------------------------------------------------------------------
            // 2. The mail settings in Web.config. Checked BEFORE anything is saved, and each one is
            //    named in the message, so a bad value is easy to find.
            // ------------------------------------------------------------------
            string smtpHost = (ConfigurationManager.AppSettings["SmtpHost"] ?? "").Trim();
            string smtpUser = ConfigurationManager.AppSettings["SmtpUser"] ?? "";
            string smtpPass = ConfigurationManager.AppSettings["SmtpPass"] ?? "";
            string senderText = (ConfigurationManager.AppSettings["SenderEmail"] ?? "").Trim();

            int smtpPort;
            if (smtpHost.Length == 0 || !int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out smtpPort))
            {
                ShowError("The email settings are incomplete: check SmtpHost and SmtpPort in Web.config.");
                return;
            }

            MailAddress fromAddress;
            if (!TryParseAddress(senderText, "EduCore Security", out fromAddress))
            {
                ShowError("The SenderEmail setting in Web.config is not a valid email address" +
                          (ShowTechnicalErrors ? " (current value: [" + senderText + "])" : "") +
                          ". Use only the plain address, for example name@example.com: no quotes, spaces, display name, " +
                          "semicolon, or a dot at the start or end.");
                return;
            }

            // ------------------------------------------------------------------
            // 3. Does this email belong to an account?
            // ------------------------------------------------------------------
            string connString = ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM public.users WHERE LOWER(email) = LOWER(@email)", conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    if (Convert.ToInt64(cmd.ExecuteScalar()) == 0)
                    {
                        ShowError("Email address not found in our system.");
                        return;
                    }
                }

                // --------------------------------------------------------------
                // 4. Make a 6-digit code and save it with its expiry time (UTC)
                // --------------------------------------------------------------
                string otp = GenerateOtp();
                DateTime expiry = DateTime.UtcNow.AddMinutes(OtpValidMinutes);

                using (var cmd = new NpgsqlCommand(
                    "UPDATE public.users SET reset_otp = @otp, otp_expiry = @expiry WHERE LOWER(email) = LOWER(@email)", conn))
                {
                    cmd.Parameters.AddWithValue("@otp", otp);
                    cmd.Parameters.AddWithValue("@expiry", expiry);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.ExecuteNonQuery();
                }

                // --------------------------------------------------------------
                // 5. Send the email - once
                // --------------------------------------------------------------
                try
                {
                    // Gmail needs TLS 1.2. Set it BEFORE the client connects.
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                    using (var client = new SmtpClient(smtpHost, smtpPort))
                    using (var mail = new MailMessage())
                    {
                        client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                        client.EnableSsl = true;

                        mail.From = fromAddress;
                        mail.To.Add(toAddress);
                        mail.Subject = "Your EduCore Password Reset OTP";
                        mail.IsBodyHtml = false;
                        mail.Body = "Hello,\n\nYour OTP code for resetting your password is: " + otp +
                                    "\n\nThis code will expire in " + OtpValidMinutes + " minutes." +
                                    "\n\nIf you did not request this, please ignore this email.";

                        client.Send(mail);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("SMTP Error: " + ex);
                    string detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ShowError("We couldn't send the email." + (ShowTechnicalErrors ? " (" + detail + ")" : " Please try again."));
                    return;
                }
            }

            // Pass the email to the verification page through the session
            Session["ResetEmail"] = email;
            Response.Redirect("verifyOtp.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        // Accepts only a plain address such as name@example.com (not "Name <name@example.com>").
        private static bool TryParseAddress(string text, string displayName, out MailAddress address)
        {
            address = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            try
            {
                MailAddress parsed = displayName == null ? new MailAddress(text) : new MailAddress(text, displayName);
                if (!parsed.Address.Equals(text, StringComparison.OrdinalIgnoreCase)) return false;
                address = parsed;
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        // A random 6-digit code from a cryptographic generator (System.Random can be guessed)
        private static string GenerateOtp()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                uint value = BitConverter.ToUInt32(bytes, 0);
                return (100000 + value % 900000).ToString();
            }
        }

        private void ShowError(string message)
        {
            lblMessage.ForeColor = System.Drawing.Color.Red;
            lblMessage.Text = HttpUtility.HtmlEncode(message);
        }
    }
}