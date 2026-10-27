using System;
using System.Globalization;
using System.Web.UI;

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
                LoadForm();

                if (Request.QueryString["saved"] == "1")
                    ShowMessage("Your changes have been saved.", true);
                if (Request.QueryString["pw"] == "1")
                    ShowPasswordMessage("Your password has been updated successfully.", true);
            }
        }

        // Runs before the page is drawn, so the banner always shows the saved details
        protected void Page_PreRender(object sender, EventArgs e)
        {
            LoadBanner();
        }

        // ---------- banner and avatar ----------
        private void LoadBanner()
        {
            AccountService.UserInfo u = AccountService.GetUser(UserId);
            if (u == null) return;

            string initials = AccountService.InitialsOf(u.FullName);

            litMiniInitials.Text = initials;
            litInitials.Text = initials;
            litBannerName.Text = (u.FullName ?? "").Trim();
            litBannerEmail.Text = u.Email;
            litBannerId.Text = UserId.ToString();
            litSince.Text = u.CreatedAt.HasValue ? u.CreatedAt.Value.ToString("MMMM yyyy", Inv) : "-";
        }

        // ---------- fill the boxes ----------
        private void LoadForm()
        {
            AccountService.UserInfo u = AccountService.GetUser(UserId);
            if (u == null)
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }

            txtName.Text = u.FullName;
            txtEmail.Text = u.Email;
            txtDob.Text = u.Dob.HasValue ? u.Dob.Value.ToString("yyyy-MM-dd", Inv) : "";
            txtRole.Text = u.Role ?? "";
        }

        // ---------- Save Changes ----------
        protected void btnSave_Click(object sender, EventArgs e)
        {
            DateTime? dob = null;
            string dobText = txtDob.Text.Trim();
            if (dobText.Length > 0)
            {
                DateTime d;
                if (!DateTime.TryParseExact(dobText, "yyyy-MM-dd", Inv, DateTimeStyles.None, out d))
                {
                    ShowMessage("That date of birth is not correct.", false);
                    return;
                }
                dob = d.Date;
            }

            AccountService.Result r = AccountService.UpdateDetails(UserId, txtName.Text, txtEmail.Text, dob);
            if (!r.Ok)
            {
                ShowMessage(r.Message, false);
                return;
            }

            // keep the session (and so the sidebar) in step with the new details
            Session["UserFullName"] = txtName.Text.Trim();
            Session["UserEmail"] = txtEmail.Text.Trim();

            // reload so that the sidebar shows the new name and a refresh cannot save twice
            Response.Redirect("~/pages/Student/settings.aspx?saved=1");
        }

        // ---------- Cancel ----------
        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/pages/Student/settings.aspx");
        }

        // ---------- Change Password ----------
        protected void btnPassword_Click(object sender, EventArgs e)
        {
            string currentPw = txtCurrentPw.Text.Trim();
            string newPw = txtNewPw.Text.Trim();
            string confirmPw = txtConfirmPw.Text.Trim();

            if (string.IsNullOrEmpty(currentPw) || string.IsNullOrEmpty(newPw) || string.IsNullOrEmpty(confirmPw))
            {
                ShowPasswordMessage("All password fields are required.", false);
                return;
            }

            AccountService.Result r = AccountService.ChangePassword(
                UserId, currentPw, newPw, confirmPw);

            if (!r.Ok)
            {
                ShowPasswordMessage(r.Message, false);
                return;
            }

            Response.Redirect("~/pages/Student/settings.aspx?pw=1");
        }

        // ---------- Logout ----------
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/auth/login.aspx");
        }

        // ---------- messages ----------
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
    }
}