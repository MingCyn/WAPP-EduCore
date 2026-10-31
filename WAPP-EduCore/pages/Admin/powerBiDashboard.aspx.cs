using System;
using System.Web;
using System.Web.UI;
using Npgsql;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    public partial class powerBiDashboard : Page
    {
        // These are shown in the sidebar and the top-right circle.
        // The .aspx page reads them with <%= AdminName %> and <%= AdminInitials %>
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        // The id of the admin who is looking at this page right now
        private int adminId = 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Not logged in, or the session ran out? Go back to the login page.
            int signedInAdmin = CurrentUserId();
            if (signedInAdmin == 0)
            {
                Response.Redirect("~/Default.aspx");
                return;
            }

            // Remember this admin in the browser until he/she clicks Logout
            WAPP_EduCore.Data.LoginKeeper.Remember(Context, signedInAdmin);

            LoadAdminInfo();   // runs every time the page opens
        }

        // =====================================================
        //  Get the admin's name from the database
        // =====================================================
        private void LoadAdminInfo()
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;

                if (CurrentUserId() > 0)
                {
                    cmd.CommandText = "SELECT full_name, user_id FROM public.users WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("@id", CurrentUserId());
                }
                else
                {
                    cmd.CommandText = "SELECT full_name, user_id FROM public.users WHERE role = 'Admin' ORDER BY user_id LIMIT 1";
                }

                string name = null;
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        name = reader[0].ToString();
                        adminId = Convert.ToInt32(reader[1]);
                    }
                }

                if (name != null)
                {
                    string fullName = name.Trim();
                    AdminName = Server.HtmlEncode(fullName);

                    // Initials = first letter of the name (big letter)
                    string[] words = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 1)
                        AdminInitials = words[0].Substring(0, 1).ToUpper();
                    else if (words.Length > 1)
                        AdminInitials = words[0].Substring(0, 1).ToUpper();
                }
            }
        }

        // =====================================================
        //  LOGOUT
        // =====================================================
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            WAPP_EduCore.Data.LoginKeeper.Forget(Context);   // remove the "remember me" cookie
            Session.Clear();         // forget who was logged in
            Session.Abandon();       // end the session
            Response.Redirect("~/auth/login.aspx");    // go to the login page
        }

        // =====================================================
        //  WHO IS LOGGED IN?
        // =====================================================
        private int CurrentUserId()
        {
            int id;
            if (int.TryParse(Convert.ToString(Session["UserID"]), out id) && id > 0) return id;
            if (int.TryParse(Convert.ToString(Session["user_id"]), out id) && id > 0) return id;

            // The session was forgotten? Ask the "remember me" cookie.
            return WAPP_EduCore.Data.LoginKeeper.Restore(Context);
        }
    }
}