using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Student
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        protected bool IsGuest  // <-- Change from private to protected
        {
            get
            {
                object id = Session["UserID"];
                if (id == null) return true;
                long parsed;
                if (!long.TryParse(Convert.ToString(id), out parsed) || parsed <= 0) return true;
                string role = Convert.ToString(Session["Role"]);
                if (string.Equals(role, "guest", StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
        }

        private long UserId
        {
            get { return IsGuest ? 0 : Convert.ToInt64(Session["UserID"]); }
        }

        private string SelectedLevel
        {
            get { return (ViewState["Level"] as string) ?? ""; }
            set { ViewState["Level"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Stale session validation check for logged-in users
            if (!IsGuest)
            {
                int userCount = 0;
                // Use your project's standard database querying method
                using (var conn = DatabaseHelper.GetConnection())
                using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE user_id = @uid", conn))
                {
                    cmd.Parameters.AddWithValue("@uid", UserId);
                    userCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (userCount <= 0)
                {
                    Session.Clear();             // stale session, force re-login
                    Response.Redirect("~/auth/login.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }
            }

            ddlEnroll.Visible = !IsGuest;

            if (!IsPostBack)
            {
                BindLevels();
                BindCategories();
                LoadCourses();
            }
        }

        // ---------- data loading ----------

        private void BindLevels()
        {
            DataTable dt = Db.Query(
                "SELECT DISTINCT course_level FROM courses " +
                "WHERE lower(status) = 'published' AND course_level IS NOT NULL AND course_level <> '' ORDER BY course_level");
            rptLevels.DataSource = dt;
            rptLevels.DataBind();
            btnAllLevels.CssClass = SelectedLevel == "" ? "pill active" : "pill";
        }

        private void BindCategories()
        {
            DataTable dt = Db.Query(
                "SELECT DISTINCT category FROM courses " +
                "WHERE lower(status) = 'published' AND category IS NOT NULL AND category <> '' ORDER BY category");
            ddlCategory.Items.Clear();
            ddlCategory.Items.Add(new ListItem("Topic: All", ""));
            foreach (DataRow r in dt.Rows)
            {
                string cat = r["category"].ToString();
                ddlCategory.Items.Add(new ListItem("Topic: " + cat, cat));
            }
        }

        private void LoadCourses()
        {
            string orderBy;
            switch (ddlSort.SelectedValue)
            {
                case "newest": orderBy = "c.created_at DESC"; break;
                case "title": orderBy = "c.title ASC"; break;
                default: orderBy = "student_count DESC, c.title ASC"; break;
            }

            string enrollFilter = IsGuest ? "" : (ddlEnroll.SelectedValue ?? "");

            string sql =
                "SELECT c.course_id, c.title, c.category, c.duration, c.course_img, c.course_level, " +
                "  COALESCE(u.full_name, 'TBA') AS instructor, " +
                "  (SELECT COUNT(*) FROM enrollment e WHERE e.course_id = c.course_id) AS student_count, " +
                "  EXISTS (SELECT 1 FROM enrollment e2 WHERE e2.course_id = c.course_id AND e2.user_id = @uid) AS is_enrolled, " +
                "  (SELECT COUNT(*) FROM lesson l WHERE l.course_id = c.course_id) AS total_lessons, " +
                "  (SELECT COUNT(*) FROM lesson_progress lp JOIN lesson l2 ON l2.lesson_id = lp.lesson_id " +
                "     WHERE l2.course_id = c.course_id AND lp.user_id = @uid AND lp.is_completed) AS done_lessons " +
                "FROM courses c " +
                "LEFT JOIN users u ON u.user_id = c.user_id " +
                "WHERE lower(c.status) = 'published' AND (@level = '' OR c.course_level = @level) " +
                "  AND (@cat = '' OR c.category = @cat) " +
                "  AND (@q = '' OR c.title ILIKE '%' || @q || '%' OR c.category ILIKE '%' || @q || '%') " +
                "  AND (@enr = '' " +
                "       OR (@enr = 'enrolled'  AND EXISTS (SELECT 1 FROM enrollment x WHERE x.course_id = c.course_id AND x.user_id = @uid)) " +
                "       OR (@enr = 'available' AND NOT EXISTS (SELECT 1 FROM enrollment y WHERE y.course_id = c.course_id AND y.user_id = @uid))) " +
                "ORDER BY " + orderBy;

            DataTable dt = Db.Query(sql, new Dictionary<string, object>
            {
                { "@uid", UserId },
                { "@level", SelectedLevel },
                { "@cat", ddlCategory.SelectedValue ?? "" },
                { "@q", txtSearch.Text.Trim() },
                { "@enr", enrollFilter }
            });

            rptCourses.DataSource = dt;
            rptCourses.DataBind();
            pnlEmpty.Visible = dt.Rows.Count == 0;

            HighlightActiveFilters();
        }

        private void HighlightActiveFilters()
        {
            ddlCategory.CssClass = "filter-select" + (string.IsNullOrEmpty(ddlCategory.SelectedValue) ? "" : " is-active");
            ddlEnroll.CssClass = "filter-select" + (string.IsNullOrEmpty(ddlEnroll.SelectedValue) ? "" : " is-active");
            ddlSort.CssClass = "filter-select" + (ddlSort.SelectedValue == "popular" ? "" : " is-active");
        }

        // ---------- events ----------

        protected void Filters_Changed(object sender, EventArgs e)
        {
            LoadCourses();
        }

        protected void btnAllLevels_Click(object sender, EventArgs e)
        {
            SelectedLevel = "";
            BindLevels();
            LoadCourses();
        }

        protected void rptLevels_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            SelectedLevel = Convert.ToString(e.CommandArgument);
            BindLevels();
            LoadCourses();
        }

        // ONLY ONE copy of this handler
        // ---------- button state (guest-safe) ----------

        private bool IsEnrolled(object isEnrolled)
        {
            // Guests can never be enrolled, regardless of what the query returns
            if (IsGuest || isEnrolled == null || isEnrolled == DBNull.Value) return false;
            return Convert.ToBoolean(isEnrolled);
        }

        protected string GetCommand(object isEnrolled)
        {
            // Guests and enrolled students -> view; other students -> enroll
            return (IsGuest || IsEnrolled(isEnrolled)) ? "view" : "enroll";
        }

        protected string GetButtonText(object isEnrolled)
        {
            if (IsGuest) return "View Course";
            return IsEnrolled(isEnrolled) ? "Continue" : "Enroll";
        }

        private void GoTo(string url)
        {
            // Avoids the ThreadAbortException that Response.Redirect(url) throws
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void rptCourses_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            long courseId;
            if (!long.TryParse(Convert.ToString(e.CommandArgument), out courseId))
                return;

            if (e.CommandName == "view")
            {
                // Guests and students both go to the overview, no enrollment check
                GoTo("~/pages/Student/courseOverview.aspx?id=" + courseId);
            }
            else if (e.CommandName == "enroll")
            {
                // Guests should never reach here, but guard anyway
                if (IsGuest)
                {
                    GoTo("~/Auth/login.aspx?returnUrl=" +
                         Server.UrlEncode("~/pages/Student/courseOverview.aspx?id=" + courseId));
                    return;
                }

                Db.Execute(
    "INSERT INTO enrollment (user_id, course_id) " +
    "SELECT u.user_id, c.course_id FROM courses c " +
    "JOIN users u ON u.user_id = @uid " +
    "WHERE c.course_id = @cid AND lower(c.status) = 'published' " +
    "ON CONFLICT (user_id, course_id) DO NOTHING",
    new Dictionary<string, object> { { "@uid", UserId }, { "@cid", courseId } });

                lblMessage.Text = "You are now enrolled.";
                lblMessage.Visible = true;
                LoadCourses();
            }
        }

        // ---------- helpers used by the markup ----------

        protected string PillClass(object level)
        {
            return Convert.ToString(level) == SelectedLevel ? "pill active" : "pill";
        }

        protected int Percent(object total, object done)
        {
            long t = Convert.ToInt64(total);
            long d = Convert.ToInt64(done);
            if (t <= 0) return 0;
            return (int)Math.Min(100, (d * 100) / t);
        }

        protected string MediaStyle(object img)
        {
            string url = Convert.ToString(img);
            if (string.IsNullOrWhiteSpace(url))
                return "background:linear-gradient(135deg,#eef3fb,#e6f0ff)";

            url = url.Trim();
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = ResolveUrl("~/" + url.TrimStart('/', '~'));

            url = url.Replace("'", "%27").Replace("(", "%28").Replace(")", "%29");
            return "background:url('" + HttpUtility.HtmlAttributeEncode(url) + "') center/cover";
        }
    }
}