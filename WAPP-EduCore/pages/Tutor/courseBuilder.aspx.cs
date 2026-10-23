using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using Npgsql;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class courseBuilder : System.Web.UI.Page
    {
        // ------------------------------------------------------------------
        // Database connection. Change ConnectionName to the name of your EXISTING
        // connection string in Web.config (the one your other pages already use).
        // ------------------------------------------------------------------
        private const string ConnectionName = "SupabasePostgres";

        // Session key set by auth/login.aspx.cs (courses.user_id).
        private const string SessionUserKey = "UserID";
        private const string LoginPage = "~/auth/login.aspx";

        // Exact table name in Supabase (singular).
        private const string LessonsTable = "public.lesson";

        private int userId;

        private bool TryGetUserId(out int id)
        {
            return int.TryParse(Convert.ToString(Session[SessionUserKey]), out id) && id > 0;
        }

        // Where to send the tutor if courseId is missing or the course does not exist.
        private const string CourseListPage = "courses.aspx";

        private static NpgsqlConnection OpenConnection()
        {
            var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString);
            conn.Open();
            return conn;
        }

        // ------------------------------------------------------------------
        // SQL. Column names match your courses and lesson tables.
        // Every course query also checks user_id, so a tutor can only open or change their own courses.
        // ------------------------------------------------------------------

        // Column ORDER matters: TryGetCourse reads title, category, duration by position.
        private const string SqlCourse =
            "SELECT title, category, duration " +
            "FROM courses WHERE course_id = @cid AND user_id = @uid";

        private const string SqlCourseUpdate =
            "UPDATE courses SET title = @title, category = @topic, duration = @duration, updated_at = now() " +
            "WHERE course_id = @cid AND user_id = @uid";

        private const string SqlLessons =
            "SELECT lesson_id, lesson_title, lesson_content::text " +
            "FROM " + LessonsTable + " WHERE course_id = @cid ORDER BY created_at, lesson_id";

        private const string SqlLesson =
            "SELECT lesson_title, lesson_description, quiz_id, lesson_content::text, created_at " +
            "FROM " + LessonsTable + " WHERE lesson_id = @lid AND course_id = @cid";

        private const string SqlLessonDetailsUpdate =
            "UPDATE " + LessonsTable + " SET lesson_title = @title, lesson_description = @desc " +
            "WHERE lesson_id = @lid AND course_id = @cid";

        private const string SqlLessonContentLock =
            "SELECT lesson_content::text FROM " + LessonsTable + " " +
            "WHERE lesson_id = @lid AND course_id = @cid FOR UPDATE";

        // If lesson_content is a plain text column (not json/jsonb), change "AS jsonb" to "AS text".
        private const string SqlLessonContentUpdate =
            "UPDATE " + LessonsTable + " SET lesson_content = CAST(@content AS jsonb) " +
            "WHERE lesson_id = @lid AND course_id = @cid";

        // ------------------------------------------------------------------
        // Data shapes
        // ------------------------------------------------------------------
        public class LessonRow
        {
            public int LessonId { get; set; }
            public string Title { get; set; }
            public bool HasVideo { get; set; }
            public bool IsActive { get; set; }
        }

        private class LessonDetail
        {
            public string Title;
            public string Description;
            public string VideoUrl;
            public long? QuizId;
            public string ContentJson;
            public DateTime CreatedAt;
        }

        private int courseId;
        private int lessonId;

        // ------------------------------------------------------------------
        // Page load
        // ------------------------------------------------------------------
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryGetUserId(out userId))
            {
                Response.Redirect(LoginPage);
                return;
            }

            // Your course list page links here as: courseBuilder.aspx?courseId=1  (optionally &lessonId=5)
            if (!int.TryParse(Request.QueryString["courseId"], out courseId))
            {
                Response.Redirect(CourseListPage);
                return;
            }
            int.TryParse(Request.QueryString["lessonId"], out lessonId);

            try
            {
                LoadPage();

                // courses.aspx "Edit" button links here with &edit=1 -> open the course info pop-up straight away
                if (!IsPostBack && Request.QueryString["edit"] == "1")
                {
                    ClientScript.RegisterStartupScript(GetType(), "openCourseInfo",
                        "window.addEventListener('load', function () { openModal('modalCourseInfo'); });", true);
                }
            }
            catch (NpgsqlException ex)
            {
                // TEMPORARY while developing: show the real database error.
                // Replace with the generic message before you deploy.
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                var pg = ex as PostgresException;
                ShowMessage("DEBUG: " + (pg != null ? pg.MessageText : ex.Message), true);
                PnlLessonView.Visible = false;
            }
        }

        private void LoadPage()
        {
            using (NpgsqlConnection conn = OpenConnection())
            {
                // 1) Course (must belong to the logged-in user)
                string courseTitle, topic, duration;
                if (!TryGetCourse(conn, out courseTitle, out topic, out duration))
                {
                    Response.Redirect(CourseListPage);
                    return;
                }
                LblCourseName.Text = Server.HtmlEncode(courseTitle);
                if (!IsPostBack)
                {
                    TxtCourseTitle.Text = courseTitle;
                    TxtCourseTopic.Text = topic;
                    TxtCourseDuration.Text = duration;
                }

                // 2) Outline (all lessons of the course)
                List<LessonRow> lessons = GetLessons(conn);
                if (lessonId <= 0 && lessons.Count > 0) lessonId = lessons[0].LessonId;
                foreach (LessonRow l in lessons) l.IsActive = (l.LessonId == lessonId);

                RptLessons.DataSource = lessons;
                RptLessons.DataBind();
                PnlNoLessons.Visible = lessons.Count == 0;

                // 3) Selected lesson
                LessonDetail lesson = lessonId > 0 ? GetLesson(conn) : null;
                PnlLessonView.Visible = lesson != null;
                PnlNoLesson.Visible = lesson == null;
                if (lesson != null) BindLesson(lesson, courseTitle);
            }
        }

        // ------------------------------------------------------------------
        // Queries
        // ------------------------------------------------------------------
        private bool TryGetCourse(NpgsqlConnection conn, out string title, out string topic, out string duration)
        {
            title = topic = duration = "";
            using (var cmd = new NpgsqlCommand(SqlCourse, conn))
            {
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.Parameters.AddWithValue("uid", userId);
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return false;
                    title = r.IsDBNull(0) ? "" : Convert.ToString(r["title"]);
                    topic = r.IsDBNull(1) ? "" : Convert.ToString(r["category"]);
                    duration = r.IsDBNull(2) ? "" : Convert.ToString(r["duration"]);
                    return true;
                }
            }
        }

        private List<LessonRow> GetLessons(NpgsqlConnection conn)
        {
            var list = new List<LessonRow>();
            using (var cmd = new NpgsqlCommand(SqlLessons, conn))
            {
                cmd.Parameters.AddWithValue("cid", courseId);
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new LessonRow
                        {
                            LessonId = Convert.ToInt32(r["lesson_id"]),
                            Title = r.IsDBNull(1) ? "(Untitled lesson)" : r.GetString(1),
                            HasVideo = FindBlock(ParseBlocks(r.IsDBNull(2) ? "" : r.GetString(2)), "video") != null
                        });
                    }
                }
            }
            return list;
        }

        private LessonDetail GetLesson(NpgsqlConnection conn)
        {
            using (var cmd = new NpgsqlCommand(SqlLesson, conn))
            {
                cmd.Parameters.AddWithValue("lid", lessonId);
                cmd.Parameters.AddWithValue("cid", courseId);   // also guarantees the lesson belongs to this course
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new LessonDetail
                    {
                        Title = r.IsDBNull(0) ? "(Untitled lesson)" : r.GetString(0),
                        Description = r.IsDBNull(1) ? "" : r.GetString(1),
                        VideoUrl = GetVideoUrl(r.IsDBNull(3) ? "" : r.GetString(3)),
                        QuizId = r.IsDBNull(2) ? (long?)null : Convert.ToInt64(r[2]),
                        ContentJson = r.IsDBNull(3) ? "" : r.GetString(3),
                        CreatedAt = r.GetDateTime(4)
                    };
                }
            }
        }

        // ------------------------------------------------------------------
        // Show the lesson. A component is only rendered when its data exists,
        // so empty fields never leave gaps in the layout.
        // ------------------------------------------------------------------
        private void BindLesson(LessonDetail lesson, string courseTitle)
        {
            LitLessonTitle.Text = Server.HtmlEncode(lesson.Title);
            LitLessonMeta.Text = "Part of " + Server.HtmlEncode(courseTitle) +
                " <span class=\"dot-sep\">&bull;</span> Created " +
                lesson.CreatedAt.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);

            // lesson_description (summary)
            bool hasSummary = !string.IsNullOrWhiteSpace(lesson.Description);
            PnlSummary.Visible = hasSummary;
            LitSummary.Text = Server.HtmlEncode(lesson.Description);

            // pop-up prefills (first load only, so a failed save keeps what the tutor typed)
            if (!IsPostBack)
            {
                TxtLessonTitle.Text = lesson.Title;
                TxtLessonDesc.Text = lesson.Description;
                TxtVideoUrl.Text = lesson.VideoUrl;
            }

            // video: stored as a block of type "video" inside lesson.lesson_content
            bool hasVideo = !string.IsNullOrWhiteSpace(lesson.VideoUrl);
            PnlVideo.Visible = hasVideo;
            BtnAddVideo.Visible = !hasVideo;
            if (hasVideo)
            {
                Uri uri;
                string source = Uri.TryCreate(lesson.VideoUrl, UriKind.Absolute, out uri) ? uri.Host : "Uploaded video";
                LitVideoSource.Text = Server.HtmlEncode(source);
            }

            // quiz_id
            bool hasQuiz = lesson.QuizId.HasValue;
            PnlAssessment.Visible = hasQuiz;
            LnkAddQuiz.Visible = !hasQuiz;
            if (hasQuiz)
                LitAssessmentMeta.Text = "Assessment Component <span class=\"dot-sep\">&bull;</span> Quiz #" + lesson.QuizId.Value;

            // lesson_content blocks of type "interactive" and "lab"
            List<Dictionary<string, object>> blocks = ParseBlocks(lesson.ContentJson);
            Dictionary<string, object> interactive = FindBlock(blocks, "interactive");
            Dictionary<string, object> lab = FindBlock(blocks, "lab");
            var drafts = new List<string>();

            PnlInteractive.Visible = interactive != null;
            LnkAddInteractive.Visible = interactive == null;
            if (interactive != null)
            {
                bool published = IsPublished(interactive);
                string title = Str(interactive, "title", "Interactive Object");
                string language = Str(interactive, "language", "");
                LitInteractiveTitle.Text = Server.HtmlEncode(title);
                LitInteractiveMeta.Text = "Interactive Object" +
                    (language.Length > 0 ? " <span class=\"dot-sep\">&bull;</span> Type: " + Server.HtmlEncode(language) + " Sandbox" : "") +
                    " <span class=\"dot-sep\">&bull;</span> Status: " + (published ? "Published" : "Draft");
                if (!published) drafts.Add(title);
            }

            PnlLab.Visible = lab != null;
            LnkAddLab.Visible = lab == null;
            if (lab != null)
            {
                bool published = IsPublished(lab);
                string title = Str(lab, "title", "Virtual Lab");
                string env = Str(lab, "environment", "");
                LitLabTitle.Text = Server.HtmlEncode(title);
                LblLabDraft.Visible = !published;
                LitLabMeta.Text = "Virtual Laboratory" +
                    (env.Length > 0 ? " <span class=\"dot-sep\">&bull;</span> Environment: " + Server.HtmlEncode(env) : "") +
                    (published ? "" : " <span class=\"dot-sep\">&bull;</span> Needs setup");
                if (!published) drafts.Add(title);
            }

            // links to assessmentBuilder.aspx for this lesson
            string suffix = "&lessonId=" + lessonId + "&courseId=" + courseId;
            LnkInteractive.HRef = "assessmentBuilder.aspx?tab=interactive" + suffix;
            LnkAddInteractive.HRef = LnkInteractive.HRef;
            LnkAssessment.HRef = "assessmentBuilder.aspx?tab=assessment" + suffix;
            LnkAddQuiz.HRef = LnkAssessment.HRef;
            LnkLab.HRef = "assessmentBuilder.aspx?tab=lab" + suffix;
            LnkAddLab.HRef = LnkLab.HRef;

            // empty state + add row
            bool anything = hasVideo || hasQuiz || interactive != null || lab != null;
            PnlEmpty.Visible = !anything;
            DivAddRow.Visible = !(hasVideo && hasQuiz && interactive != null && lab != null);

            // publish pop-up
            LitPublishLesson.Text = Server.HtmlEncode(lesson.Title);
            PnlDraftNotice.Visible = drafts.Count > 0;
            LitDraftList.Text = Server.HtmlEncode(string.Join(", ", drafts.ToArray()));
        }

        // ------------------------------------------------------------------
        // lesson_content helpers (JSON array of blocks)
        // ------------------------------------------------------------------
        private static List<Dictionary<string, object>> ParseBlocks(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<Dictionary<string, object>>();
            try
            {
                var list = new JavaScriptSerializer().Deserialize<List<Dictionary<string, object>>>(json);
                return list ?? new List<Dictionary<string, object>>();
            }
            catch (ArgumentException)
            {
                // Not valid JSON (or not an array): treat as "no blocks" instead of breaking the page.
                return new List<Dictionary<string, object>>();
            }
        }

        private static Dictionary<string, object> FindBlock(List<Dictionary<string, object>> blocks, string type)
        {
            foreach (var b in blocks)
            {
                object t;
                if (b != null && b.TryGetValue("type", out t) && string.Equals(Convert.ToString(t), type, StringComparison.OrdinalIgnoreCase))
                    return b;
            }
            return null;
        }

        private static string Str(Dictionary<string, object> block, string key, string fallback)
        {
            object v;
            if (block.TryGetValue(key, out v) && v != null)
            {
                string s = Convert.ToString(v).Trim();
                if (s.Length > 0) return s;
            }
            return fallback;
        }

        // Like ParseBlocks, but tells the caller when existing content could NOT be read,
        // so we never overwrite content we don't understand.
        private static bool TryParseBlocks(string json, out List<Dictionary<string, object>> blocks)
        {
            blocks = new List<Dictionary<string, object>>();
            if (string.IsNullOrWhiteSpace(json)) return true;     // empty = no blocks yet
            try
            {
                var list = new JavaScriptSerializer().Deserialize<List<Dictionary<string, object>>>(json);
                if (list == null) return false;
                blocks = list;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static string GetVideoUrl(string json)
        {
            Dictionary<string, object> video = FindBlock(ParseBlocks(json), "video");
            return video == null ? "" : Str(video, "url", "");
        }

        private static bool IsPublished(Dictionary<string, object> block)
        {
            object v;
            if (block.TryGetValue("published", out v) && v is bool) return (bool)v;
            return true;   // no flag stored = treat as published
        }

        // ------------------------------------------------------------------
        // Repeater helpers
        // ------------------------------------------------------------------
        protected string GetLessonUrl(object id)
        {
            return string.Format("courseBuilder.aspx?courseId={0}&lessonId={1}", courseId, id);
        }

        protected string GetLessonIcon(object hasVideo)
        {
            if ((bool)hasVideo)
                return "<svg class=\"lesson-icon\" viewBox=\"0 0 16 16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linejoin=\"round\"><rect x=\"1.5\" y=\"3.5\" width=\"9\" height=\"9\" rx=\"1.5\" /><path d=\"M10.5 7l4-2v6l-4-2z\" /></svg>";
            return "<svg class=\"lesson-icon\" viewBox=\"0 0 16 16\" fill=\"currentColor\"><path d=\"M3 1.5h6.5L13 5v9.5H3z\" /><path d=\"M9.5 1.5V5H13\" fill=\"none\" stroke=\"#fff\" stroke-width=\"1\" /><path d=\"M5 8h6M5 10.5h6\" stroke=\"#fff\" stroke-width=\"1\" /></svg>";
        }

        // ------------------------------------------------------------------
        // Pop-up save handlers (each updates the database, then reloads the page
        // so what you see is exactly what was saved)
        // ------------------------------------------------------------------
        protected void BtnSaveCourseInfo_Click(object sender, EventArgs e)
        {
            string title = TxtCourseTitle.Text.Trim();
            string topic = TxtCourseTopic.Text.Trim();
            int weeks;

            if (title.Length == 0 || !int.TryParse(TxtCourseDuration.Text, out weeks) || weeks <= 0)
            {
                ShowMessage("Please enter a course title and a duration in weeks.", true);
                return;
            }

            using (NpgsqlConnection conn = OpenConnection())
            using (var cmd = new NpgsqlCommand(SqlCourseUpdate, conn))
            {
                cmd.Parameters.AddWithValue("title", title);
                cmd.Parameters.AddWithValue("topic", topic);
                cmd.Parameters.AddWithValue("duration", weeks);   // adjust if your duration column is text
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.Parameters.AddWithValue("uid", userId);
                cmd.ExecuteNonQuery();
            }
            Response.Redirect(Request.RawUrl);
        }

        protected void BtnSaveDetails_Click(object sender, EventArgs e)
        {
            string title = TxtLessonTitle.Text.Trim();
            string desc = TxtLessonDesc.Text.Trim();

            if (lessonId <= 0 || title.Length == 0)
            {
                ShowMessage("Please enter a lesson title.", true);
                return;
            }

            using (NpgsqlConnection conn = OpenConnection())
            using (var cmd = new NpgsqlCommand(SqlLessonDetailsUpdate, conn))
            {
                cmd.Parameters.AddWithValue("title", title);
                // empty summary is stored as NULL so the student page skips it
                cmd.Parameters.AddWithValue("desc", desc.Length == 0 ? (object)DBNull.Value : desc);
                cmd.Parameters.AddWithValue("lid", lessonId);
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.ExecuteNonQuery();
            }
            Response.Redirect(Request.RawUrl);
        }

        protected void BtnConfirmUpload_Click(object sender, EventArgs e)
        {
            string url = TxtVideoUrl.Text.Trim();
            Uri uri;
            if (lessonId <= 0 || !Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                ShowMessage("Please paste a valid https video link.", true);
                return;
            }

            using (NpgsqlConnection conn = OpenConnection())
            using (NpgsqlTransaction tx = conn.BeginTransaction())
            {
                // Lock the lesson row so two saves can't overwrite each other's changes.
                string json;
                using (var read = new NpgsqlCommand(SqlLessonContentLock, conn, tx))
                {
                    read.Parameters.AddWithValue("lid", lessonId);
                    read.Parameters.AddWithValue("cid", courseId);
                    object value = read.ExecuteScalar();
                    if (value == null)
                    {
                        ShowMessage("This lesson could not be found.", true);
                        return;                      // transaction is rolled back on dispose
                    }
                    json = value == DBNull.Value ? "" : Convert.ToString(value);
                }

                List<Dictionary<string, object>> blocks;
                if (!TryParseBlocks(json, out blocks))
                {
                    ShowMessage("This lesson's content is in an unexpected format, so the video was not saved.", true);
                    return;
                }

                // Update the existing video block, or add one at the top of the lesson.
                Dictionary<string, object> video = FindBlock(blocks, "video");
                if (video == null)
                {
                    video = new Dictionary<string, object>();
                    video["type"] = "video";
                    blocks.Insert(0, video);
                }
                video["url"] = url;

                using (var write = new NpgsqlCommand(SqlLessonContentUpdate, conn, tx))
                {
                    write.Parameters.AddWithValue("content", new JavaScriptSerializer().Serialize(blocks));
                    write.Parameters.AddWithValue("lid", lessonId);
                    write.Parameters.AddWithValue("cid", courseId);
                    write.ExecuteNonQuery();
                }
                tx.Commit();
            }
            Response.Redirect(Request.RawUrl);
        }

        // The course outline has no modules table yet, so this just explains that.
        protected void BtnConfirmAddModule_Click(object sender, EventArgs e)
        {
            ShowMessage("Modules are not available yet. Lessons are listed in a single group for now.", true);
        }

        private void ShowMessage(string message, bool isError)
        {
            LblStatus.Text = Server.HtmlEncode(message);
            LblStatus.CssClass = isError ? "page-status error" : "page-status";
            LblStatus.Visible = true;
        }
    }
}