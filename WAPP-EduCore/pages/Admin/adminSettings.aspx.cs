using System;
using System.Data;
using System.Globalization;
using System.Web.UI;
using Npgsql;
using NpgsqlTypes;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    // NOTE: the class name is adminSettings because your .aspx first line says Inherits="...adminSettings"
    public partial class adminSettings : System.Web.UI.Page
    {
        // These are shown in the sidebar and the top-right circle
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        // Which user are we editing? (the logged-in admin)
        private int adminId = 0;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;


        // =====================================================
        //  STEP 1: Page opens
        //  - find out who the admin is
        //  - the FIRST time only: fill the boxes with the saved information
        //    (after a button click we keep what the admin typed)
        // =====================================================
        protected void Page_Load(object sender, EventArgs e)
        {
            adminId = GetAdminId();

            if (!IsPostBack)
            {
                LoadForm();
            }
        }

        // =====================================================
        //  STEP 2: PreRender runs after every button click and just BEFORE
        //  the page is drawn. So the name in the sidebar and the banner
        //  is always the newest one (even right after saving).
        // =====================================================
        protected void Page_PreRender(object sender, EventArgs e)
        {
            LoadBanner();
        }


        // =====================================================
        //  WHO IS THE ADMIN?
        //  - If the login page saved Session["user_id"], use that user.
        //  - If not, use the first user whose role is 'Admin'.
        // =====================================================
        private int GetAdminId()
        {
            if (CurrentUserId() > 0)
                return CurrentUserId();

            object result = GetValue("SELECT user_id FROM public.users WHERE role = 'Admin' ORDER BY user_id LIMIT 1");
            return (result == null) ? 0 : Convert.ToInt32(result);
        }


        // =====================================================
        //  THE BANNER  +  the name in the sidebar and the top-right circle
        // =====================================================
        private void LoadBanner()
        {
            DataTable user = GetTable("SELECT full_name, email, created_at FROM public.users WHERE user_id = @id",
                new NpgsqlParameter("@id", adminId));

            if (user.Rows.Count == 0) return;

            string fullName = user.Rows[0]["full_name"].ToString().Trim();

            AdminName = Server.HtmlEncode(fullName);
            AdminInitials = MakeInitials(fullName);

            litInitials.Text = AdminInitials;
            litBannerName.Text = fullName;
            litBannerEmail.Text = user.Rows[0]["email"].ToString();
            litBannerId.Text = adminId.ToString();

            if (user.Rows[0]["created_at"] != DBNull.Value)
                litSince.Text = Convert.ToDateTime(user.Rows[0]["created_at"]).ToString("MMMM yyyy", Inv);
            else
                litSince.Text = "—";
        }

        // "Maya Chen" -> "M"  (first letter of the name (big letter, like Z for zhilin))
        private string MakeInitials(string fullName)
        {
            string[] words = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0) return "AD";
            if (words.Length == 1) return words[0].Substring(0, 1).ToUpper();
            return words[0].Substring(0, 1).ToUpper();
        }


        // =====================================================
        //  FILL THE BOXES with the saved information
        // =====================================================
        private void LoadForm()
        {
            // ----- From the users table -----
            DataTable user = GetTable("SELECT full_name, email, dob, role FROM public.users WHERE user_id = @id",
                new NpgsqlParameter("@id", adminId));

            if (user.Rows.Count > 0)
            {
                txtName.Text = user.Rows[0]["full_name"].ToString();
                txtEmail.Text = user.Rows[0]["email"].ToString();

                // The date box needs the date written as  2000-12-31
                if (user.Rows[0]["dob"] != DBNull.Value)
                    txtDob.Text = Convert.ToDateTime(user.Rows[0]["dob"]).ToString("yyyy-MM-dd", Inv);

                // The role is only SHOWN (the box is read-only, it cannot be changed)
                txtRole.Text = user.Rows[0]["role"] == DBNull.Value ? "" : user.Rows[0]["role"].ToString();
            }
        }


        // =====================================================
        //  BUTTON : Save Changes  (personal information)
        // =====================================================
        protected void btnSave_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();

            // ----- Check what the admin typed -----
            if (name == "")
            {
                ShowMessage("Please type your full name.", false);
                return;
            }

            if (email == "" || !email.Contains("@"))
            {
                ShowMessage("Please type a correct email address.", false);
                return;
            }

            // Is this email already used by SOMEONE ELSE?
            object same = GetValue("SELECT COUNT(*) FROM public.users WHERE LOWER(email) = LOWER(@email) AND user_id <> @id",
                new NpgsqlParameter("@email", email),
                new NpgsqlParameter("@id", adminId));
            if (Convert.ToInt32(same) > 0)
            {
                ShowMessage("That email is already used by another account.", false);
                return;
            }

            // The date of birth is optional. Empty = nothing saved.
            NpgsqlParameter dob = new NpgsqlParameter("@dob", NpgsqlDbType.Date);
            DateTime dobValue;
            if (txtDob.Text.Trim() == "")
            {
                dob.Value = DBNull.Value;
            }
            else if (DateTime.TryParse(txtDob.Text.Trim(), Inv, DateTimeStyles.None, out dobValue))
            {
                dob.Value = dobValue.Date;
            }
            else
            {
                ShowMessage("That date of birth is not correct.", false);
                return;
            }

            // ----- Save -----
            try
            {
                RunSql("UPDATE public.users SET full_name = @name, email = @email, dob = @dob WHERE user_id = @id",
                    new NpgsqlParameter("@name", name),
                    new NpgsqlParameter("@email", email),
                    dob,
                    new NpgsqlParameter("@id", adminId));

                ShowMessage("Saved! Your account information was updated.", true);
            }
            catch (Exception ex)
            {
                ShowMessage("Could not save: " + ex.Message, false);
            }
        }


        // =====================================================
        //  BUTTON : Cancel  (throw away what was typed, show the saved information again)
        // =====================================================
        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("adminSettings.aspx");
        }


        // =====================================================
        //  BUTTON : Change Password
        // =====================================================
        protected void btnPassword_Click(object sender, EventArgs e)
        {
            string current = txtCurrentPw.Text;
            string newPw = txtNewPw.Text;
            string confirm = txtConfirmPw.Text;

            // 1) Is every box filled?
            if (current == "" || newPw == "" || confirm == "")
            {
                ShowPasswordMessage("Please fill in all three password boxes.", false);
                return;
            }

            // 2) Is the current password right?
            //    (the password is saved as plain text in password_hash, same as signup.aspx)
            object saved = GetValue("SELECT password_hash FROM public.users WHERE user_id = @id",
                new NpgsqlParameter("@id", adminId));
            if (saved == null || saved.ToString() != current)
            {
                ShowPasswordMessage("The current password is not correct.", false);
                return;
            }

            // 3) Is the new password long enough?
            if (newPw.Length < 8)
            {
                ShowPasswordMessage("The new password must have at least 8 characters.", false);
                return;
            }

            // 4) Are the 2 new passwords the same?
            if (newPw != confirm)
            {
                ShowPasswordMessage("The new passwords do not match.", false);
                return;
            }

            // 5) Is it really NEW?
            if (newPw == current)
            {
                ShowPasswordMessage("Please choose a password that is different from the current one.", false);
                return;
            }

            // 6) Save it
            try
            {
                RunSql("UPDATE public.users SET password_hash = @pw WHERE user_id = @id",
                    new NpgsqlParameter("@pw", newPw),
                    new NpgsqlParameter("@id", adminId));

                ShowPasswordMessage("Your password was changed.", true);
            }
            catch (Exception ex)
            {
                ShowPasswordMessage("Could not change the password: " + ex.Message, false);
            }
        }


        // =====================================================
        //  LOGOUT  (the "Logout" option in the drop-down menu)
        // =====================================================
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();        // forget who was logged in
            Session.Abandon();      // end the session
            Response.Redirect("~/auth/login.aspx");   // go to the login page
        }


        // =====================================================
        //  HELPERS  (small tools used again and again)
        // =====================================================

        // Green (ok) or red (error) message at the top of the page
        private void ShowMessage(string text, bool ok)
        {
            pnlMsg.Visible = true;
            pnlMsg.CssClass = "msg " + (ok ? "ok" : "error");
            litMsg.Text = text;
        }

        // Green (ok) or red (error) message inside the "Account & Security" card
        private void ShowPasswordMessage(string text, bool ok)
        {
            pnlPwMsg.Visible = true;
            pnlPwMsg.CssClass = "msg " + (ok ? "ok" : "error");
            litPwMsg.Text = text;
        }

        // Asks the database for ONE value (a number, a text...)
        private object GetValue(string sql, params NpgsqlParameter[] parameters)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                object result = cmd.ExecuteScalar();
                return (result == DBNull.Value) ? null : result;
            }
        }

        // Asks the database for a TABLE of rows
        private DataTable GetTable(string sql, params NpgsqlParameter[] parameters)
        {
            DataTable table = new DataTable();
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd))
                {
                    adapter.Fill(table);
                }
            }
            return table;
        }

        // Runs a command that CHANGES the database. Gives back how many rows changed.
        private int RunSql(string sql, params NpgsqlParameter[] parameters)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        // =====================================================
        //  WHO IS LOGGED IN?  (used by every admin page)
        //  The login page saves the user number in Session["UserID"].
        //  (Session["user_id"] is also checked, in case an older page saved it.)
        //  Gives back 0 when nobody is logged in.
        // =====================================================
        private int CurrentUserId()
        {
            int id;
            if (int.TryParse(Convert.ToString(Session["UserID"]), out id) && id > 0) return id;
            if (int.TryParse(Convert.ToString(Session["user_id"]), out id) && id > 0) return id;
            return 0;
        }
    }
}