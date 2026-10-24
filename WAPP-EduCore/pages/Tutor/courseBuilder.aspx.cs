using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
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

        // While developing, the real database error is shown in the status box.
        // Set this to false before you deploy.
        private const bool ShowDatabaseErrors = true;

        // A course created from "Create New Course" starts with this title.
        // The title box is left empty for it so the tutor types a real one.
        private const string DraftTitle = "Untitled course";

        // ------------------------------------------------------------------
        // Course info rules
        // ------------------------------------------------------------------
        // courses.status values. The student pages must only show courses where status = 'published'.
        private const string StatusPublished = "published";
        private const string StatusDraft = "draft";

        private const int MaxWeeks = 52;                 // the Weeks drop-down offers 1 - 52
        private const int MaxHoursPerWeek = 40;          // the Hours drop-down offers 1 - 40

        // Course image upload: saved on the web server, the app-relative path goes into courses.course_img
        private const string ImageFolder = "~/uploads/courses/";
        private const int MaxImageBytes = 2 * 1024 * 1024;   // 2 MB

        // "4 weeks (2 hrs/week)"  <- the format stored in courses.duration
        private static readonly Regex DurationPattern = new Regex(
            @"^\s*(\d+)\s*weeks?\s*\(\s*(\d+)\s*hrs?\s*/\s*week\s*\)\s*$", RegexOptions.IgnoreCase);

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

        // Column ORDER matters: TryGetCourse reads these by position.
        private const string SqlCourse =
            "SELECT title, category, duration, course_level, course_img, status " +
            "FROM courses WHERE course_id = @cid AND user_id = @uid";

        // course_img is only replaced when a new image was uploaded (NULL keeps the current one).
        private const string SqlCourseUpdate =
            "UPDATE courses SET title = @title, category = @topic, duration = @duration, course_level = @level, " +
            "course_img = COALESCE(CAST(@img AS text), course_img), updated_at = now() " +
            "WHERE course_id = @cid AND user_id = @uid";

        private const string SqlCourseStatusUpdate =
            "UPDATE courses SET status = @status, updated_at = now() WHERE course_id = @cid AND user_id = @uid";

        private const string SqlLessons =
            "SELECT lesson_id, lesson_title, lesson_content::text " +
            "FROM " + LessonsTable + " WHERE course_id = @cid ORDER BY created_at, lesson_id";

        private const string SqlLesson =
            "SELECT lesson_title, lesson_description, quiz_id, lesson_content::text, created_at " +
            "FROM " + LessonsTable + " WHERE lesson_id = @lid AND course_id = @cid";

        // New lesson: empty content ([]), and only if the course belongs to the logged-in tutor.
        private const string SqlLessonInsert =
            "INSERT INTO " + LessonsTable + " (course_id, lesson_title, lesson_description, lesson_content) " +
            "SELECT CAST(@cid AS bigint), @title, CAST(@desc AS text), CAST('[]' AS jsonb) " +
            "WHERE EXISTS (SELECT 1 FROM courses WHERE course_id = @cid AND user_id = @uid) " +
            "RETURNING lesson_id";

        private const string SqlLessonDetailsUpdate =
            "UPDATE " + LessonsTable + " SET lesson_title = @title, lesson_description = @desc " +
            "WHERE lesson_id = @lid AND course_id = @cid";

        private const string SqlLessonContentLock =
            "SELECT lesson_content::text FROM " + LessonsTable + " " +
            "WHERE lesson_id = @lid AND course_id = @cid FOR UPDATE";

        // lesson_content is jsonb in your table.
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

        private string currentImage = "";      // courses.course_img as loaded for this request

        // Course values as loaded at the start of this request
        private string loadedTitle = "", loadedTopic = "", loadedLevel = "", loadedDuration = "", loadedImage = "", courseStatus = "";
        private int lessonCount;

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

            // The course list links here as: courseBuilder.aspx?courseId=1  (optionally &lessonId=5)
            if (!int.TryParse(Request.QueryString["courseId"], out courseId))
            {
                Response.Redirect(CourseListPage);
                return;
            }
            int.TryParse(Request.QueryString["lessonId"], out lessonId);

            // Week / hour choices (positive numbers only). Added once; kept in view state afterwards.
            if (!IsPostBack) FillDurationLists();

            try
            {
                LoadPage();
                if (!IsPostBack) ShowNoticeFromQuery();

                // courses.aspx "Edit" and "Create New Course" link here with &edit=1
                // -> open the course info pop-up straight away
                if (!IsPostBack && Request.QueryString["edit"] == "1")
                {
                    ClientScript.RegisterStartupScript(GetType(), "openCourseInfo",
                        "window.addEventListener('load', function () { openModal('modalCourseInfo'); });", true);
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                var pg = ex as PostgresException;
                string detail = pg != null ? pg.MessageText : ex.Message;
                ShowMessage("We couldn't load this course right now." +
                            (ShowDatabaseErrors ? " (" + detail + ")" : " Please try again."), true);
                PnlLessonView.Visible = false;
            }
        }

        private void LoadPage()
        {
            using (NpgsqlConnection conn = OpenConnection())
            {
                // 1) Course (must belong to the logged-in user)
                string courseTitle, topic, duration, level, image, status;
                if (!TryGetCourse(conn, out courseTitle, out topic, out duration, out level, out image, out status))
                {
                    Response.Redirect(CourseListPage);
                    return;
                }
                currentImage = image;
                loadedTitle = courseTitle;
                loadedTopic = topic;
                loadedLevel = level;
                loadedDuration = duration;
                loadedImage = image;
                courseStatus = status;
                LitPublishLesson.Text = Server.HtmlEncode(string.IsNullOrWhiteSpace(courseTitle) ? "this course" : courseTitle);
                ApplyStatusUi(status);
                LblCourseName.Text = Server.HtmlEncode(courseTitle);
                ShowCurrentImage(image);

                if (!IsPostBack)
                {
                    // A brand-new course still has the placeholder title: leave the box empty.
                    TxtCourseTitle.Text = courseTitle == DraftTitle ? "" : courseTitle;
                    TxtCourseTopic.Text = topic;

                    // "4 weeks (2 hrs/week)" -> Weeks = 4, Hours = 2
                    string weeks, hours;
                    ParseDuration(duration, out weeks, out hours);
                    SelectIfPresent(DdlCourseWeeks, weeks);
                    SelectIfPresent(DdlCourseHours, hours);
                    SelectIfPresent(DdlCourseLevel, (level ?? "").Trim().ToUpperInvariant());
                }

                // 2) Outline (all lessons of the course)
                List<LessonRow> lessons = GetLessons(conn);
                lessonCount = lessons.Count;
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
        private bool TryGetCourse(NpgsqlConnection conn, out string title, out string topic, out string duration,
                                  out string level, out string image, out string status)
        {
            title = topic = duration = level = image = status = "";
            using (var cmd = new NpgsqlCommand(SqlCourse, conn))
            {
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.Parameters.AddWithValue("uid", userId);
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return false;
                    title = r.IsDBNull(0) ? "" : Convert.ToString(r[0]);
                    topic = r.IsDBNull(1) ? "" : Convert.ToString(r[1]);
                    duration = r.IsDBNull(2) ? "" : Convert.ToString(r[2]);
                    level = r.IsDBNull(3) ? "" : Convert.ToString(r[3]);
                    image = r.IsDBNull(4) ? "" : Convert.ToString(r[4]);
                    status = r.IsDBNull(5) ? "" : Convert.ToString(r[5]);
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
        // Course info helpers: duration text, drop-downs and the course image
        // ------------------------------------------------------------------

        // Positive values only, so a negative or zero duration can never be chosen.
        private void FillDurationLists()
        {
            for (int w = 1; w <= MaxWeeks; w++)
                DdlCourseWeeks.Items.Add(new ListItem(w + (w == 1 ? " week" : " weeks"), w.ToString(CultureInfo.InvariantCulture)));

            for (int h = 1; h <= MaxHoursPerWeek; h++)
                DdlCourseHours.Items.Add(new ListItem(h + (h == 1 ? " hour" : " hours") + " per week", h.ToString(CultureInfo.InvariantCulture)));
        }

        // 4, 2  ->  "4 weeks (2 hrs/week)"
        private static string FormatDuration(int weeks, int hoursPerWeek)
        {
            return weeks + (weeks == 1 ? " week" : " weeks") +
                   " (" + hoursPerWeek + (hoursPerWeek == 1 ? " hr" : " hrs") + "/week)";
        }

        // "4 weeks (2 hrs/week)" -> weeks "4", hours "2". A plain number (older data, e.g. "8") fills weeks only.
        private static void ParseDuration(string text, out string weeks, out string hours)
        {
            weeks = hours = "";
            if (string.IsNullOrWhiteSpace(text)) return;

            Match m = DurationPattern.Match(text);
            if (m.Success)
            {
                weeks = m.Groups[1].Value;
                hours = m.Groups[2].Value;
                return;
            }

            int plain;
            if (int.TryParse(text.Trim(), out plain) && plain > 0) weeks = plain.ToString(CultureInfo.InvariantCulture);
        }

        private static void SelectIfPresent(DropDownList list, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            ListItem item = list.Items.FindByValue(value);
            if (item != null)
            {
                list.ClearSelection();
                item.Selected = true;
            }
        }

        // Shows the current course image above the upload box (only safe locations are shown).
        private void ShowCurrentImage(string path)
        {
            string url = SafeImageUrl(path);
            ImgCourseCurrent.Visible = url.Length > 0;
            if (url.Length > 0) ImgCourseCurrent.Src = url;
        }

        private string SafeImageUrl(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            if (path.StartsWith(ImageFolder, StringComparison.OrdinalIgnoreCase)) return ResolveUrl(path);

            Uri uri;
            if (Uri.TryCreate(path, UriKind.Absolute, out uri) && uri.Scheme == Uri.UriSchemeHttps) return uri.AbsoluteUri;
            return "";
        }

        // Checks the file's real content (not its name) and returns the matching extension, or null.
        private static string DetectImageExtension(byte[] head, int length)
        {
            if (length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return ".jpg";

            if (length >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47 &&
                head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A) return ".png";

            if (length >= 12 && head[0] == (byte)'R' && head[1] == (byte)'I' && head[2] == (byte)'F' && head[3] == (byte)'F' &&
                head[8] == (byte)'W' && head[9] == (byte)'E' && head[10] == (byte)'B' && head[11] == (byte)'P') return ".webp";

            return null;
        }

        // Saves the chosen image (if any). Returns true when there is nothing to save or it saved fine.
        private bool TrySaveCourseImage(out string savedPath, out string error)
        {
            savedPath = null;
            error = null;

            HttpPostedFile file = FileCourseImg.PostedFile;
            if (file == null || file.ContentLength == 0) return true;      // no new image chosen

            if (file.ContentLength > MaxImageBytes)
            {
                error = "The image must be 2 MB or smaller.";
                return false;
            }

            var head = new byte[12];
            int read = file.InputStream.Read(head, 0, head.Length);
            file.InputStream.Position = 0;

            string extension = DetectImageExtension(head, read);
            if (extension == null)
            {
                error = "Please choose a JPG, PNG or WEBP image.";
                return false;
            }

            try
            {
                string folder = Server.MapPath(ImageFolder);
                Directory.CreateDirectory(folder);

                // The uploaded file name is never used: a generated name avoids clashes and path tricks.
                string fileName = courseId + "-" + Guid.NewGuid().ToString("N") + extension;
                file.SaveAs(Path.Combine(folder, fileName));
                savedPath = ImageFolder + fileName;
                return true;
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                error = "The image could not be saved on the server.";
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                error = "The server is not allowed to save images in " + ImageFolder + " (check the folder permissions).";
                return false;
            }
        }

        // Deletes an image file we saved earlier. Only files inside our upload folder are ever touched.
        private void DeleteStoredImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(ImageFolder, StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                string folder = Path.GetFullPath(Server.MapPath(ImageFolder));
                string full = Path.GetFullPath(Server.MapPath(path));
                if (full.StartsWith(folder, StringComparison.OrdinalIgnoreCase) && File.Exists(full)) File.Delete(full);
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
                    System.Diagnostics.Debug.WriteLine(ex.ToString());      // an old file left behind is harmless
                else
                    throw;
            }
        }

        // Shows an error INSIDE the course info pop-up and opens it again.
        // (A file chosen before the error has to be chosen again - browsers clear it on reload.)
        private void ReopenCourseInfoDialog(string message)
        {
            string script =
                "window.addEventListener('load', function () {" +
                "  var box = document.getElementById('courseInfoError');" +
                "  if (box) { box.textContent = " + new JavaScriptSerializer().Serialize(message) + "; box.style.display = 'block'; }" +
                "  openModal('modalCourseInfo');" +
                "});";
            ClientScript.RegisterStartupScript(GetType(), "courseInfoError", script, true);
        }

        // ------------------------------------------------------------------
        // Pop-up save handlers (each updates the database, then reloads the page
        // so what you see is exactly what was saved)
        // ------------------------------------------------------------------
        protected void BtnSaveCourseInfo_Click(object sender, EventArgs e)
        {
            string title = TxtCourseTitle.Text.Trim();
            string topic = TxtCourseTopic.Text.Trim();
            string level = DdlCourseLevel.SelectedValue;
            int weeks, hours;

            if (title.Length == 0)
            {
                ReopenCourseInfoDialog("Please enter a course title.");
                return;
            }
            if (level != "IGCSE" && level != "SPM")
            {
                ReopenCourseInfoDialog("Please choose a course level (IGCSE or SPM).");
                return;
            }
            if (!int.TryParse(DdlCourseWeeks.SelectedValue, out weeks) || weeks < 1 || weeks > MaxWeeks)
            {
                ReopenCourseInfoDialog("Please choose how many weeks the course runs.");
                return;
            }
            if (!int.TryParse(DdlCourseHours.SelectedValue, out hours) || hours < 1 || hours > MaxHoursPerWeek)
            {
                ReopenCourseInfoDialog("Please choose how many hours per week the course needs.");
                return;
            }

            // Optional new course image
            string newImage, imageError;
            if (!TrySaveCourseImage(out newImage, out imageError))
            {
                ReopenCourseInfoDialog(imageError);
                return;
            }

            string duration = FormatDuration(weeks, hours);       // e.g. "4 weeks (2 hrs/week)"

            try
            {
                int rows;
                using (NpgsqlConnection conn = OpenConnection())
                using (var cmd = new NpgsqlCommand(SqlCourseUpdate, conn))
                {
                    cmd.Parameters.AddWithValue("title", title);
                    cmd.Parameters.AddWithValue("topic", topic);
                    cmd.Parameters.AddWithValue("duration", duration);
                    cmd.Parameters.AddWithValue("level", level);
                    cmd.Parameters.AddWithValue("img", newImage == null ? (object)DBNull.Value : newImage);
                    cmd.Parameters.AddWithValue("cid", courseId);
                    cmd.Parameters.AddWithValue("uid", userId);
                    rows = cmd.ExecuteNonQuery();
                }

                if (rows == 0)
                {
                    DeleteStoredImage(newImage);                  // nothing was saved: don't leave the new file behind
                    ReopenCourseInfoDialog("This course could not be found.");
                    return;
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                DeleteStoredImage(newImage);

                var pg = ex as PostgresException;
                string detail = pg != null ? pg.MessageText : ex.Message;
                ReopenCourseInfoDialog("We couldn't save the course." + (ShowDatabaseErrors ? " (" + detail + ")" : " Please try again."));
                return;
            }

            // A new image replaced the old one: remove the old file
            if (newImage != null) DeleteStoredImage(currentImage);

            // Drop "&edit=1" so the course info pop-up doesn't reopen after saving.
            Response.Redirect("courseBuilder.aspx?courseId=" + courseId + (lessonId > 0 ? "&lessonId=" + lessonId : ""));
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

        // "Add New Lesson": creates an empty lesson (lesson_content = []) and opens it in the editor.
        protected void BtnConfirmAddModule_Click(object sender, EventArgs e)
        {
            string title = TxtModuleTitle.Text.Trim();
            string desc = TxtModuleDesc.Text.Trim();

            if (title.Length == 0)
            {
                ShowMessage("Please enter a lesson title.", true);
                return;
            }

            object newLessonId;
            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                using (var cmd = new NpgsqlCommand(SqlLessonInsert, conn))
                {
                    cmd.Parameters.AddWithValue("cid", courseId);
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.Parameters.AddWithValue("title", title);
                    cmd.Parameters.AddWithValue("desc", desc.Length == 0 ? (object)DBNull.Value : desc);
                    newLessonId = cmd.ExecuteScalar();
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                var pg = ex as PostgresException;
                string detail = pg != null ? pg.MessageText : ex.Message;
                ShowMessage("We couldn't add the lesson." + (ShowDatabaseErrors ? " (" + detail + ")" : " Please try again."), true);
                return;
            }

            if (newLessonId == null)
            {
                ShowMessage("This course could not be found.", true);
                return;
            }

            // Open the new lesson straight away
            Response.Redirect("courseBuilder.aspx?courseId=" + courseId + "&lessonId=" + newLessonId);
        }

        // ------------------------------------------------------------------
        // Publishing: the draft / published state is just courses.status in the SAME table.
        // ------------------------------------------------------------------
        private static string NormalizeStatus(string status)
        {
            string s = (status ?? "").Trim().ToLowerInvariant().Replace(' ', '_');
            if (s == StatusPublished) return StatusPublished;
            if (s == "pending_review" || s == "pending") return "pending_review";
            return StatusDraft;
        }

        // Status badge next to the course name, and the publish buttons
        private void ApplyStatusUi(string status)
        {
            string key = NormalizeStatus(status);
            bool published = key == StatusPublished;

            LblCourseStatus.Text = published ? "Published" : key == "pending_review" ? "Pending review" : "Draft";
            LblCourseStatus.CssClass = "status-pill " + (published ? "published" : key == "pending_review" ? "pending" : "draft");

            BtnPublishCourse.Text = published ? "Published" : "Publish Course";
            BtnPublishCourse.Enabled = !published;
            BtnPublishModule.Text = published ? "Published" : "Publish Module Content";
            BtnPublishModule.Enabled = !published;
        }

        // Message shown after a publish / draft change (the page is reloaded with ?notice=...)
        private void ShowNoticeFromQuery()
        {
            string notice = Request.QueryString["notice"];
            if (notice == "published")
                ShowMessage("Course published. Students can now see it.", false);
            else if (notice == "draft")
                ShowMessage("Course saved as a draft. It is hidden from students.", false);
        }

        private string BuilderUrl(string notice)
        {
            return "courseBuilder.aspx?courseId=" + courseId +
                   (lessonId > 0 ? "&lessonId=" + lessonId : "") +
                   "&notice=" + notice;
        }

        private bool SetCourseStatus(string newStatus)
        {
            try
            {
                int rows;
                using (NpgsqlConnection conn = OpenConnection())
                using (var cmd = new NpgsqlCommand(SqlCourseStatusUpdate, conn))
                {
                    cmd.Parameters.AddWithValue("status", newStatus);
                    cmd.Parameters.AddWithValue("cid", courseId);
                    cmd.Parameters.AddWithValue("uid", userId);
                    rows = cmd.ExecuteNonQuery();
                }

                if (rows == 0)
                {
                    ShowMessage("This course could not be found.", true);
                    return false;
                }
                return true;
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                var pg = ex as PostgresException;
                string detail = pg != null ? pg.MessageText : ex.Message;
                ShowMessage("We couldn't update the course status." + (ShowDatabaseErrors ? " (" + detail + ")" : " Please try again."), true);
                return false;
            }
        }

        // "Publish now" in the pop-up (opened by Publish Course and Publish Module Content)
        protected void BtnConfirmPublish_Click(object sender, EventArgs e)
        {
            // Validator: ensure all required course columns and lessons are filled before publishing
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(loadedTitle) || loadedTitle == DraftTitle)
                missing.Add("a course title");

            if (string.IsNullOrWhiteSpace(loadedTopic))
                missing.Add("a course topic/category");

            if (string.IsNullOrWhiteSpace(loadedLevel))
                missing.Add("a course level (IGCSE or SPM)");

            if (string.IsNullOrWhiteSpace(loadedDuration))
                missing.Add("the course duration");

            if (string.IsNullOrWhiteSpace(loadedImage))
                missing.Add("a course image thumbnail");

            if (lessonCount == 0)
                missing.Add("at least one lesson in the course syllabus");

            // If anything is missing, block publication and list what is required
            if (missing.Count > 0)
            {
                // Close the modal via client script or show an explicit error message
                ShowMessage("Cannot publish course. Please provide: " + string.Join(", ", missing.ToArray()) +
                            ". (You can update course info via 'Edit course info'.)", true);
                return;
            }

            // All validations passed, proceed to publish
            if (!SetCourseStatus(StatusPublished)) return;
            Response.Redirect(BuilderUrl("published"));
        }

        // "Save as Draft": hides the course from students again
        protected void BtnSaveDraft_Click(object sender, EventArgs e)
        {
            if (!SetCourseStatus(StatusDraft)) return;
            Response.Redirect(BuilderUrl("draft"));
        }

        private void ShowMessage(string message, bool isError)
        {
            LblStatus.Text = Server.HtmlEncode(message);
            LblStatus.CssClass = isError ? "page-status error" : "page-status";
            LblStatus.Visible = true;
        }
    }
}