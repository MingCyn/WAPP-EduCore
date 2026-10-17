using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace WAPP_EduCore.pages.Shared
{
    public partial class Sidebar : UserControl
    {
        // Declared by hand because this control has no designer file
        protected HtmlGenericControl lblName;
        protected HtmlGenericControl lblId;

        protected void Page_Load(object sender, EventArgs e)
        {
            MarkActivePage();
            LoadStudent();
        }

        // ---------- active menu item ----------

        private void MarkActivePage()
        {
            try
            {
                string currentFile = Path.GetFileName(
                    Request.AppRelativeCurrentExecutionFilePath ?? string.Empty).ToLowerInvariant();

                // Keep "My Courses" highlighted on the course overview page
                if (currentFile == "courseoverview.aspx" || currentFile == "assessment.aspx") currentFile = "mycourses.aspx";

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

        // ---------- student name and ID ----------

        private void LoadStudent()
        {
            try
            {
                if (Session["UserID"] == null) return;
                long userId = Convert.ToInt64(Session["UserID"]);

                // login.aspx.cs already saves the name here
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
            }
            catch { /* keep the placeholder text if anything fails */ }
        }
    }
}