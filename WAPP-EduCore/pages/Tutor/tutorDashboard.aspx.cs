using System;
using System.Web.UI;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class tutorDashboard : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserRole"] == null || !Session["UserRole"].ToString().Equals("Tutor", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("../../auth/login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }
    }
}