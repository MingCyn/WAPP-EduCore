using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WAPP_EduCore
{
    public partial class navDashboard : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public string ActivePage { get; set; }

        protected bool IsLoggedIn
        {
            get { return Session["user_id"] != null; }
        }

        protected string DisplayName
        {
            get { return Convert.ToString(Session["full_name"]) ?? ""; }
        }

        protected string Role
        {
            get { return Convert.ToString(Session["role"]) ?? ""; }
        }

        protected string Initial
        {
            get
            {
                string n = DisplayName.Trim();
                return n.Length > 0 ? n.Substring(0, 1).ToUpperInvariant() : "?";
            }
        }

        /// <summary>After login the user comes back to the page they were on.</summary>
        protected string LoginUrl
        {
            get
            {
                string back = HttpUtility.UrlEncode(Request.Url.PathAndQuery);
                return ResolveUrl("~/login.aspx") + "?returnUrl=" + back;
            }
        }

        protected string Css(string key)
        {
            return string.Equals(ActivePage, key, StringComparison.OrdinalIgnoreCase) ? "active" : "";
        }


    }
}