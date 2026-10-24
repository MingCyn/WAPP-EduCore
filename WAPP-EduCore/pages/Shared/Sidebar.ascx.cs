using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace WAPP_EduCore.pages.Shared
{
    public partial class Sidebar : UserControl
    {
        // Declared by hand because this control has no designer file
        protected HtmlGenericControl lblName;
        protected HtmlGenericControl lblId;
        protected HtmlGenericControl lblInitials;
        protected HtmlAnchor lnkDashboard;
        protected Panel pnlLoggedIn;
        protected Panel pnlGuest;
        protected LinkButton btnLogout;
        //protected HtmlAnchor lnkCalendar, lnkMessages, lnkGrades, lnkCertificates, lnkSettings;

        protected void Page_Load(object sender, EventArgs e)
        {
            MarkActivePage();
            CheckLoginState();
        }

        // ---------- active menu item ----------

        private void MarkActivePage()
        {
            try
            {
                string currentFile = Path.GetFileName(
                    Request.AppRelativeCurrentExecutionFilePath ?? string.Empty).ToLowerInvariant();

                if (currentFile == "courseoverview.aspx" || currentFile == "assessment.aspx" || currentFile == "learningprogression.aspx")
                    currentFile = "mycourses.aspx";

                FindAndMarkActive(this, currentFile);
            }
            catch { /* never break the page because of the menu highlight */ }
        }

        private void FindAndMarkActive(Control parent, string currentFile)
        {
            foreach (Control ctrl in parent.Controls)
            {
                HtmlAnchor anchor = ctrl as HtmlAnchor;
                if (anchor != null)
                {
                    string linkFile = Path.GetFileName(anchor.HRef ?? string.Empty).ToLowerInvariant();
                    if (linkFile.Length > 0 && linkFile == currentFile)
                    {
                        string classes = anchor.Attributes["class"] ?? string.Empty;
                        if (!classes.Contains("active"))
                            anchor.Attributes["class"] = (classes + " active").Trim();
                    }
                }
                else if (ctrl.HasControls())
                {
                    FindAndMarkActive(ctrl, currentFile);
                }
            }
        }

        // ---------- check session & toggle panels ----------

        private void CheckLoginState()
        {
            if (Session["UserID"] == null)
            {
                // Guest mode
                if (pnlLoggedIn != null) pnlLoggedIn.Visible = false;
                if (pnlGuest != null) pnlGuest.Visible = true;
                if (lnkDashboard != null) lnkDashboard.HRef = ResolveUrl("~/Default.aspx");
            }
            else
            {
                // Logged-in mode
                if (pnlLoggedIn != null) pnlLoggedIn.Visible = true;
                if (pnlGuest != null) pnlGuest.Visible = false;
                if (lnkDashboard != null) lnkDashboard.HRef = ResolveUrl("~/pages/Student/studentDashboard.aspx");
                LoadStudent();
            }
        }

        private void LoadStudent()
        {
            try
            {
                long userId = Convert.ToInt64(Session["UserID"]);

                string name = Session["UserFullName"] as string;
                if (string.IsNullOrEmpty(name))
                {
                    DataTable dt = Db.Query(
                        "SELECT full_name FROM users WHERE user_id = @uid",
                        new Dictionary<string, object> { { "@uid", userId } });

                    if (dt.Rows.Count == 0) return;
                    name = Convert.ToString(dt.Rows[0]["full_name"]);
                    Session["UserFullName"] = name;
                }

                if (lblName != null) lblName.InnerText = name;
                if (lblId != null) lblId.InnerText = "Student ID: " + userId;
                if (lblInitials != null) lblInitials.InnerText = InitialsOf(name);
            }
            catch { /* keep placeholder text if anything fails */ }
        }

        private static string InitialsOf(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";

            string[] parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string initials = parts[0].Substring(0, 1);
            if (parts.Length > 1) initials += parts[parts.Length - 1].Substring(0, 1);
            return initials.ToUpperInvariant();
        }

        // ---------- log out ----------

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            FormsAuthentication.SignOut();

            Response.Redirect("~/auth/login.aspx");
        }
    }
}