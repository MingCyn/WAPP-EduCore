using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace WAPP_EduCore.pages.Shared
{
    public partial class Sidebar : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Mark active link based on current page
                MarkActivePage();
            }
        }

        private void MarkActivePage()
        {
            try
            {
                var current = Request.AppRelativeCurrentExecutionFilePath?.ToLowerInvariant() ?? string.Empty;

                // Find all anchors in the sidebar and mark the matching one as active
                foreach (Control ctrl in this.Controls)
                {
                    if (ctrl is LiteralControl) continue;
                    FindAndMarkActive(ctrl, current);
                }
            }
            catch { /* Silently fail if parsing current page fails */ }
        }

        private void FindAndMarkActive(Control parent, string currentPath)
        {
            foreach (Control ctrl in parent.Controls)
            {
                var anchor = ctrl as HtmlAnchor;
                if (anchor != null)
                {
                    string href = anchor.HRef ?? string.Empty;
                    string id = anchor.ID ?? string.Empty;

                    bool isActive = false;
                    if (currentPath.Contains("studentdashboard.aspx") && href.Contains("studentdashboard.aspx"))
                        isActive = true;
                    else if (currentPath.Contains("mycourses.aspx") && href.Contains("mycourses.aspx"))
                        isActive = true;
                    else if (currentPath.Contains("calendar.aspx") && href.Contains("calendar.aspx"))
                        isActive = true;
                    else if (currentPath.Contains("discussion.aspx") && href.Contains("discussion.aspx"))
                        isActive = true;
                    else if (currentPath.Contains("settings.aspx") && href.Contains("settings.aspx"))
                        isActive = true;
                    else if (currentPath.Contains("messages.aspx") && href.Contains("messages.aspx"))
                        isActive = true;

                    if (isActive)
                    {
                        var classes = anchor.Attributes["class"] ?? string.Empty;
                        if (!classes.Contains("active"))
                            anchor.Attributes["class"] = (classes + " active").Trim();
                    }
                }
                else
                {
                    // Recursively check child controls
                    FindAndMarkActive(ctrl, currentPath);
                }
            }
        }
    }
}
