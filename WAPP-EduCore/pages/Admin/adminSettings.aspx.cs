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
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        private int adminId = 0;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

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

            if (!IsPostBack)
            {
                LoadForm();

                if (Request.QueryString["saved"] == "1")
                    ShowMessage("Saved! Your account information was updated.", true);
                if (Request.QueryString["pw"] == "1")
                    ShowPasswordMessage("Your password was changed successfully.", true);
            }
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            LoadBanner();
        }

        private int GetAdminId()
        {
            if (CurrentUserId() > 0)
                return CurrentUserId();

            object result = GetValue("SELECT user_id FROM public.users WHERE role = 'Admin' ORDER BY user_id LIMIT 1");
            return (result == null) ? 0 : Convert.ToInt32(result);
        }

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

        private string MakeInitials(string fullName)
        {
            string[] words = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0) return "AD";
            if (words.Length == 1) return words[0].Substring(0, 1).ToUpper();
            return words[0].Substring(0, 1).ToUpper();
        }

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

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();

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

            object same = GetValue("SELECT COUNT(*) FROM public.users WHERE LOWER(email) = LOWER(@email) AND user_id <> @id",
                new NpgsqlParameter("@email", email),
                new NpgsqlParameter("@id", adminId));
            if (Convert.ToInt32(same) > 0)
            {
                ShowMessage("That email is already used by another account.", false);
                return;
            }

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

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("adminSettings.aspx");
        }

        protected void btnPassword_Click(object sender, EventArgs e)
        {
            string current = txtCurrentPw.Text;
            string newPw = txtNewPw.Text;
            string confirm = txtConfirmPw.Text;

            if (current == "" || newPw == "" || confirm == "")
            {
                ShowPasswordMessage("Please fill in all three password boxes.", false);
                return;
            }

            object saved = GetValue("SELECT password_hash FROM public.users WHERE user_id = @id",
                new NpgsqlParameter("@id", adminId));
            if (saved == null || saved.ToString() != current)
            {
                ShowPasswordMessage("The current password is not correct.", false);
                return;
            }

            if (newPw.Length < 8)
            {
                ShowPasswordMessage("The new password must have at least 8 characters.", false);
                return;
            }

            if (newPw != confirm)
            {
                ShowPasswordMessage("The new passwords do not match.", false);
                return;
            }

            if (newPw == current)
            {
                ShowPasswordMessage("Please choose a password that is different from the current one.", false);
                return;
            }

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

        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            WAPP_EduCore.Data.LoginKeeper.Forget(Context);   // remove the "remember me" cookie
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/auth/login.aspx");
        }

        private void ShowMessage(string text, bool ok)
        {
            pnlMsg.Visible = true;
            pnlMsg.CssClass = "msg-box " + (ok ? "success" : "error");
            litMsg.Text = text;
            ClientScript.RegisterStartupScript(GetType(), "toTop", "window.scrollTo(0,0);", true);
        }

        private void ShowPasswordMessage(string text, bool ok)
        {
            pnlPwMsg.Visible = true;
            pnlPwMsg.CssClass = "msg-box " + (ok ? "success" : "error");
            litPwMsg.Text = text;
        }

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

        private int RunSql(string sql, params NpgsqlParameter[] parameters)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        private int CurrentUserId()
        {
            int id;
            if (int.TryParse(Convert.ToString(Session["UserID"]), out id) && id > 0) return id;
            if (int.TryParse(Convert.ToString(Session["user_id"]), out id) && id > 0) return id;

            // The session was forgotten (idle for a long time)? Ask the "remember me" cookie who the admin is.
            return WAPP_EduCore.Data.LoginKeeper.Restore(Context);
        }
    }
}