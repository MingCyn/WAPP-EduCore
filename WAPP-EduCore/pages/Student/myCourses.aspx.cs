using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WAPP_EduCore.pages.Student
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        private long UserId
        {
            get { return Convert.ToInt64(Session["UserId"]); }
        }

        private string SelectedLevel
        {
            get { return (ViewState["Level"] as string) ?? ""; }
            set { ViewState["Level"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserId"] == null)
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }

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
                "WHERE course_level IS NOT NULL AND course_level <> '' ORDER BY course_level");
            rptLevels.DataSource = dt;
            rptLevels.DataBind();
            btnAllLevels.CssClass = SelectedLevel == "" ? "pill active" : "pill";
        }

        private void BindCategories()
        {
            DataTable dt = Db.Query(
                "SELECT DISTINCT category FROM courses " +
                "WHERE category IS NOT NULL AND category <> '' ORDER BY category");
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
                "WHERE (@level = '' OR c.course_level = @level) " +
                "  AND (@cat = '' OR c.category = @cat) " +
                "  AND (@q = '' OR c.title ILIKE '%' || @q || '%' OR c.category ILIKE '%' || @q || '%') " +
                "ORDER BY " + orderBy;   // orderBy is whitelisted above, never user text

            DataTable dt = Db.Query(sql, new Dictionary<string, object>
            {
                { "@uid", UserId },
                { "@level", SelectedLevel },
                { "@cat", ddlCategory.SelectedValue ?? "" },
                { "@q", txtSearch.Text.Trim() }
            });

            rptCourses.DataSource = dt;
            rptCourses.DataBind();
            pnlEmpty.Visible = dt.Rows.Count == 0;
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

        protected void rptCourses_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            long courseId = Convert.ToInt64(e.CommandArgument);

            if (e.CommandName == "enroll")
            {
                Db.Execute(
                    "INSERT INTO enrollment (user_id, course_id) VALUES (@uid, @cid) " +
                    "ON CONFLICT (user_id, course_id) DO NOTHING",
                    new Dictionary<string, object> { { "@uid", UserId }, { "@cid", courseId } });

                lblMessage.Text = "You are now enrolled.";
                lblMessage.Visible = true;
                LoadCourses();
            }
            else if (e.CommandName == "view")
            {
                // Page we build next (lessons of this course
                Response.Redirect("~/pages/Student/courseOverview.aspx?id=" + courseId);
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
            return (int)Math.Min(100, d * 100 / t);
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