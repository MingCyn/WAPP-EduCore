using System;
using System.Web.UI;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class tutorNav : Page
    {
        // Name shown in the navbar, taken from the logged-in user's session.
        protected string DisplayName = "Tutor";

        protected void Page_Load(object sender, EventArgs e)
        {
            string name = Convert.ToString(Session["UserFullName"]);
            if (!string.IsNullOrWhiteSpace(name))
            {
                DisplayName = name.Trim();
            }
        }
    }
}