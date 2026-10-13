using System;
using System.IO;
using System.Web.UI;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class tutorDashboard : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Ensure only tutors can access the dashboard
            if (Session["UserRole"] == null || !Session["UserRole"].ToString().Equals("Tutor", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("../../auth/login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            // Render the shared navigation (tutorNav.aspx) into the panel container
            try
            {
                using (var sw = new StringWriter())
                {
                    Server.Execute("~/pages/Tutor/tutorNav.aspx", sw);
                    pnlNavContainer.Controls.Add(new LiteralControl(sw.ToString()));
                }
            }
            catch (Exception)
            {
                // Fail gracefully if the shared nav cannot be rendered
                pnlNavContainer.Controls.Add(new LiteralControl("<!-- tutorNav rendering failed -->"));
            }
        }
    }
}
