using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Npgsql;

namespace WAPP_EduCore.Data
{
    // =====================================================
    //  LoginKeeper : keeps the ADMIN logged in as the SAME admin until he/she clicks Logout.
    //
    //  The problem: the "session" (the website's memory of who is logged in) is forgotten
    //  after about 20 minutes of no clicking, so the page did not know the admin any more.
    //
    //  The idea: while an admin is logged in we also keep a small "remember me" cookie in the browser.
    //  It holds the admin's number + a secret signature (so nobody can fake it).
    //  If the session is forgotten, the cookie tells us which admin it is, and we fill the session again.
    //  Only the admin pages use this file. Logout removes the cookie.
    // =====================================================
    public static class LoginKeeper
    {
        private const string CookieName = "EduCoreAdminLogin";

        // Saves the cookie. The admin pages call it when an admin is logged in.
        public static void Remember(HttpContext context, int userId)
        {
            if (userId <= 0) return;

            string value = userId + "|" + Sign(userId.ToString());

            // the browser already has this exact cookie? Then there is nothing to do.
            HttpCookie old = context.Request.Cookies[CookieName];
            if (old != null && old.Value == value) return;

            HttpCookie cookie = new HttpCookie(CookieName, value);
            cookie.HttpOnly = true;                                // JavaScript cannot read it
            cookie.Secure = context.Request.IsSecureConnection;    // only over https when the site uses https
            cookie.SameSite = SameSiteMode.Lax;
            cookie.Path = "/";
            // No "Expires" on purpose: the browser throws this cookie away when the browser is closed.
            context.Response.Cookies.Set(cookie);
        }

        // The session is empty: ask the cookie which admin it is.
        // Gives back the admin's number (and fills the session again), or 0 when we do not know.
        public static int Restore(HttpContext context)
        {
            HttpCookie cookie = context.Request.Cookies[CookieName];
            if (cookie == null || string.IsNullOrEmpty(cookie.Value)) return 0;

            // the cookie looks like:   7|A1B2C3...   (number | signature)
            string[] parts = cookie.Value.Split('|');
            int userId;
            if (parts.Length != 2 || !int.TryParse(parts[0], out userId) || userId <= 0) return 0;

            // Is the signature right? (if not, someone changed the cookie)
            if (!SameText(parts[1], Sign(parts[0]))) return 0;

            // Is this user still in the database, and is he/she an Admin?
            string role = null, fullName = null, email = null;
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT role, full_name, email FROM public.users WHERE user_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", userId);
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return 0;
                    role = reader.IsDBNull(0) ? "" : reader.GetString(0);
                    fullName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    email = reader.IsDBNull(2) ? "" : reader.GetString(2);
                }
            }
            if (!string.Equals((role ?? "").Trim(), "Admin", StringComparison.OrdinalIgnoreCase)) return 0;

            // Fill the session again, like the login page does
            context.Session["UserID"] = userId;
            context.Session["user_id"] = userId;
            context.Session["UserRole"] = role;
            context.Session["UserFullName"] = fullName;
            context.Session["UserEmail"] = email;
            return userId;
        }

        // Logout: remove the cookie
        public static void Forget(HttpContext context)
        {
            HttpCookie cookie = new HttpCookie(CookieName, "");
            cookie.Path = "/";
            cookie.Expires = DateTime.Now.AddDays(-1);      // a date in the past = the browser deletes it
            context.Response.Cookies.Set(cookie);
        }

        // The secret signature of a number. Only our website can make it,
        // because the secret key is your database connection text (nobody else has it).
        private static string Sign(string text)
        {
            byte[] key = Encoding.UTF8.GetBytes(ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString);
            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes("EduCoreAdminLogin:" + text));
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        // Compares two texts (always takes the same time, so it cannot be guessed letter by letter)
        private static bool SameText(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int difference = 0;
            for (int i = 0; i < a.Length; i++) difference |= a[i] ^ b[i];
            return difference == 0;
        }
    }
}