using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI;
using WAPP_EduCore;

namespace WAPP_EduCore.pages.General
{
    public partial class coursesDirectory : Page
    {
        // Values must match what is stored in courses.course_level
        private static readonly string[] Levels = { "Primary", "Secondary", "Undergraduate", "Professional", "Lifelong" };

        protected string Query = "";
        protected string Level = "";
        protected string Topic = "";
        protected string Sort = "newest";

        protected void Page_Load(object sender, EventArgs e)
        {
            Query = (Request.QueryString["q"] ?? "").Trim();
            Level = (Request.QueryString["level"] ?? "").Trim();
            Topic = (Request.QueryString["topic"] ?? "").Trim();
            Sort = (Request.QueryString["sort"] ?? "newest").Trim();
            if (Sort != "title") Sort = "newest";

            BuildChips();
            BuildTopics();
            BuildSort();
            BindCourses();
        }

        // ---------- Data ----------

        private void BindCourses()
        {
            string sql = @"
            SELECT c.course_id, c.title, c.category, c.duration, c.course_img, c.course_level,
                   u.full_name AS instructor,
                   (SELECT COUNT(*) FROM lesson l WHERE l.course_id = c.course_id) AS lesson_count
            FROM courses c
            LEFT JOIN users u ON u.user_id = c.user_id
            WHERE (@level = '' OR LOWER(c.course_level) = @level)
              AND (@topic = '' OR c.category = @topic)
              AND (@q = ''
                   OR LOWER(c.title) LIKE @like
                   OR LOWER(c.course_description) LIKE @like
                   OR LOWER(u.full_name) LIKE @like
                   OR EXISTS (SELECT 1 FROM lesson l2
                              WHERE l2.course_id = c.course_id
                                AND LOWER(l2.lesson_title) LIKE @like))
            ORDER BY " + (Sort == "title" ? "c.title ASC" : "c.created_at DESC");

            string q = Query.ToLowerInvariant();
            string likeValue = "%" + q.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

            var p = new Dictionary<string, object>
        {
            { "@level", Level.ToLowerInvariant() },
            { "@topic", Topic },
            { "@q", q },
            { "@like", likeValue }
        };

            DataTable dt = Db.Query(sql, p);
            rptCourses.DataSource = dt;
            rptCourses.DataBind();
            pnlEmpty.Visible = dt.Rows.Count == 0;
        }

        // ---------- Filter UI ----------

        private void BuildChips()
        {
            var sb = new StringBuilder();
            sb.Append(Chip("All", "", Level == ""));
            foreach (string l in Levels)
                sb.Append(Chip(l, l, string.Equals(Level, l, StringComparison.OrdinalIgnoreCase)));
            litChips.Text = sb.ToString();
        }

        private string Chip(string label, string value, bool active)
        {
            return string.Format("<a class=\"chip{0}\" href=\"{1}\"{2}>{3}</a>",
                active ? " active" : "",
                Enc(UrlFor(value)),
                active ? " aria-current=\"true\"" : "",
                Enc(label));
        }

        private string UrlFor(string level)
        {
            var parts = new List<string>();
            if (Query != "") parts.Add("q=" + HttpUtility.UrlEncode(Query));
            if (level != "") parts.Add("level=" + HttpUtility.UrlEncode(level));
            if (Topic != "") parts.Add("topic=" + HttpUtility.UrlEncode(Topic));
            if (Sort != "newest") parts.Add("sort=" + Sort);
            return "coursesDirectory.aspx" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
        }

        private void BuildTopics()
        {
            DataTable dt = Db.Query("SELECT DISTINCT category FROM courses WHERE category IS NOT NULL AND category <> '' ORDER BY category");

            var sb = new StringBuilder();
            sb.AppendFormat("<option value=\"\"{0}>All topics</option>", Topic == "" ? " selected" : "");
            foreach (DataRow r in dt.Rows)
            {
                string cat = Convert.ToString(r["category"]);
                sb.AppendFormat("<option value=\"{0}\"{1}>Topic: {0}</option>",
                    Enc(cat), cat == Topic ? " selected" : "");
            }
            litTopics.Text = sb.ToString();
        }

        private void BuildSort()
        {
            litSort.Text =
                "<option value=\"newest\"" + (Sort == "newest" ? " selected" : "") + ">Sort by: Newest</option>" +
                "<option value=\"title\"" + (Sort == "title" ? " selected" : "") + ">Sort by: Title A-Z</option>";
        }

        // ---------- Template helpers ----------

        protected static string Enc(object o)
        {
            return HttpUtility.HtmlEncode(Convert.ToString(o) ?? "");
        }

        protected string CoverStyle(object img)
        {
            string url = Convert.ToString(img).Trim();
            if (string.IsNullOrWhiteSpace(url)) return "";

            // Automatically routes filenames to your root images/ folder
            if (!url.StartsWith("~/") && !url.StartsWith("/") && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (url.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
                {
                    url = "~/" + url;
                }
                else
                {
                    url = "~/images/" + url;
                }
            }

            if (url.StartsWith("~/"))
            {
                url = ResolveUrl(url);
            }

            url = url.Replace("'", "%27").Replace("\"", "%22");
            return "background-image:url('" + url + "')";
        }

        protected string LessonText(object count)
        {
            int n = Convert.ToInt32(count);
            return n + (n == 1 ? " lesson" : " lessons");
        }

        protected string DurationText(object duration)
        {
            string d = Convert.ToString(duration);
            return string.IsNullOrWhiteSpace(d) ? "" : " &middot; " + Enc(d);
        }
    }
}