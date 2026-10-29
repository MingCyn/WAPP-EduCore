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
                // 1. Exchange authorization code for access token
                string tokenJson = ExchangeCodeForToken(code);
                var tokenData = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(tokenJson);

                if (!tokenData.ContainsKey("access_token"))
                {
                    Response.Redirect("login.aspx?error=token_failed", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                string accessToken = tokenData["access_token"].ToString();

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

                // 3. Check or Insert User in Database
                ProcessUserLoginOrRegistration(email, fullName);
            }
            catch (Exception ex)
            {
                // Temporarily output error to see what's failing
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
        private void ProcessUserLoginOrRegistration(string email, string fullName)
        {
            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                // 1. Check if user already exists
                string checkQuery = "SELECT user_id FROM public.users WHERE email = @email";
                using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("email", email);
                    object existingUser = checkCmd.ExecuteScalar();

                    if (existingUser != null && existingUser != DBNull.Value)
                    {
                        // User already exists, send them back to login to sign in normally
                        Response.Redirect("login.aspx?registered=google_success", false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;
                    }
                }

                // Inside ProcessUserLoginOrRegistration for a new user:
                string insertQuery = @"INSERT INTO public.users (full_name, email, role, password_hash, dob, gender) 
                       VALUES (@full_name, @email, 'Student', 'GOOGLE_AUTH_USER', '2000-01-01', 'Other') 
                       RETURNING user_id;";

                using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("full_name", fullName);
                    insertCmd.Parameters.AddWithValue("email", email);

                    object newUserId = insertCmd.ExecuteScalar();
                    if (newUserId != null)
                    {
                        Session["UserID"] = newUserId;
                        Session["UserRole"] = "Student";

                        // Redirect to the profile completion screen!
                        Response.Redirect("complete-profile.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                    }
                }
            }
        }
    }
}