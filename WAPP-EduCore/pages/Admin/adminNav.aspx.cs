using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using Npgsql;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    public partial class adminNav : Page
    {
        // These are available for binding in the .aspx navigation view
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Logout must be checked FIRST, otherwise LoginKeeper.Restore()
            // would read the cookie and log the admin straight back in.
            if (Request.QueryString["logout"] == "1")
            {
                DoLogout();
                return;
            }

            // Verify if the admin session is active
            int signedInAdmin = CurrentUserId();
            if (signedInAdmin > 0)
            {
                LoadAdminInfo(signedInAdmin);
            }
        }

        // Logout: remove the remember-me cookie, clear the session, go to the home page
        private void DoLogout()
        {
            LoginKeeper.Forget(Context);   // deletes the EduCoreAdminLogin cookie
            Session.Clear();
            Session.Abandon();
            FormsAuthentication.SignOut();

            Response.Redirect("~/default.aspx");
        }

        // Retrieves the administrator's full name and builds initials
        private void LoadAdminInfo(int adminId)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT full_name FROM public.users WHERE user_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", adminId);
                object result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    string fullName = result.ToString().Trim();
                    AdminName = Server.HtmlEncode(fullName);

                    string[] words = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length > 0)
                    {
                        string initials = words[0].Substring(0, 1);
                        if (words.Length > 1)
                            initials += words[words.Length - 1].Substring(0, 1);
                        AdminInitials = initials.ToUpper();
                    }
                }
            }
        }

        // Helper to retrieve the current user's session ID
        private int CurrentUserId()
        {
            int id;
            if (int.TryParse(Convert.ToString(Session["UserID"]), out id) && id > 0) return id;
            if (int.TryParse(Convert.ToString(Session["user_id"]), out id) && id > 0) return id;

            return 0;
        }
    }
}