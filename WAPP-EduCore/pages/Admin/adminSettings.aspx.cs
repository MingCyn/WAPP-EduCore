using System;
using System.Data;
using System.Globalization;
using System.Web.UI;
using Npgsql;
using NpgsqlTypes;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    public partial class adminSettings : System.Web.UI.Page
    {

        // SECTION 1 : FIELDS (variables used by this page)
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        private int adminId = 0;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;


        // SECTION 2 : PAGE EVENTS (Page_Load, Page_PreRender)

        // ---------- Page_Load: check login, remember admin, load the form ----------
        protected void Page_Load(object sender, EventArgs e)
        {
            // Not logged in, or the session ran out (for example after resting for a while)?
            // Then go to the login page. We do NOT guess an admin any more.
            int signedInAdmin = CurrentUserId();
            if (signedInAdmin == 0)
            {
                Response.Redirect("~/Default.aspx");     // the first page of the website (it has the Log In button)
                return;
            }

            // Remember this admin in the browser until he/she clicks Logout
            WAPP_EduCore.Data.LoginKeeper.Remember(Context, signedInAdmin);

            adminId = GetAdminId();

            // First time the page opens (not a button click): fill the form and show messages
            if (!IsPostBack)
            {
                LoadForm();

                if (Request.QueryString["saved"] == "1")
                    ShowMessage("Saved! Your account information was updated.", true);

                if (Request.QueryString["pw"] == "1")
                    ShowPasswordMessage("Your password was changed successfully.", true);
            }
        }

        // ---------- Page_PreRender: refresh the banner just before the page is drawn ----------
        protected void Page_PreRender(object sender, EventArgs e)
        {
            LoadBanner();
        }

        // SECTION 3 : LOAD DATA (admin id, banner, form fields)

        // ---------- Get the id of the admin who is using this page ----------
        private int GetAdminId()
        {
            if (CurrentUserId() > 0)
                return CurrentUserId();

            object result = GetValue("SELECT user_id FROM public.users WHERE role = 'Admin' ORDER BY user_id LIMIT 1");
            return (result == null) ? 0 : Convert.ToInt32(result);
        }

        // ---------- Fill the banner (initials, name, email, id, member since) ----------
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

        // ---------- Make the initials from the full name ----------
        private string MakeInitials(string fullName)
        {
            string[] words = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0) return "AD";
            if (words.Length == 1) return words[0].Substring(0, 1).ToUpper();
            return words[0].Substring(0, 1).ToUpper();
        }

        // ---------- Fill the "Personal Information" form fields ----------
        private void LoadForm()
        {
            DataTable user = GetTable("SELECT full_name, email, dob, role FROM public.users WHERE user_id = @id",
                new NpgsqlParameter("@id", adminId));

            if (user.Rows.Count > 0)
            {
                txtName.Text = user.Rows[0]["full_name"].ToString();
                txtEmail.Text = user.Rows[0]["email"].ToString();

                if (user.Rows[0]["dob"] != DBNull.Value)
                    txtDob.Text = Convert.ToDateTime(user.Rows[0]["dob"]).ToString("yyyy-MM-dd", Inv);

                txtRole.Text = user.Rows[0]["role"] == DBNull.Value ? "" : user.Rows[0]["role"].ToString();
            }
        }

        // SECTION 4 : BUTTON EVENTS (Save, Cancel, Change Password, Logout)

        // ---------- "Save Changes" button: validate and update personal information ----------
        protected void btnSave_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();

            // Check: full name
            if (name == "")
            {
                ShowMessage("Please type your full name.", false);
                return;
            }

            // Check: email format
            if (email == "" || !email.Contains("@"))
            {
                ShowMessage("Please type a correct email address.", false);
                return;
            }

            // Check: email not used by another account
            object same = GetValue("SELECT COUNT(*) FROM public.users WHERE LOWER(email) = LOWER(@email) AND user_id <> @id",
                new NpgsqlParameter("@email", email),
                new NpgsqlParameter("@id", adminId));
            if (Convert.ToInt32(same) > 0)
            {
                ShowMessage("That email is already used by another account.", false);
                return;
            }

            // Check: date of birth (empty is allowed)
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

            // Save to the database
            try
            {
                RunSql("UPDATE public.users SET full_name = @name, email = @email, dob = @dob WHERE user_id = @id",
                    new NpgsqlParameter("@name", name),
                    new NpgsqlParameter("@email", email),
                    dob,
                    new NpgsqlParameter("@id", adminId));

                Response.Redirect("adminSettings.aspx?saved=1");
            }
            catch (Exception ex)
            {
                ShowMessage("Could not save: " + ex.Message, false);
            }
        }

        // ---------- "Cancel" button: reload the page without saving ----------
        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("adminSettings.aspx");
        }

        // ---------- "Change Password" button: validate and update the password ----------
        protected void btnPassword_Click(object sender, EventArgs e)
        {
            string current = txtCurrentPw.Text;
            string newPw = txtNewPw.Text;
            string confirm = txtConfirmPw.Text;

            // Check: all three boxes are filled
            if (current == "" || newPw == "" || confirm == "")
            {
                ShowPasswordMessage("Please fill in all three password boxes.", false);
                return;
            }

            // Check: current password is correct
            object saved = GetValue("SELECT password_hash FROM public.users WHERE user_id = @id",
                new NpgsqlParameter("@id", adminId));
            if (saved == null || saved.ToString() != current)
            {
                ShowPasswordMessage("The current password is not correct.", false);
                return;
            }

            // Check: new password length
            if (newPw.Length < 8)
            {
                ShowPasswordMessage("The new password must have at least 8 characters.", false);
                return;
            }

            // Check: new password matches the confirmation
            if (newPw != confirm)
            {
                ShowPasswordMessage("The new passwords do not match.", false);
                return;
            }

            // Check: new password is different from the current one
            if (newPw == current)
            {
                ShowPasswordMessage("Please choose a password that is different from the current one.", false);
                return;
            }

            // Save to the database
            try
            {
                RunSql("UPDATE public.users SET password_hash = @pw WHERE user_id = @id",
                    new NpgsqlParameter("@pw", newPw),
                    new NpgsqlParameter("@id", adminId));

                Response.Redirect("adminSettings.aspx?pw=1");
            }
            catch (Exception ex)
            {
                ShowPasswordMessage("Could not change the password: " + ex.Message, false);
            }
        }

        // ---------- "Logout" link: clear the login and go to the login page ----------
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            WAPP_EduCore.Data.LoginKeeper.Forget(Context);   // remove the "remember me" cookie
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/auth/login.aspx");
        }

        // SECTION 5 : MESSAGE HELPERS (show success / error messages)

        // ---------- Message at the top of the page (personal information) ----------
        private void ShowMessage(string text, bool ok)
        {
            pnlMsg.Visible = true;
            pnlMsg.CssClass = "msg-box " + (ok ? "success" : "error");
            litMsg.Text = text;
            ClientScript.RegisterStartupScript(GetType(), "toTop", "window.scrollTo(0,0);", true);
        }

        // ---------- Message inside the "Account & Security" card (password) ----------
        private void ShowPasswordMessage(string text, bool ok)
        {
            pnlPwMsg.Visible = true;
            pnlPwMsg.CssClass = "msg-box " + (ok ? "success" : "error");
            litPwMsg.Text = text;
        }

        // SECTION 6 : DATABASE HELPERS (run SQL queries)

        // ---------- Get one single value (e.g. COUNT, one column) ----------
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

        // ---------- Get many rows as a table ----------
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

        // ---------- Run INSERT / UPDATE / DELETE (returns rows affected) ----------
        private int RunSql(string sql, params NpgsqlParameter[] parameters)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        // SECTION 7 : LOGIN HELPER (who is signed in?)

        // ---------- Get the signed-in user id from Session or the "remember me" cookie ----------
        private int CurrentUserId()
        {
            int id;
            if (int.TryParse(Convert.ToString(Session["UserID"]), out id) && id > 0) return id;
            if (int.TryParse(Convert.ToString(Session["user_id"]), out id) && id > 0) return id;

            // No login in this session? Then nobody is logged in (we do NOT use the "remember me" cookie any more),
            // so the page sends the visitor to Default.aspx first.
            return 0;
        }
    }
}