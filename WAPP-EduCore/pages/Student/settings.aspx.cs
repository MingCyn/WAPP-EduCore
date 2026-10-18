using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net.Mail;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WAPP_EduCore.pages.Student
{
    public partial class studentProfile : System.Web.UI.Page
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private long UserId { get { return Convert.ToInt64(Session["UserID"]); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadSettings();

                if (Request.QueryString["saved"] == "1")
                    ShowMessage(true, "Your changes have been saved.");
                if (Request.QueryString["pw"] == "1")
                    ShowPwMessage(true, "Your password has been updated.");
            }
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            litA11y.Text = BuildAccessibilityScript();
        }

        // ---------- loading ----------
        private void LoadSettings()
        {
            var p = new Dictionary<string, object> { { "@uid", UserId } };

            DataTable u = Db.Query(
                "SELECT full_name, email, dob, created_at FROM users WHERE user_id = @uid", p);
            if (u.Rows.Count == 0)
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }

            DataRow r = u.Rows[0];
            string name = Convert.ToString(r["full_name"]);
            string email = Convert.ToString(r["email"]);

            txtName.Text = name;
            txtEmail.Text = email;
            txtDob.Text = r["dob"] == DBNull.Value
                ? "" : Convert.ToDateTime(r["dob"]).ToString("yyyy-MM-dd", Inv);

            litInitials.Text = InitialsOf(name);
            litBannerName.Text = name;
            litBannerEmail.Text = email;
            litBannerId.Text = UserId.ToString();
            litSince.Text = r["created_at"] == DBNull.Value
                ? "-" : Convert.ToDateTime(r["created_at"]).ToString("MMMM yyyy", Inv);

            // defaults, used when the student has no profile row yet
            rngGoal.Value = "1.5";

            try
            {
                DataTable pr = Db.Query(
                    "SELECT phone, program, learning_preference, daily_goal_hours, " +
                    "high_contrast, large_font, reduced_motion FROM profile WHERE user_id = @uid", p);

                if (pr.Rows.Count > 0)
                {
                    DataRow x = pr.Rows[0];
                    txtPhone.Text = Convert.ToString(x["phone"]);
                    txtProgram.Text = Convert.ToString(x["program"]);

                    string style = Convert.ToString(x["learning_preference"]);
                    if (ddlStyle.Items.FindByValue(style) != null) ddlStyle.SelectedValue = style;

                    if (x["daily_goal_hours"] != DBNull.Value)
                        rngGoal.Value = Convert.ToDecimal(x["daily_goal_hours"]).ToString("0.0", Inv);

                    chkContrast.Checked = Convert.ToBoolean(x["high_contrast"]);
                    chkLarge.Checked = Convert.ToBoolean(x["large_font"]);
                    chkMotion.Checked = Convert.ToBoolean(x["reduced_motion"]);
                }
            }
            catch (Exception)
            {
                ShowMessage(false, "The profile table was not found. Run the CREATE TABLE profile SQL in Supabase first.");
            }
        }

        // ---------- save profile ----------
        protected void btnSave_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();

            if (name.Length == 0)
            {
                ShowMessage(false, "Please enter your full name.");
                return;
            }
            if (!IsValidEmail(email))
            {
                ShowMessage(false, "Please enter a valid email address.");
                return;
            }

            object dob = DBNull.Value;
            string dobText = txtDob.Text.Trim();
            if (dobText.Length > 0)
            {
                DateTime d;
                if (!DateTime.TryParseExact(dobText, "yyyy-MM-dd", Inv, DateTimeStyles.None, out d))
                {
                    ShowMessage(false, "Please enter a valid date of birth.");
                    return;
                }
                if (d.Date > DateTime.Today)
                {
                    ShowMessage(false, "Date of birth cannot be in the future.");
                    return;
                }
                dob = d.Date;
            }

            decimal goal;
            if (!decimal.TryParse(rngGoal.Value, NumberStyles.Float, Inv, out goal)) goal = 1.5m;
            if (goal < 0.5m) goal = 0.5m;
            if (goal > 6m) goal = 6m;

            try
            {
                DataTable dup = Db.Query(
                    "SELECT 1 FROM users WHERE lower(email) = lower(@e) AND user_id <> @uid",
                    new Dictionary<string, object> { { "@e", email }, { "@uid", UserId } });
                if (dup.Rows.Count > 0)
                {
                    ShowMessage(false, "That email address is already used by another account.");
                    return;
                }

                // profile first, so that a missing table stops the save before anything changes
                Db.Execute(
                    "INSERT INTO profile (user_id, phone, program, learning_preference, daily_goal_hours, " +
                    "  high_contrast, large_font, reduced_motion) " +
                    "VALUES (@uid, @phone, @prog, @style, @goal, @hc, @lf, @rm) " +
                    "ON CONFLICT (user_id) DO UPDATE SET phone = @phone, program = @prog, " +
                    "  learning_preference = @style, daily_goal_hours = @goal, " +
                    "  high_contrast = @hc, large_font = @lf, reduced_motion = @rm",
                    new Dictionary<string, object>
                    {
                        { "@uid", UserId },
                        { "@phone", NullIfEmpty(txtPhone.Text) },
                        { "@prog", NullIfEmpty(txtProgram.Text) },
                        { "@style", ddlStyle.SelectedValue },
                        { "@goal", goal },
                        { "@hc", chkContrast.Checked },
                        { "@lf", chkLarge.Checked },
                        { "@rm", chkMotion.Checked }
                    });

                Db.Execute(
                    "UPDATE users SET full_name = @n, email = @e, dob = CAST(@dob AS date) WHERE user_id = @uid",
                    new Dictionary<string, object>
                    {
                        { "@n", name }, { "@e", email }, { "@dob", dob }, { "@uid", UserId }
                    });
            }
            catch (Exception ex)
            {
                ShowMessage(false, "Could not save your changes: " + ex.Message);
                return;
            }

            // keep the session (and so the sidebar) in step with the new details
            Session["UserFullName"] = name;
            Session["UserEmail"] = email;

            // reload so that the sidebar shows the new name and a refresh cannot save twice
            Response.Redirect("~/pages/Student/settings.aspx?saved=1");
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/pages/Student/settings.aspx");
        }

        // ---------- change password ----------
        protected void btnPassword_Click(object sender, EventArgs e)
        {
            string current = txtCurrentPw.Text;
            string newPw = txtNewPw.Text;
            string confirm = txtConfirmPw.Text;

            if (current.Length == 0 || newPw.Length == 0 || confirm.Length == 0)
            {
                ShowPwMessage(false, "Please fill in all three password fields.");
                return;
            }

            try
            {
                DataTable t = Db.Query(
                    "SELECT password_hash FROM users WHERE user_id = @uid",
                    new Dictionary<string, object> { { "@uid", UserId } });

                // login.aspx.cs compares the typed password with this column directly
                if (t.Rows.Count == 0 || Convert.ToString(t.Rows[0]["password_hash"]) != current)
                {
                    ShowPwMessage(false, "Your current password is incorrect.");
                    return;
                }
                if (newPw.Length < 8)
                {
                    ShowPwMessage(false, "The new password must be at least 8 characters.");
                    return;
                }
                if (newPw != confirm)
                {
                    ShowPwMessage(false, "The new passwords do not match.");
                    return;
                }
                if (newPw == current)
                {
                    ShowPwMessage(false, "The new password must be different from the current one.");
                    return;
                }

                Db.Execute(
                    "UPDATE users SET password_hash = @p WHERE user_id = @uid",
                    new Dictionary<string, object> { { "@p", newPw }, { "@uid", UserId } });
            }
            catch (Exception ex)
            {
                ShowPwMessage(false, "Could not change the password: " + ex.Message);
                return;
            }

            Response.Redirect("~/pages/Student/settings.aspx?pw=1");
        }

        // ---------- helpers ----------
        private void ShowMessage(bool ok, string text)
        {
            pnlMsg.Visible = true;
            pnlMsg.CssClass = "msg " + (ok ? "ok" : "error");
            litMsg.Text = text;
            ClientScript.RegisterStartupScript(GetType(), "toTop", "window.scrollTo(0,0);", true);
        }

        private void ShowPwMessage(bool ok, string text)
        {
            pnlPwMsg.Visible = true;
            pnlPwMsg.CssClass = "msg " + (ok ? "ok" : "error");
            litPwMsg.Text = text;
        }

        private static object NullIfEmpty(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                MailAddress a = new MailAddress(email);
                return a.Address == email;
            }
            catch { return false; }
        }

        private static string InitialsOf(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            string[] parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string initials = parts[0].Substring(0, 1);
            if (parts.Length > 1) initials += parts[parts.Length - 1].Substring(0, 1);
            return initials.ToUpperInvariant();
        }

        private string BuildAccessibilityScript()
        {
            var classes = new List<string>();
            if (chkContrast.Checked) classes.Add("a11y-contrast");
            if (chkLarge.Checked) classes.Add("a11y-large");
            if (chkMotion.Checked) classes.Add("a11y-motion");

            if (classes.Count == 0) return "";
            return "<script>document.documentElement.className += ' " + string.Join(" ", classes) + "';</script>";
        }
    }
}