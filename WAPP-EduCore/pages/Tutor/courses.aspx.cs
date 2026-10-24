using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using Npgsql;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class courses : System.Web.UI.Page
    {
        // Change to the name of your EXISTING connection string in Web.config.
        private const string ConnectionName = "SupabasePostgres";

        // Session keys set by auth/login.aspx.cs
        private const string SessionUserKey = "UserID";
        private const string SessionNameKey = "UserFullName";
        private const string LoginPage = "~/auth/login.aspx";

        // Exact table name in Supabase (singular).
        private const string LessonsTable = "public.lesson";

        // While developing, the real database error is shown inside the Create course pop-up
        // and in the status box so problems are easy to diagnose. Set this to false before you deploy.
        private const bool ShowDatabaseErrors = true;

        private int userId;

        private bool TryGetUserId(out int id)
        {
            return int.TryParse(Convert.ToString(Session[SessionUserKey]), out id) && id > 0;
        }

        // Shown in the page footer. Filled from the login session in Page_Load.
        protected string TutorName = "";

        // Totals for the summary tiles and filter chips (filled in LoadCourses)
        protected int TotalCount, PublishedCount, DraftCount;

        // ------------------------------------------------------------------
        // SQL - only the signed-in tutor's courses are listed (c.user_id = @uid).
        // status is expected to be 'published', 'draft' or 'pending_review' (case-insensitive).
        // ------------------------------------------------------------------
        private const string SqlCourses =
            "SELECT c.course_id, c.title, c.category, " +
            "       COALESCE(c.status, 'draft') AS status, " +
            "       COALESCE(NULLIF(c.course_img, ''), NULLIF(c.thumbnail_url, ''), '') AS thumb, " +
            "       COALESCE(c.created_at, NOW()) AS created_at, " +
            "       COALESCE(c.updated_at, c.created_at, NOW()) AS updated_at, " +
            "       (SELECT COUNT(*) FROM " + LessonsTable + " l WHERE l.course_id = c.course_id) AS lesson_count " +
            "FROM courses c " +
            "WHERE c.user_id = @uid " +
            "ORDER BY c.created_at DESC NULLS LAST, c.course_id DESC";

        // Assumes an enrollments table with one row per student per course.
        // If it doesn't exist yet the page still works and shows 0 students.
        private const string SqlEnrolled =
            "SELECT course_id, COUNT(*) FROM enrollments GROUP BY course_id";

        // Creates the course and records who owns it (user_id).
        // If your courses table has other NOT NULL columns without a default
        // (for example course_description or course_level), add them here too.
        // Creates an empty draft owned by the logged-in tutor; the details are filled in inside the builder.
        private const string SqlCreateDraft =
            "INSERT INTO courses (title, category, duration, user_id, status, created_at, updated_at) " +
            "VALUES ('Untitled course', '', 1, @uid, 'draft', NOW(), NOW()) RETURNING course_id";

        private const string SqlUserName =
            "SELECT full_name FROM public.users WHERE user_id = @uid";

        public class CourseRow
        {
            public int CourseId { get; set; }
            public string Title { get; set; }
            public string SortTitle { get; set; }
            public string Topic { get; set; }
            public string StatusKey { get; set; }      // published | draft | pending
            public string StatusLabel { get; set; }
            public string StatusClass { get; set; }
            public string LessonsText { get; set; }
            public int Enrolled { get; set; }
            public string EnrolledText { get; set; }
            public string MetaText { get; set; }
            public string ThumbHtml { get; set; }
            public string SearchText { get; set; }
            public long UpdatedMs { get; set; }
            public long CreatedMs { get; set; }
        }

        // ------------------------------------------------------------------
        // Page load: no courses -> empty state, otherwise the course list
        // ------------------------------------------------------------------
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryGetUserId(out userId))
            {
                Response.Redirect(LoginPage);
                return;
            }

            // Runs on every request (including postbacks) so the footer name is always the logged-in user's.
            TutorName = GetTutorName();

            if (IsPostBack) return;

            List<CourseRow> courses;
            try
            {
                courses = LoadCourses();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                var pg = ex as PostgresException;
                ShowMessage("DEBUG: " + ex.GetType().Name + " - " + (pg != null ? pg.MessageText : ex.Message));
                return;                              // error shown, neither state displayed
            }

            if (courses.Count == 0)
            {
                ShowDebugInfo();                     // TEMPORARY: session id vs user_id values in the table
                PnlEmpty.Visible = true;
                PnlList.Visible = false;
            }
            else
            {
                PnlEmpty.Visible = false;
                PnlList.Visible = true;
                RptCourses.DataSource = courses;
                RptCourses.DataBind();
            }
        }

        // ------------------------------------------------------------------
        // Logged-in user's name: session first, database as a fallback
        // ------------------------------------------------------------------
        private string GetTutorName()
        {
            string name = Convert.ToString(Session[SessionNameKey]);
            if (!string.IsNullOrWhiteSpace(name)) return name;

            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                using (var cmd = new NpgsqlCommand(SqlUserName, conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        name = Convert.ToString(result);
                        Session[SessionNameKey] = name;   // cache for other pages
                        return name;
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            return "Tutor";
        }

        private List<CourseRow> LoadCourses()
        {
            var list = new List<CourseRow>();

            using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
            {
                conn.Open();

                Dictionary<int, int> enrolled = LoadEnrolledCounts(conn);

                using (NpgsqlCommand cmd = CoursesCommand(conn))
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int id = Convert.ToInt32(r["course_id"]);
                        string title = r.IsDBNull(r.GetOrdinal("title")) ? "(Untitled course)" : Convert.ToString(r["title"]);
                        string topic = r.IsDBNull(r.GetOrdinal("category")) ? "" : Convert.ToString(r["category"]);
                        string status = r.IsDBNull(r.GetOrdinal("status")) ? "draft" : Convert.ToString(r["status"]);
                        string thumb = r.IsDBNull(r.GetOrdinal("thumb")) ? "" : Convert.ToString(r["thumb"]);
                        DateTime created = ToUtc(r.GetDateTime(r.GetOrdinal("created_at")));
                        DateTime updated = r.IsDBNull(r.GetOrdinal("updated_at")) ? created : ToUtc(r.GetDateTime(r.GetOrdinal("updated_at")));
                        int lessons = Convert.ToInt32(r["lesson_count"]);
                        int students = enrolled.ContainsKey(id) ? enrolled[id] : 0;

                        string key, label, css;
                        MapStatus(status, out key, out label, out css);

                        list.Add(new CourseRow
                        {
                            CourseId = id,
                            Title = title,
                            SortTitle = title.ToLowerInvariant(),
                            Topic = topic,
                            StatusKey = key,
                            StatusLabel = label,
                            StatusClass = css,
                            LessonsText = lessons == 1 ? "1 lesson" : lessons + " lessons",
                            Enrolled = students,
                            EnrolledText = students == 1 ? "1 enrolled student" : students + " enrolled students",
                            MetaText = BuildMeta(key, updated),
                            ThumbHtml = BuildThumb(thumb, id, title, topic), // <-- Keep this one
                            SearchText = (title + " " + topic).ToLowerInvariant(),
                            UpdatedMs = ToUnixMs(updated),
                            CreatedMs = ToUnixMs(created)
                        });

                        TotalCount++;
                        if (key == "published") PublishedCount++;
                        else DraftCount++;
                    }
                }
            }
            return list;
        }

        // Courses query, limited to the signed-in tutor
        private NpgsqlCommand CoursesCommand(NpgsqlConnection conn)
        {
            var cmd = new NpgsqlCommand(SqlCourses, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            return cmd;
        }

        private static Dictionary<int, int> LoadEnrolledCounts(NpgsqlConnection conn)
        {
            var map = new Dictionary<int, int>();
            try
            {
                using (var cmd = new NpgsqlCommand(SqlEnrolled, conn))
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) map[Convert.ToInt32(r[0])] = Convert.ToInt32(r[1]);
                }
            }
            catch (PostgresException ex)
            {
                // No enrollments table yet (or different name): show 0 students instead of failing the page.
                System.Diagnostics.Debug.WriteLine("Enrollment counts unavailable: " + ex.Message);
            }
            return map;
        }

        // ------------------------------------------------------------------
        // Presentation helpers
        // ------------------------------------------------------------------
        private static void MapStatus(string raw, out string key, out string label, out string css)
        {
            switch ((raw ?? "").Trim().ToLowerInvariant().Replace(' ', '_'))
            {
                case "published":
                    key = "published"; label = "Published"; css = "cc-badge-published"; break;
                default:
                    key = "draft"; label = "Draft"; css = "cc-badge-draft"; break;
            }
        }

        private static string BuildMeta(string statusKey, DateTime updatedUtc)
        {
            string when = RelativeTime(updatedUtc);
            switch (statusKey)
            {
                case "published": return "All content published \u2022 Updated " + when;
                case "pending": return "Submitted for review \u2022 " + when;
                default: return "Course setup in progress \u2022 Edited " + when;
            }
        }

        private static string RelativeTime(DateTime utc)
        {
            TimeSpan span = DateTime.UtcNow - utc;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return Plural((int)span.TotalMinutes, "min") + " ago";
            if (span.TotalHours < 24) return Plural((int)span.TotalHours, "hour") + " ago";
            if (span.TotalDays < 30) return Plural((int)span.TotalDays, "day") + " ago";
            return utc.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        }

        private static string Plural(int n, string unit)
        {
            return n + " " + unit + (n == 1 ? "" : "s");
        }

        // Real thumbnail if thumbnail_url is a valid https link, otherwise an illustrated placeholder.
        // Updated BuildThumb to support local server uploads and https links
        private string BuildThumb(string url, int id, string title, string topic)
        {
            // 1. Check if it's a valid local server upload (e.g. starting with ~/ or /uploads/)
            if (!string.IsNullOrWhiteSpace(url) && (url.StartsWith("~/", StringComparison.Ordinal) || url.StartsWith("/uploads/", StringComparison.Ordinal)))
            {
                string resolvedUrl = ResolveUrl(url);
                return "<div class=\"cc-thumb\"><img src=\"" + HttpUtility.HtmlAttributeEncode(resolvedUrl) + "\" alt=\"\" /></div>";
            }

            // 2. Check if it's an absolute https link
            Uri uri;
            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out uri) && uri.Scheme == Uri.UriSchemeHttps)
            {
                return "<div class=\"cc-thumb\"><img src=\"" + HttpUtility.HtmlAttributeEncode(url) + "\" alt=\"\" /></div>";
            }

            // 3. Fallback to illustrated placeholder if no valid image exists
            string text = (title + " " + topic).ToLowerInvariant();
            int variant;
            if (text.Contains("web") || text.Contains("program") || text.Contains("code")) variant = 0;
            else if (text.Contains("database") || text.Contains("sql") || text.Contains("data")) variant = 1;
            else if (text.Contains("graph") || text.Contains("algorithm") || text.Contains("tree")) variant = 2;
            else variant = id % 3;

            const string lines =
                "<rect x=\"96\" y=\"14\" width=\"100\" height=\"8\" rx=\"4\" fill=\"#c4c8de\" />" +
                "<rect x=\"96\" y=\"30\" width=\"142\" height=\"8\" rx=\"4\" fill=\"#c4c8de\" />" +
                "<rect x=\"96\" y=\"46\" width=\"80\" height=\"8\" rx=\"4\" fill=\"#c4c8de\" />";
            const string open = "<svg viewBox=\"0 0 240 80\" fill=\"none\" stroke=\"#2a4494\" stroke-width=\"2.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">";

            string icon, bg;
            if (variant == 0)
            {
                bg = "cc-thumb-blue";
                icon = "<polyline points=\"24,16 8,38 24,60\" /><polyline points=\"58,16 74,38 58,60\" /><line x1=\"48\" y1=\"8\" x2=\"34\" y2=\"68\" />";
            }
            else if (variant == 1)
            {
                bg = "cc-thumb-peach";
                icon = "<ellipse cx=\"38\" cy=\"18\" rx=\"28\" ry=\"10\" /><path d=\"M10 18v40c0 6 12.5 10 28 10s28-4 28-10V18\" /><path d=\"M10 38c0 6 12.5 10 28 10s28-4 28-10\" />";
            }
            else
            {
                bg = "cc-thumb-peach";
                icon = "<rect x=\"30\" y=\"4\" width=\"16\" height=\"16\" rx=\"3\" /><rect x=\"6\" y=\"54\" width=\"16\" height=\"16\" rx=\"3\" /><rect x=\"54\" y=\"54\" width=\"16\" height=\"16\" rx=\"3\" /><path d=\"M38 20v16M14 54V36h48v18\" />";
            }

            return "<div class=\"cc-thumb " + bg + "\">" + open + icon + "<g stroke=\"none\">" + lines + "</g></svg></div>";
        }

        private static DateTime ToUtc(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Local) return dt.ToUniversalTime();
            if (dt.Kind == DateTimeKind.Unspecified) return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return dt;
        }

        private static long ToUnixMs(DateTime utc)
        {
            return (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        // ------------------------------------------------------------------
        // Create course pop-up -> insert the course, then open it in the course builder
        // ------------------------------------------------------------------
        protected void BtnNewCourse_Click(object sender, EventArgs e)
        {
            int newId;
            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                using (var cmd = new NpgsqlCommand(SqlCreateDraft, conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    conn.Open();
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                var pg = ex as PostgresException;
                string detail = pg != null ? pg.MessageText : ex.Message;
                ReloadList();   // a postback skips the list loading in Page_Load, so rebuild the page first
                ShowMessage("We couldn't create the course." + (ShowDatabaseErrors ? " (" + detail + ")" : " Please try again."));
                return;
            }

            // edit=1 makes the builder open its "Edit course information" pop-up right away
            Response.Redirect("courseBuilder.aspx?courseId=" + newId + "&edit=1");
        }

        // After a failed pop-up submit the page is rebuilt, so load the list again.
        private void ReloadList()
        {
            try
            {
                TotalCount = PublishedCount = DraftCount = 0;
                List<CourseRow> courses = LoadCourses();
                PnlEmpty.Visible = courses.Count == 0;
                PnlList.Visible = courses.Count > 0;
                RptCourses.DataSource = courses;
                RptCourses.DataBind();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                ShowMessage("DEBUG: " + ex.GetType().Name + " - " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // TEMPORARY debug helper: remove the ShowDebugInfo() call in Page_Load
        // (and this method) before you deploy.
        // ------------------------------------------------------------------
        private void ShowDebugInfo()
        {
            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                {
                    conn.Open();

                    long mine, total;
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM courses WHERE user_id = @uid", conn))
                    {
                        cmd.Parameters.AddWithValue("uid", userId);
                        mine = Convert.ToInt64(cmd.ExecuteScalar());
                    }
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM courses", conn))
                    {
                        total = Convert.ToInt64(cmd.ExecuteScalar());
                    }

                    var ids = new List<string>();
                    using (var cmd = new NpgsqlCommand("SELECT DISTINCT user_id FROM courses LIMIT 10", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) ids.Add(r.IsDBNull(0) ? "NULL" : Convert.ToString(r[0]));
                    }

                    ShowMessage("DEBUG: session UserID=" + userId +
                                " | courses for this user=" + mine +
                                " | total courses=" + total +
                                " | user_id values in table=[" + string.Join(", ", ids.ToArray()) + "]");
                }
            }
            catch (Exception ex)
            {
                ShowMessage("DEBUG info failed: " + ex.Message);
            }
        }

        private void ShowMessage(string message)
        {
            LblStatus.Text = HttpUtility.HtmlEncode(message);
            LblStatus.Visible = true;
        }
    }
}