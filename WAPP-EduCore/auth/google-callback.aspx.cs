using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using Npgsql;

namespace WAPP_EduCore.auth
{
    public partial class google_callback : System.Web.UI.Page
    {
        private readonly string ClientId = ConfigurationManager.AppSettings["GoogleClientId"];
        private readonly string ClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];
        private const string RedirectUri = "https://localhost:44383/auth/google-callback.aspx";

        protected void Page_Load(object sender, EventArgs e)
        {
            string code = Request.QueryString["code"];
            if (string.IsNullOrEmpty(code))
            {
                Response.Redirect("login.aspx?error=google_failed", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            try
            {
                // 1. Exchange authorization code for access token & refresh token
                string tokenJson = ExchangeCodeForToken(code);
                var tokenData = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(tokenJson);

                if (!tokenData.ContainsKey("access_token"))
                {
                    Response.Redirect("login.aspx?error=token_failed", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                string accessToken = tokenData["access_token"].ToString();

                // Google only sends a refresh_token on the first prompt or when offline access is forced
                string refreshToken = tokenData.ContainsKey("refresh_token") ? tokenData["refresh_token"].ToString() : null;

                // 2. Fetch User Profile Info from Google
                string userInfoJson = GetGoogleUserInfo(accessToken);
                var userInfo = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(userInfoJson);

                string email = userInfo.ContainsKey("email") ? userInfo["email"].ToString() : "";
                string fullName = userInfo.ContainsKey("name") ? userInfo["name"].ToString() : "Google User";

                if (string.IsNullOrEmpty(email))
                {
                    Response.Redirect("login.aspx?error=email_missing", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // 3. Check or Insert User in Database and save Google Tokens
                ProcessUserLoginOrRegistration(email, fullName, accessToken, refreshToken);
            }
            catch (Exception ex)
            {
                // Output error to see what's failing during OAuth
                Response.Write("OAuth Error: " + ex.Message);
                Response.End();
            }
        }

        private string ExchangeCodeForToken(string code)
        {
            string url = "https://oauth2.googleapis.com/token";
            string postData = $"code={code}&client_id={ClientId}&client_secret={ClientSecret}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&grant_type=authorization_code";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            using (var writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(postData);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private string GetGoogleUserInfo(string accessToken)
        {
            string url = "https://www.googleapis.com/oauth2/v2/userinfo?access_token=" + accessToken;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private void ProcessUserLoginOrRegistration(string email, string fullName, string accessToken, string refreshToken)
        {
            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                // 1. Check if user already exists
                string checkQuery = "SELECT user_id, role, dob FROM public.users WHERE email = @email";
                long existingUserId = 0;
                string userRole = "Student";
                object existingDob = null;

                using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("email", email);
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            existingUserId = reader.GetInt64(0);
                            userRole = reader.IsDBNull(1) ? "Student" : reader.GetString(1);
                            existingDob = reader.IsDBNull(2) ? null : reader.GetValue(2);
                        }
                    }
                }

                if (existingUserId > 0)
                {
                    // User exists: Update tokens (preserve old refresh token if Google didn't send a new one)
                    string updateTokensQuery = refreshToken != null
                        ? "UPDATE public.users SET google_access_token = @token, google_refresh_token = @rtoken WHERE user_id = @uid"
                        : "UPDATE public.users SET google_access_token = @token WHERE user_id = @uid";

                    using (var updateCmd = new NpgsqlCommand(updateTokensQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("token", accessToken);
                        if (refreshToken != null)
                        {
                            updateCmd.Parameters.AddWithValue("rtoken", refreshToken);
                        }
                        updateCmd.Parameters.AddWithValue("uid", existingUserId);
                        updateCmd.ExecuteNonQuery();
                    }

                    // Set up active session for the existing user
                    Session["UserID"] = existingUserId;
                    Session["UserRole"] = userRole;

                    // Check if they completed their profile before
                    bool isProfileIncomplete = (existingDob == null || existingDob.ToString().Contains("2000-01-01"));

                    string targetUrl = isProfileIncomplete
                        ? "complete-profile.aspx"
                        : (userRole.Equals("Tutor", StringComparison.OrdinalIgnoreCase)
                            ? "../pages/Tutor/tutorDashboard.aspx"
                            : "../pages/Student/studentDashboard.aspx");

                    Response.Redirect(targetUrl, false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // 2. Brand new user: Insert record including Google tokens
                string insertQuery = @"INSERT INTO public.users (full_name, email, role, password_hash, dob, gender, google_access_token, google_refresh_token) 
                                       VALUES (@full_name, @email, 'Student', 'GOOGLE_AUTH_USER', '2000-01-01', 'Other', @token, @rtoken) 
                                       RETURNING user_id;";

                using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("full_name", fullName);
                    insertCmd.Parameters.AddWithValue("email", email);
                    insertCmd.Parameters.AddWithValue("token", accessToken);
                    insertCmd.Parameters.AddWithValue("rtoken", (object)refreshToken ?? DBNull.Value);

                    object newUserId = insertCmd.ExecuteScalar();
                    if (newUserId != null)
                    {
                        Session["UserID"] = newUserId;
                        Session["UserRole"] = "Student";

                        // Redirect to profile setup for new users
                        Response.Redirect("complete-profile.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                    }
                }
            }
        }
    }
}