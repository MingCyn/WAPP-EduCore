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
        // ---------- Configuration ----------
        private const string ConnectionName = "SupabasePostgres";
        private const string SessionUserKey = "UserID";
        private const string LoginPage = "~/auth/login.aspx";
        private const string CourseListPage = "courses.aspx";

        private const string LessonsTable = "public.lesson";
        private const bool ShowDatabaseErrors = true;
        private const string DraftTitle = "Untitled course";

        private const string StatusPublished = "published";
        private const string StatusDraft = "draft";

        private const int MaxWeeks = 52;
        private const int MaxHoursPerWeek = 40;

        // Course image
        private const string ImageFolder = "~/uploads/courses/";
        private const int MaxImageBytes = 2 * 1024 * 1024;   // 2 MB

        // Image added inside a lesson (one per lesson)
        private const string LessonImageFolder = "~/uploads/lessons/";
        private const int MaxLessonImageBytes = 2 * 1024 * 1024;   // 2 MB

        // Lesson content blocks
        private const int MaxBlockChars = 10000;
        private const int MaxHeadingChars = 200;
        private const int MaxCaptionChars = 200;
        private const int MaxListItems = 100;
        private const int MaxListItemChars = 500;

        private static readonly Regex DurationPattern = new Regex(
            @"^\s*(\d+)\s*weeks?\s*\(\s*(\d+)\s*hrs?\s*/\s*week\s*\)\s*$", RegexOptions.IgnoreCase);

        // ---------- SQL ----------
        private const string SqlCourse =
            "SELECT title, category, duration, course_level, course_img, status, course_description, reference " +
            "FROM courses WHERE course_id = @cid AND user_id = @uid";

        private const string SqlCourseUpdate =
            "UPDATE courses SET title = @title, category = @topic, duration = @duration, course_level = @level, " +
            "course_description = @desc, reference = CAST(@reference AS jsonb), " +
            "course_img = COALESCE(CAST(@img AS text), course_img), updated_at = now() " +
            "WHERE course_id = @cid AND user_id = @uid";

        private const string SqlCourseStatusUpdate =
            "UPDATE courses SET status = @status, updated_at = now() WHERE course_id = @cid AND user_id = @uid";

        private const string SqlEnrolledCount =
            "SELECT COUNT(*) FROM enrollment WHERE course_id = @cid";

        private const string SqlLessons =
            "SELECT lesson_id, lesson_title, lesson_content::text " +
            "FROM " + LessonsTable + " WHERE course_id = @cid ORDER BY created_at, lesson_id";

        private const string SqlLesson =
            "SELECT lesson_title, lesson_description, quiz_id, lesson_content::text, created_at " +
            "FROM " + LessonsTable + " WHERE lesson_id = @lid AND course_id = @cid";

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

        private const string SqlLessonContentUpdate =
            "UPDATE " + LessonsTable + " SET lesson_content = CAST(@content AS jsonb) " +
            "WHERE lesson_id = @lid AND course_id = @cid";

        private const string SqlLessonContentForDelete =
            "SELECT l.lesson_content::text FROM " + LessonsTable + " l " +
            "WHERE l.lesson_id = @lid AND l.course_id = @cid " +
            "AND EXISTS (SELECT 1 FROM courses WHERE course_id = @cid AND user_id = @uid)";

        private const string SqlLessonDelete =
            "DELETE FROM " + LessonsTable + " " +
            "WHERE lesson_id = @lid AND course_id = @cid " +
            "AND EXISTS (SELECT 1 FROM courses WHERE course_id = @cid AND user_id = @uid)";

        // ---------- Types ----------
        public class LessonRow
        {
            public int LessonId { get; set; }
            public string Title { get; set; }
            public bool HasVideo { get; set; }
            public bool IsActive { get; set; }
        }

        // One row of the "Lesson content" list: a video, heading, paragraph, list, image ...
        public class BlockRow
        {
            public int Index { get; set; }            // position inside the lesson_content array
            public string TypeKey { get; set; }       // the block's "type" in the JSON
            public string TypeLabel { get; set; }
            public string Preview { get; set; }
            public string ThumbUrl { get; set; }
            public bool CanEdit { get; set; }
            public bool CanMoveUp { get; set; }
            public bool CanMoveDown { get; set; }
        }

        private class LessonDetail
        {
            public string Title;
            public string Description;
            public long? QuizId;
            public string ContentJson;
            public DateTime CreatedAt;
        }

        // ---------- State ----------
        protected int CourseIdValue;

        private int userId;
        private int courseId;
        private int lessonId;
        private string currentImage = "";
        private string currentContentJson = "";

        private string loadedTitle = "", loadedTopic = "", loadedLevel = "", loadedDuration = "", loadedImage = "", courseStatus = "";
        private int lessonCount;
        private readonly List<string> emptyLessons = new List<string>();

        // ---------- Helpers ----------
        private bool TryGetUserId(out int id)
        {
            return int.TryParse(Convert.ToString(Session["UserID"]), out id) && id > 0;
        }

        private static NpgsqlConnection OpenConnection()
        {
            var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString);
            conn.Open();
            return conn;
        }

        private static string DbErrorText(NpgsqlException ex)
        {
            var pg = ex as PostgresException;
            string detail = pg != null ? pg.MessageText : ex.Message;
            return ShowDatabaseErrors ? " (" + detail + ")" : " Please try again.";
        }

        // ---------- Page load ----------
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserRole"] == null ||
                !Session["UserRole"].ToString().Equals("Tutor", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect(LoginPage, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!TryGetUserId(out userId))
            {
                Response.Redirect(LoginPage, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            int.TryParse(Request.QueryString["courseId"], out courseId);
            int.TryParse(Request.QueryString["lessonId"], out lessonId);

            if (courseId <= 0)
            {
                Response.Redirect(CourseListPage, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            CourseIdValue = courseId;

            if (!IsPostBack)
            {
                FillDurationLists();
                ShowNoticeFromQuery();
            }

            LoadPage();

            if (!IsPostBack && Request.QueryString["addLesson"] == "1")
            {
                ClientScript.RegisterStartupScript(GetType(), "openAddLesson",
                    "window.addEventListener('load', function () { openModal('modalAddModule'); });", true);
            }

            if (!IsPostBack && Request.QueryString["edit"] == "1")
            {
                ClientScript.RegisterStartupScript(GetType(), "openCourseInfo",
                    "window.addEventListener('load', function () { openModal('modalCourseInfo'); });", true);
            }
        }

        private void LoadPage()
        {
            using (NpgsqlConnection conn = OpenConnection())
            {
                string courseTitle, topic, duration, level, image, status, description, referenceJson;
                if (!TryGetCourse(conn, out courseTitle, out topic, out duration, out level, out image, out status, out description, out referenceJson))
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
                courseStatus = NormalizeStatus(status);

                string displayName = string.IsNullOrWhiteSpace(courseTitle) ? "this course" : courseTitle;
                LitPublishLesson.Text = Server.HtmlEncode(displayName);
                LitUnpublishName.Text = Server.HtmlEncode(displayName);
                LblCourseName.Text = Server.HtmlEncode(courseTitle);
                ShowCurrentImage(image);

                ApplyStatusUi(courseStatus == StatusPublished ? CountEnrolled(conn) : -1);

                if (!IsPostBack) FillCourseInfoForm(courseTitle, topic, duration, level, description, referenceJson);

                List<LessonRow> lessons = GetLessons(conn);
                lessonCount = lessons.Count;
                if (lessonId <= 0 && lessons.Count > 0) lessonId = lessons[0].LessonId;
                foreach (LessonRow l in lessons) l.IsActive = (l.LessonId == lessonId);

                RptLessons.DataSource = lessons;
                RptLessons.DataBind();
                PnlNoLessons.Visible = lessons.Count == 0;

                LessonDetail lesson = lessonId > 0 ? GetLesson(conn) : null;
                PnlLessonView.Visible = lesson != null;
                PnlNoLesson.Visible = lesson == null;
                if (lesson != null) BindLesson(lesson, courseTitle);
            }
        }

        // Fills the "Edit course info" dialog the first time the page loads
        private void FillCourseInfoForm(string courseTitle, string topic, string duration, string level, string description, string referenceJson)
        {
            TxtCourseTitle.Text = courseTitle == DraftTitle ? "" : courseTitle;
            TxtCourseTopic.Text = topic;

            var json = new JavaScriptSerializer();

            var outcomes = new List<string>();
            if (!string.IsNullOrWhiteSpace(description))
            {
                foreach (string line in description.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length > 0) outcomes.Add(trimmed);
                }
            }
            RegisterRowFiller("loadOutcomes", "outcomesContainer", "addOutcomeInput", json.Serialize(outcomes));

            var refs = new List<string>();
            if (!string.IsNullOrWhiteSpace(referenceJson))
            {
                try
                {
                    var parsed = json.Deserialize<List<string>>(referenceJson);
                    if (parsed != null) refs = parsed;
                }
                catch (ArgumentException) { }
                catch (InvalidOperationException) { }
            }
            RegisterRowFiller("loadReferences", "referencesContainer", "addReferenceInput", json.Serialize(refs));

            string weeks, hours;
            ParseDuration(duration, out weeks, out hours);
            SelectIfPresent(DdlCourseWeeks, weeks);
            SelectIfPresent(DdlCourseHours, hours);
            SelectIfPresent(DdlCourseLevel, (level ?? "").Trim().ToUpperInvariant());
        }

        // Clears a container and adds one input row per value (or one empty row)
        private void RegisterRowFiller(string key, string containerId, string addFunction, string jsArray)
        {
            string script =
                "window.addEventListener('load', function () {" +
                "  var c = document.getElementById('" + containerId + "');" +
                "  if (!c) return;" +
                "  c.innerHTML = '';" +
                "  var items = " + jsArray + ";" +
                "  if (items.length > 0) items.forEach(function (v) { " + addFunction + "(v); });" +
                "  else " + addFunction + "();" +
                "});";
            ClientScript.RegisterStartupScript(GetType(), key, script, true);
        }

        private bool TryGetCourse(NpgsqlConnection conn, out string title, out string topic, out string duration,
                                  out string level, out string image, out string status, out string description, out string referenceJson)
        {
            title = topic = duration = level = image = status = description = referenceJson = "";
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
                    description = r.IsDBNull(6) ? "" : Convert.ToString(r[6]);
                    referenceJson = r.IsDBNull(7) ? "" : Convert.ToString(r[7]);
                    return true;
                }
            }
        }

        private int CountEnrolled(NpgsqlConnection conn)
        {
            try
            {
                using (var cmd = new NpgsqlCommand(SqlEnrolledCount, conn))
                {
                    cmd.Parameters.AddWithValue("cid", courseId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (PostgresException ex)
            {
                System.Diagnostics.Debug.WriteLine("Enrollment count unavailable: " + ex.Message);
                return -1;
            }
        }

        // Counts heading / paragraph / list / image / video blocks (interactive and lab cards are not "content")
        private static int CountContentBlocks(List<Dictionary<string, object>> blocks)
        {
            int n = 0;
            foreach (var b in blocks)
                if (b != null && !IsSeparateCard(BlockTypeOf(b))) n++;
            return n;
        }

        private List<LessonRow> GetLessons(NpgsqlConnection conn)
        {
            var list = new List<LessonRow>();
            emptyLessons.Clear();
            using (var cmd = new NpgsqlCommand(SqlLessons, conn))
            {
                cmd.Parameters.AddWithValue("cid", courseId);
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string title = r.IsDBNull(1) ? "(Untitled lesson)" : r.GetString(1);
                        var blocks = ParseBlocks(r.IsDBNull(2) ? "" : r.GetString(2));

                        if (CountContentBlocks(blocks) == 0) emptyLessons.Add(title);

                        list.Add(new LessonRow
                        {
                            LessonId = Convert.ToInt32(r["lesson_id"]),
                            Title = title,
                            HasVideo = FindBlock(blocks, "video") != null
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
                cmd.Parameters.AddWithValue("cid", courseId);
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new LessonDetail
                    {
                        Title = r.IsDBNull(0) ? "(Untitled lesson)" : r.GetString(0),
                        Description = r.IsDBNull(1) ? "" : r.GetString(1),
                        QuizId = r.IsDBNull(2) ? (long?)null : Convert.ToInt64(r[2]),
                        ContentJson = r.IsDBNull(3) ? "" : r.GetString(3),
                        CreatedAt = r.GetDateTime(4)
                    };
                }
            }
        }

        private void BindLesson(LessonDetail lesson, string courseTitle)
        {
            currentContentJson = lesson.ContentJson;

            LitLessonTitle.Text = Server.HtmlEncode(lesson.Title);
            LitLessonMeta.Text = "Part of " + Server.HtmlEncode(courseTitle) +
                " <span class=\"dot-sep\">&bull;</span> Created " +
                lesson.CreatedAt.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            LitDeleteLessonName.Text = Server.HtmlEncode(lesson.Title);

            PnlSummary.Visible = !string.IsNullOrWhiteSpace(lesson.Description);
            LitSummary.Text = Server.HtmlEncode(lesson.Description);

            List<Dictionary<string, object>> blocks = ParseBlocks(lesson.ContentJson);
            Dictionary<string, object> video = FindBlock(blocks, "video");
            bool hasVideo = video != null && Str(video, "url", "").Length > 0;

            if (!IsPostBack)
            {
                TxtLessonTitle.Text = lesson.Title;
                TxtLessonDesc.Text = lesson.Description;
                if (hasVideo)
                {
                    TxtVideoUrl.Text = Str(video, "url", "");
                    TxtVideoCaption.Text = Str(video, "caption", "");
                }
            }

            BtnAddVideo.Visible = !hasVideo;
            BtnAddImage.Visible = FindBlock(blocks, "image") == null;

            List<BlockRow> rows = BuildBlockRows(blocks);
            RptBlocks.DataSource = rows;
            RptBlocks.DataBind();
            PnlNoBlocks.Visible = rows.Count == 0;

            bool hasQuiz = lesson.QuizId.HasValue;
            PnlAssessment.Visible = hasQuiz;
            LnkAddQuiz.Visible = !hasQuiz;
            if (hasQuiz)
                LitAssessmentMeta.Text = "Assessment Component <span class=\"dot-sep\">&bull;</span> Quiz #" + lesson.QuizId.Value;

            Dictionary<string, object> interactive = FindBlock(blocks, "interactive");
            Dictionary<string, object> lab = FindBlock(blocks, "lab");
            var drafts = new List<string>();

            PnlInteractive.Visible = interactive != null;
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

            string suffix = "&lessonId=" + lessonId + "&courseId=" + courseId;
            LnkInteractive.HRef = "assessmentBuilder.aspx?tab=interactive" + suffix;
            LnkAssessment.HRef = "assessmentBuilder.aspx?tab=assessment" + suffix;
            LnkAddQuiz.HRef = LnkAssessment.HRef;
            LnkLab.HRef = "assessmentBuilder.aspx?tab=lab" + suffix;

            PnlDraftNotice.Visible = drafts.Count > 0;
            LitDraftList.Text = Server.HtmlEncode(string.Join(", ", drafts.ToArray()));
        }

        // ---------- JSON block helpers ----------
        private static List<Dictionary<string, object>> ParseBlocks(string json)
        {
            List<Dictionary<string, object>> blocks;
            return TryParseBlocks(json, out blocks) ? blocks : new List<Dictionary<string, object>>();
        }

        private static bool TryParseBlocks(string json, out List<Dictionary<string, object>> blocks)
        {
            blocks = new List<Dictionary<string, object>>();
            if (string.IsNullOrWhiteSpace(json)) return true;
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
            catch (InvalidOperationException)
            {
                return false;
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

        private static string RawStr(Dictionary<string, object> block, string key)
        {
            object v;
            return block.TryGetValue(key, out v) && v != null ? Convert.ToString(v) : "";
        }

        private static object GetValue(Dictionary<string, object> block, string key)
        {
            object v;
            return block.TryGetValue(key, out v) ? v : null;
        }

        private static List<string> ToStringList(object value)
        {
            var result = new List<string>();
            if (value == null || value is string) return result;
            var sequence = value as System.Collections.IEnumerable;
            if (sequence == null) return result;
            foreach (object item in sequence) result.Add(Convert.ToString(item) ?? "");
            return result;
        }

        private static bool IsPublished(Dictionary<string, object> block)
        {
            object v;
            if (block.TryGetValue("published", out v) && v is bool) return (bool)v;
            return true;
        }

        private static string BlockTypeOf(Dictionary<string, object> block)
        {
            return block == null ? "" : Str(block, "type", "").ToLowerInvariant();
        }

        private static bool IsSeparateCard(string type)
        {
            return type == "interactive" || type == "lab";
        }

        private static bool IsEditableType(string type)
        {
            return type == "subheading" || type == "paragraph" || type == "list" || type == "image" || type == "video";
        }

        private static string TypeLabel(string type)
        {
            switch (type)
            {
                case "subheading": return "Heading";
                case "paragraph": return "Paragraph";
                case "list": return "List";
                case "image": return "Image";
                case "video": return "Video";
                default: return type.Length == 0 ? "Content" : char.ToUpperInvariant(type[0]) + type.Substring(1);
            }
        }

        private static string Truncate(string text, int max)
        {
            if (text == null) return "";
            text = Regex.Replace(text.Trim(), @"\s+", " ");
            return text.Length <= max ? text : text.Substring(0, max).TrimEnd() + "...";
        }

        private static string PreviewFor(string type, Dictionary<string, object> block)
        {
            switch (type)
            {
                case "subheading":
                    return Truncate(Str(block, "text", ""), 180);

                case "paragraph":
                    return Truncate(Str(block, "content", ""), 180);

                case "list":
                    {
                        List<string> items = ToStringList(GetValue(block, "items"));
                        if (items.Count == 0) return "(empty list)";
                        return items.Count + (items.Count == 1 ? " item: " : " items: ") + Truncate(items[0], 120);
                    }

                case "image":
                case "video":
                    {
                        string caption = Str(block, "caption", "");
                        return caption.Length > 0 ? Truncate(caption, 180) : Truncate(Str(block, "url", ""), 180);
                    }

                default:
                    return "";
            }
        }

        private static string ExtractYouTubeId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            try
            {
                var uri = new Uri(url);
                if (uri.Host.Contains("youtube.com"))
                    return HttpUtility.ParseQueryString(uri.Query)["v"] ?? "";
                if (uri.Host.Contains("youtu.be"))
                    return uri.AbsolutePath.Trim('/');
            }
            catch (UriFormatException) { }
            return "";
        }

        private string ThumbFor(string type, Dictionary<string, object> block)
        {
            if (type == "image") return SafeBlockImageUrl(Str(block, "url", ""));

            if (type == "video")
            {
                string url = Str(block, "url", "");
                if (url.Contains("youtube.com") || url.Contains("youtu.be"))
                {
                    string id = ExtractYouTubeId(url);
                    if (id.Length > 0) return "https://img.youtube.com/vi/" + id + "/hqdefault.jpg";
                }
            }
            return "";
        }

        // Lesson images are stored as "~/uploads/lessons/xxx.jpg"; turn that into a URL the browser understands
        private string SafeBlockImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            url = url.Trim();

            if (url.StartsWith("~/") || url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                return ResolveUrl(url.StartsWith("~/") ? url : "~" + url);

            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri) && uri.Scheme == Uri.UriSchemeHttps) return uri.AbsoluteUri;
            return "";
        }

        private List<BlockRow> BuildBlockRows(List<Dictionary<string, object>> blocks)
        {
            var rows = new List<BlockRow>();
            var positions = new List<int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] == null) continue;
                if (IsSeparateCard(BlockTypeOf(blocks[i]))) continue;
                positions.Add(i);
            }

            for (int k = 0; k < positions.Count; k++)
            {
                int index = positions[k];
                string type = BlockTypeOf(blocks[index]);
                rows.Add(new BlockRow
                {
                    Index = index,
                    TypeKey = type,
                    TypeLabel = TypeLabel(type),
                    Preview = PreviewFor(type, blocks[index]),
                    ThumbUrl = ThumbFor(type, blocks[index]),
                    CanEdit = IsEditableType(type),
                    CanMoveUp = k > 0,
                    CanMoveDown = k < positions.Count - 1
                });
            }
            return rows;
        }

        // Reads lesson_content under a row lock, lets "change" edit it, and writes it back. Returns an error message or null.
        private string ModifyBlocks(Func<List<Dictionary<string, object>>, string> change)
        {
            if (lessonId <= 0) return "Please select a lesson first.";

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                using (NpgsqlTransaction tx = conn.BeginTransaction())
                {
                    string json;
                    using (var read = new NpgsqlCommand(SqlLessonContentLock, conn, tx))
                    {
                        read.Parameters.AddWithValue("lid", lessonId);
                        read.Parameters.AddWithValue("cid", courseId);
                        object value = read.ExecuteScalar();
                        if (value == null) return "This lesson could not be found.";
                        json = value == DBNull.Value ? "" : Convert.ToString(value);
                    }

                    List<Dictionary<string, object>> blocks;
                    if (!TryParseBlocks(json, out blocks))
                        return "This lesson's content is in an unexpected format, so nothing was changed.";

                    string error = change(blocks);
                    if (error != null) return error;

                    using (var write = new NpgsqlCommand(SqlLessonContentUpdate, conn, tx))
                    {
                        write.Parameters.AddWithValue("content", new JavaScriptSerializer().Serialize(blocks));
                        write.Parameters.AddWithValue("lid", lessonId);
                        write.Parameters.AddWithValue("cid", courseId);
                        write.ExecuteNonQuery();
                    }
                    tx.Commit();
                    return null;
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return "We couldn't save the lesson." + DbErrorText(ex);
            }
        }

        private void ReloadLesson()
        {
            Response.Redirect("courseBuilder.aspx?courseId=" + courseId + (lessonId > 0 ? "&lessonId=" + lessonId : ""));
        }

        // ---------- Heading & paragraph ----------
        protected void BtnSaveText_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0)
            {
                ShowMessage("Please select a lesson first.", true);
                return;
            }

            string mode = HdnTextMode.Value;
            if (mode != "both" && mode != "heading" && mode != "paragraph") mode = "both";

            int index;
            if (!int.TryParse(HdnTextIndex.Value, out index)) index = -1;
            if (mode == "both") index = -1;
            else if (index < 0)
            {
                ReopenTextDialog("That block can't be edited any more. Please reload the page.");
                return;
            }

            string heading = Regex.Replace((TxtHeading.Text ?? "").Trim(), @"\s+", " ");
            string paragraph = (TxtParagraph.Text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();

            string error = null;
            if (mode == "both" && heading.Length == 0 && paragraph.Length == 0) error = "Please enter a heading, a paragraph, or both.";
            else if (mode == "heading" && heading.Length == 0) error = "Please enter the heading.";
            else if (mode == "paragraph" && paragraph.Length == 0) error = "Please enter the paragraph.";
            else if (heading.Length > MaxHeadingChars) error = "A heading should be " + MaxHeadingChars + " characters or fewer.";
            else if (paragraph.Length > MaxBlockChars) error = "That paragraph is too long. Please keep it under " + MaxBlockChars + " characters, or split it into two paragraphs.";

            if (error != null)
            {
                ReopenTextDialog(error);
                return;
            }

            error = ModifyBlocks(blocks =>
            {
                if (mode == "both")
                {
                    if (heading.Length > 0)
                    {
                        var h = new Dictionary<string, object>();
                        h["type"] = "subheading";
                        h["text"] = heading;
                        blocks.Add(h);
                    }
                    if (paragraph.Length > 0)
                    {
                        var p = new Dictionary<string, object>();
                        p["type"] = "paragraph";
                        p["content"] = paragraph;
                        blocks.Add(p);
                    }
                    return null;
                }

                string expected = mode == "heading" ? "subheading" : "paragraph";
                if (index >= blocks.Count || blocks[index] == null || BlockTypeOf(blocks[index]) != expected)
                    return "That block can't be edited any more. Please reload the page.";

                if (mode == "heading") blocks[index]["text"] = heading;
                else blocks[index]["content"] = paragraph;
                return null;
            });

            if (error != null)
            {
                ReopenTextDialog(error);
                return;
            }
            ReloadLesson();
        }

        private void ReopenTextDialog(string message)
        {
            string mode = HdnTextMode.Value;
            if (mode != "both" && mode != "heading" && mode != "paragraph") mode = "both";

            string script =
                "window.addEventListener('load', function () {" +
                "  var box = document.getElementById('textError');" +
                "  if (box) { box.textContent = " + new JavaScriptSerializer().Serialize(message) + "; box.style.display = 'block'; }" +
                "  setTextMode('" + mode + "');" +
                "  openModal('modalText');" +
                "});";
            ClientScript.RegisterStartupScript(GetType(), "textError", script, true);
        }

        // ---------- List ----------
        protected void BtnSaveList_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0)
            {
                ShowMessage("Please select a lesson first.", true);
                return;
            }

            int index;
            if (!int.TryParse(HdnListIndex.Value, out index)) index = -1;

            // The item boxes are created by JavaScript, so read them straight from the posted form
            var items = new List<string>();
            string[] raw = Request.Unvalidated.Form.GetValues("listItem");
            if (raw != null)
            {
                foreach (string r in raw)
                {
                    string item = Regex.Replace((r ?? "").Trim(), @"\s+", " ");
                    if (item.Length > 0) items.Add(item);
                }
            }

            string error = null;
            if (items.Count == 0) error = "Please enter at least one list item.";
            else if (items.Count > MaxListItems) error = "A list can have up to " + MaxListItems + " items.";
            else
            {
                foreach (string item in items)
                {
                    if (item.Length > MaxListItemChars)
                    {
                        error = "Each list item should be " + MaxListItemChars + " characters or fewer.";
                        break;
                    }
                }
            }

            if (error != null)
            {
                ReopenListDialog(error, items);
                return;
            }

            error = ModifyBlocks(blocks =>
            {
                if (index < 0)
                {
                    var list = new Dictionary<string, object>();
                    list["type"] = "list";
                    list["items"] = items;
                    blocks.Add(list);
                    return null;
                }

                if (index >= blocks.Count || blocks[index] == null || BlockTypeOf(blocks[index]) != "list")
                    return "That list can't be edited any more. Please reload the page.";

                blocks[index]["items"] = items;
                return null;
            });

            if (error != null)
            {
                ReopenListDialog(error, items);
                return;
            }
            ReloadLesson();
        }

        // The item boxes are lost on postback: send the items back to refill the dialog
        private void ReopenListDialog(string message, List<string> items)
        {
            int index;
            if (!int.TryParse(HdnListIndex.Value, out index)) index = -1;

            var json = new JavaScriptSerializer();
            string script =
                "window.addEventListener('load', function () {" +
                "  fillListModal(" + json.Serialize(items) + ", " + index + ");" +
                "  var box = document.getElementById('listError');" +
                "  if (box) { box.textContent = " + json.Serialize(message) + "; box.style.display = 'block'; }" +
                "  openModal('modalList');" +
                "});";
            ClientScript.RegisterStartupScript(GetType(), "listError", script, true);
        }

        // ---------- Image (one per lesson) ----------
        protected void BtnSaveImage_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0)
            {
                ShowMessage("Please select a lesson first.", true);
                return;
            }

            int index;
            if (!int.TryParse(HdnImageIndex.Value, out index)) index = -1;
            bool editing = index >= 0;

            string caption = Regex.Replace((TxtImageCaption.Text ?? "").Trim(), @"\s+", " ");
            if (caption.Length > MaxCaptionChars)
            {
                ReopenImageDialog("The caption should be " + MaxCaptionChars + " characters or fewer.");
                return;
            }

            HttpPostedFile posted = FileBlockImage.PostedFile;
            bool hasFile = posted != null && posted.ContentLength > 0;

            if (editing && hasFile)
            {
                ReopenImageDialog("To use a different picture, remove the current image first, then add a new one.");
                return;
            }
            if (!editing && !hasFile)
            {
                ReopenImageDialog("Please choose an image to upload.");
                return;
            }

            string savedUrl = "";
            if (!editing)
            {
                string imageError;
                if (!TrySaveLessonImage(posted, lessonId.ToString(CultureInfo.InvariantCulture), out savedUrl, out imageError))
                {
                    ReopenImageDialog(imageError);
                    return;
                }
            }

            string error = ModifyBlocks(blocks =>
            {
                if (!editing)
                {
                    if (FindBlock(blocks, "image") != null)
                        return "This lesson already has an image. Please remove it before uploading a new one.";

                    var img = new Dictionary<string, object>();
                    img["type"] = "image";
                    img["url"] = savedUrl;
                    if (caption.Length > 0) img["caption"] = caption;
                    blocks.Add(img);
                    return null;
                }

                if (index >= blocks.Count || blocks[index] == null || BlockTypeOf(blocks[index]) != "image")
                    return "That image can't be edited any more. Please reload the page.";

                // Editing only changes the caption
                if (caption.Length > 0) blocks[index]["caption"] = caption;
                else blocks[index].Remove("caption");
                return null;
            });

            if (error != null)
            {
                DeleteLessonImage(savedUrl);   // don't leave an orphan file if the save failed
                ReopenImageDialog(error);
                return;
            }

            ReloadLesson();
        }

        private bool TrySaveLessonImage(HttpPostedFile file, string targetLessonId, out string savedUrl, out string errorMessage)
        {
            savedUrl = "";
            errorMessage = "";

            if (file == null || file.ContentLength == 0) return true;

            if (file.ContentLength > MaxLessonImageBytes)
            {
                errorMessage = "The image must be 2 MB or smaller.";
                return false;
            }

            var head = new byte[12];
            int read = file.InputStream.Read(head, 0, head.Length);
            file.InputStream.Position = 0;

            string extension = DetectImageExtension(head, read);
            if (extension == null)
            {
                errorMessage = "Please choose a valid JPG, PNG, or WEBP image.";
                return false;
            }

            try
            {
                string folder = Server.MapPath(LessonImageFolder);
                Directory.CreateDirectory(folder);

                string fileName = targetLessonId + "-" + Guid.NewGuid().ToString("N") + extension;
                file.SaveAs(Path.Combine(folder, fileName));

                savedUrl = LessonImageFolder + fileName;
                return true;
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                errorMessage = "The image could not be saved on the server.";
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                errorMessage = "The server is not allowed to save images in " + LessonImageFolder;
                return false;
            }
        }

        private void DeleteLessonImage(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                string appRelative = VirtualPathUtility.ToAppRelative(url);
                if (!appRelative.StartsWith(LessonImageFolder, StringComparison.OrdinalIgnoreCase)) return;

                string folder = Path.GetFullPath(Server.MapPath(LessonImageFolder));
                string full = Path.GetFullPath(Server.MapPath(appRelative));
                if (full.StartsWith(folder, StringComparison.OrdinalIgnoreCase) && File.Exists(full)) File.Delete(full);
            }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is HttpException)) throw;
            }
        }

        private void ShowBlockImage(int index)
        {
            ImgBlockCurrent.Visible = false;

            List<Dictionary<string, object>> blocks = ParseBlocks(currentContentJson);
            if (index < 0 || index >= blocks.Count || blocks[index] == null || BlockTypeOf(blocks[index]) != "image") return;

            string url = SafeBlockImageUrl(Str(blocks[index], "url", ""));
            if (url.Length == 0) return;

            ImgBlockCurrent.Src = url;
            ImgBlockCurrent.Visible = true;
        }

        private void ReopenImageDialog(string message)
        {
            int index;
            bool editing = int.TryParse(HdnImageIndex.Value, out index) && index >= 0;
            if (editing) ShowBlockImage(index);

            string script =
                "window.addEventListener('load', function () {" +
                "  var box = document.getElementById('imageError');" +
                "  if (box) { box.textContent = " + new JavaScriptSerializer().Serialize(message) + "; box.style.display = 'block'; }" +
                "  var t = document.getElementById('title_modalImage');" +
                "  if (t) t.textContent = '" + (editing ? "Edit image" : "Add image") + "';" +
                "  var rb = document.getElementById('btnRemoveImage'); if (rb) rb.style.display = '" + (editing ? "" : "none") + "';" +
                "  var ua = document.getElementById('imageUploadArea'); if (ua) ua.style.display = '" + (editing ? "none" : "") + "';" +
                "  openModal('modalImage');" +
                "});";
            ClientScript.RegisterStartupScript(GetType(), "imageError", script, true);
        }

        // ---------- Video ----------
        protected void BtnConfirmUpload_Click(object sender, EventArgs e)
        {
            string url = TxtVideoUrl.Text.Trim();
            string caption = Regex.Replace((TxtVideoCaption.Text ?? "").Trim(), @"\s+", " ");

            Uri uri;
            if (lessonId <= 0 || !Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                ShowMessage("Please paste a valid https video link.", true);
                return;
            }
            if (caption.Length > MaxCaptionChars)
            {
                ShowMessage("The caption should be " + MaxCaptionChars + " characters or fewer.", true);
                return;
            }

            string error = ModifyBlocks(blocks =>
            {
                Dictionary<string, object> video = FindBlock(blocks, "video");
                if (video == null)
                {
                    video = new Dictionary<string, object>();
                    video["type"] = "video";
                    blocks.Insert(0, video);
                }
                video["url"] = url;
                if (caption.Length > 0) video["caption"] = caption;
                else video.Remove("caption");
                return null;
            });

            if (error != null)
            {
                ShowMessage(error, true);
                return;
            }
            ReloadLesson();
        }

        // ---------- Content list: edit / move / delete ----------
        protected void RptBlocks_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int index;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out index) || index < 0) return;

            switch (e.CommandName)
            {
                case "edit":
                    OpenBlockForEdit(index);
                    break;
                case "up":
                    MoveBlock(index, -1);
                    break;
                case "down":
                    MoveBlock(index, 1);
                    break;
            }
        }

        private void MoveBlock(int index, int direction)
        {
            string error = ModifyBlocks(blocks =>
            {
                if (index >= blocks.Count || blocks[index] == null || IsSeparateCard(BlockTypeOf(blocks[index])))
                    return "That content no longer exists. Please reload the page.";

                var positions = new List<int>();
                for (int i = 0; i < blocks.Count; i++)
                    if (blocks[i] != null && !IsSeparateCard(BlockTypeOf(blocks[i]))) positions.Add(i);

                int k = positions.IndexOf(index);
                int other = k + direction;
                if (k < 0 || other < 0 || other >= positions.Count) return null;

                Dictionary<string, object> keep = blocks[index];
                blocks[index] = blocks[positions[other]];
                blocks[positions[other]] = keep;
                return null;
            });

            if (error != null)
            {
                ShowMessage(error, true);
                return;
            }
            ReloadLesson();
        }

        private void OpenBlockForEdit(int index)
        {
            List<Dictionary<string, object>> blocks = ParseBlocks(currentContentJson);
            if (index >= blocks.Count || blocks[index] == null || !IsEditableType(BlockTypeOf(blocks[index])))
            {
                ShowMessage("That content can't be edited here.", true);
                return;
            }

            Dictionary<string, object> block = blocks[index];
            string type = BlockTypeOf(block);

            if (type == "subheading" || type == "paragraph")
            {
                string mode = type == "subheading" ? "heading" : "paragraph";

                HdnTextIndex.Value = index.ToString(CultureInfo.InvariantCulture);
                HdnTextMode.Value = mode;
                TxtHeading.Text = type == "subheading" ? RawStr(block, "text") : "";
                TxtParagraph.Text = type == "paragraph" ? RawStr(block, "content") : "";

                ClientScript.RegisterStartupScript(GetType(), "editText",
                    "window.addEventListener('load', function () { setTextMode('" + mode + "'); openModal('modalText'); });", true);
            }
            else if (type == "list")
            {
                HdnListIndex.Value = index.ToString(CultureInfo.InvariantCulture);
                string jsItems = new JavaScriptSerializer().Serialize(ToStringList(GetValue(block, "items")));

                ClientScript.RegisterStartupScript(GetType(), "editList",
                    "window.addEventListener('load', function () { fillListModal(" + jsItems + ", " + index + "); openModal('modalList'); });", true);
            }
            else if (type == "image")
            {
                HdnImageIndex.Value = index.ToString(CultureInfo.InvariantCulture);
                TxtImageCaption.Text = RawStr(block, "caption");
                ShowBlockImage(index);

                // Editing an image only changes its caption: hide the upload area, show "Remove current image"
                ClientScript.RegisterStartupScript(GetType(), "editImage",
                    "window.addEventListener('load', function () {" +
                    "  document.getElementById('title_modalImage').textContent = 'Edit image';" +
                    "  var rb = document.getElementById('btnRemoveImage'); if (rb) rb.style.display = '';" +
                    "  document.getElementById('imageUploadArea').style.display = 'none';" +
                    "  openModal('modalImage');" +
                    "});", true);
            }
            else if (type == "video")
            {
                TxtVideoUrl.Text = RawStr(block, "url");
                TxtVideoCaption.Text = RawStr(block, "caption");

                ClientScript.RegisterStartupScript(GetType(), "editVideo",
                    "window.addEventListener('load', function () { openModal('modalUploadVideo'); });", true);
            }
        }

        protected void BtnConfirmRemoveBlock_Click(object sender, EventArgs e)
        {
            int index;
            string expectedType = HdnRemoveBlockType.Value ?? "";
            if (!int.TryParse(HdnRemoveBlockIndex.Value, out index) || index < 0)
            {
                ShowPopup("Couldn't delete it", "That content could not be found. Please reload the page and try again.");
                return;
            }

            string removedImageUrl = null;
            string error = ModifyBlocks(blocks =>
            {
                if (index >= blocks.Count || blocks[index] == null || IsSeparateCard(BlockTypeOf(blocks[index])))
                    return "That content no longer exists. Please reload the page and try again.";

                string type = BlockTypeOf(blocks[index]);
                if (type != expectedType)
                    return "The lesson has changed since the page was loaded. Please reload the page and try again.";

                if (type == "image") removedImageUrl = RawStr(blocks[index], "url");
                blocks.RemoveAt(index);
                return null;
            });

            if (error != null)
            {
                ShowPopup("Couldn't delete it", error);
                return;
            }

            DeleteLessonImage(removedImageUrl);
            ReloadLesson();
        }

        // ---------- Lessons: add / edit details / delete ----------
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

        protected void BtnSaveDetails_Click(object sender, EventArgs e)
        {
            string title = TxtLessonTitle.Text.Trim();
            string desc = TxtLessonDesc.Text.Trim();

            if (lessonId <= 0 || title.Length == 0)
            {
                ShowMessage("Please enter a lesson title.", true);
                return;
            }

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                using (var cmd = new NpgsqlCommand(SqlLessonDetailsUpdate, conn))
                {
                    cmd.Parameters.AddWithValue("title", title);
                    cmd.Parameters.AddWithValue("desc", desc.Length == 0 ? (object)DBNull.Value : desc);
                    cmd.Parameters.AddWithValue("lid", lessonId);
                    cmd.Parameters.AddWithValue("cid", courseId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                ShowMessage("We couldn't save the lesson details." + DbErrorText(ex), true);
                return;
            }
            ReloadLesson();
        }

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
                ShowMessage("We couldn't add the lesson." + DbErrorText(ex), true);
                return;
            }

            if (newLessonId == null)
            {
                ShowMessage("This course could not be found.", true);
                return;
            }

            Response.Redirect("courseBuilder.aspx?courseId=" + courseId + "&lessonId=" + newLessonId);
        }

        protected void BtnConfirmDeleteLesson_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0)
            {
                ShowPopup("Couldn't delete it", "Please select a lesson first.");
                return;
            }

            if (courseStatus == StatusPublished && lessonCount <= 1)
            {
                ShowPopup("Couldn't delete it",
                    "A published course must keep at least one lesson. Add another lesson first, or unpublish the course.");
                return;
            }

            var imageUrls = new List<string>();

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                using (NpgsqlTransaction tx = conn.BeginTransaction())
                {
                    string json;
                    using (var read = new NpgsqlCommand(SqlLessonContentForDelete, conn, tx))
                    {
                        read.Parameters.AddWithValue("lid", lessonId);
                        read.Parameters.AddWithValue("cid", courseId);
                        read.Parameters.AddWithValue("uid", userId);
                        object value = read.ExecuteScalar();
                        if (value == null)
                        {
                            ShowPopup("Couldn't delete it", "This lesson could not be found.");
                            return;
                        }
                        json = value == DBNull.Value ? "" : Convert.ToString(value);
                    }

                    foreach (var b in ParseBlocks(json))
                        if (b != null && BlockTypeOf(b) == "image") imageUrls.Add(RawStr(b, "url"));

                    int rows;
                    using (var del = new NpgsqlCommand(SqlLessonDelete, conn, tx))
                    {
                        del.Parameters.AddWithValue("lid", lessonId);
                        del.Parameters.AddWithValue("cid", courseId);
                        del.Parameters.AddWithValue("uid", userId);
                        rows = del.ExecuteNonQuery();
                    }

                    if (rows == 0)
                    {
                        ShowPopup("Couldn't delete it", "This lesson could not be found.");
                        return;
                    }
                    tx.Commit();
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                var pg = ex as PostgresException;
                if (pg != null && pg.SqlState == "23503") // foreign key violation
                {
                    ShowPopup("Couldn't delete it",
                        "This lesson can't be deleted because other records (such as student progress) still depend on it.");
                    return;
                }
                ShowPopup("Couldn't delete it", "We couldn't delete the lesson." + DbErrorText(ex));
                return;
            }

            // Remove the lesson's uploaded images from disk
            foreach (string url in imageUrls) DeleteLessonImage(url);

            Response.Redirect("courseBuilder.aspx?courseId=" + courseId + "&notice=lessondeleted");
        }

        // ---------- Course image / duration helpers ----------
        private void FillDurationLists()
        {
            for (int w = 1; w <= MaxWeeks; w++)
                DdlCourseWeeks.Items.Add(new ListItem(w + (w == 1 ? " week" : " weeks"), w.ToString(CultureInfo.InvariantCulture)));

            for (int h = 1; h <= MaxHoursPerWeek; h++)
                DdlCourseHours.Items.Add(new ListItem(h + (h == 1 ? " hour" : " hours") + " per week", h.ToString(CultureInfo.InvariantCulture)));
        }

        private static string FormatDuration(int weeks, int hoursPerWeek)
        {
            return weeks + (weeks == 1 ? " week" : " weeks") +
                   " (" + hoursPerWeek + (hoursPerWeek == 1 ? " hr" : " hrs") + "/week)";
        }

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

        // Checks the file's first bytes, not its name, so a renamed file is rejected
        private static string DetectImageExtension(byte[] head, int length)
        {
            if (length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return ".jpg";

            if (length >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47 &&
                head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A) return ".png";

            if (length >= 12 && head[0] == (byte)'R' && head[1] == (byte)'I' && head[2] == (byte)'F' && head[3] == (byte)'F' &&
                head[8] == (byte)'W' && head[9] == (byte)'E' && head[10] == (byte)'B' && head[11] == (byte)'P') return ".webp";

            return null;
        }

        private bool TrySaveCourseImage(out string savedPath, out string error)
        {
            savedPath = null;
            error = null;

            HttpPostedFile file = FileCourseImg.PostedFile;
            if (file == null || file.ContentLength == 0) return true;

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
                error = "The server is not allowed to save images in " + ImageFolder;
                return false;
            }
        }

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
                if (!(ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)) throw;
            }
        }

        // ---------- Course info ----------
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

            string combinedDescription = string.Join(Environment.NewLine, ReadPostedList("learningOutcome").ToArray());
            List<string> referenceList = ReadPostedList("courseReference");
            string referenceJson = referenceList.Count > 0 ? new JavaScriptSerializer().Serialize(referenceList) : null;

            string newImage, imageError;
            if (!TrySaveCourseImage(out newImage, out imageError))
            {
                ReopenCourseInfoDialog(imageError);
                return;
            }

            string duration = FormatDuration(weeks, hours);

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
                    cmd.Parameters.AddWithValue("desc", combinedDescription.Length == 0 ? (object)DBNull.Value : combinedDescription);
                    cmd.Parameters.AddWithValue("reference", referenceJson == null ? (object)DBNull.Value : referenceJson);
                    cmd.Parameters.AddWithValue("img", newImage == null ? (object)DBNull.Value : newImage);
                    cmd.Parameters.AddWithValue("cid", courseId);
                    cmd.Parameters.AddWithValue("uid", userId);
                    rows = cmd.ExecuteNonQuery();
                }

                if (rows == 0)
                {
                    DeleteStoredImage(newImage);
                    ReopenCourseInfoDialog("This course could not be found.");
                    return;
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                DeleteStoredImage(newImage);
                ReopenCourseInfoDialog("We couldn't save the course." + DbErrorText(ex));
                return;
            }

            if (newImage != null) DeleteStoredImage(currentImage);

            ReloadLesson();
        }

        // Reads a set of JavaScript-created text boxes (same name) from the posted form, skipping empty ones
        private List<string> ReadPostedList(string fieldName)
        {
            var result = new List<string>();
            string[] raw = Request.Unvalidated.Form.GetValues(fieldName);
            if (raw == null) return result;

            foreach (string value in raw)
            {
                string trimmed = (value ?? "").Trim();
                if (trimmed.Length > 0) result.Add(trimmed);
            }
            return result;
        }

        // ---------- Publish / unpublish ----------
        private static string NormalizeStatus(string status)
        {
            string s = (status ?? "").Trim().ToLowerInvariant().Replace(' ', '_');
            return s == StatusPublished ? StatusPublished : StatusDraft;
        }

        private void ApplyStatusUi(int enrolled)
        {
            bool published = courseStatus == StatusPublished;

            LblCourseStatus.Text = published ? "Published" : "Draft";
            LblCourseStatus.CssClass = "status-pill " + (published ? "published" : "draft");

            BtnPublishCourse.Visible = !published;
            BtnUnpublish.Visible = published;
            PnlLiveNotice.Visible = published;
            PnlDraftFooter.Visible = !published;

            if (published)
            {
                string info;
                if (enrolled > 0)
                    info = enrolled + (enrolled == 1 ? " student is" : " students are") +
                           " currently enrolled and will no longer see it until you publish it again. " +
                           "To change lessons without hiding the course, just edit them: changes save straight away.";
                else
                    info = "Students will no longer see it until you publish it again. " +
                           "To change lessons without hiding the course, just edit them: changes save straight away.";
                LitUnpublishInfo.Text = Server.HtmlEncode(info);
            }
        }

        private void ShowNoticeFromQuery()
        {
            switch (Request.QueryString["notice"])
            {
                case "published":
                    ShowMessage("Course published. Students can now see it.", false);
                    break;
                case "draft":
                    ShowMessage("Your changes are saved. The course is still a draft and hidden from students.", false);
                    break;
                case "unpublished":
                    ShowMessage("Course unpublished. It is now hidden from students.", false);
                    break;
                case "lessondeleted":
                    ShowMessage("Lesson deleted.", false);
                    break;
            }
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
                ShowMessage("We couldn't update the course status." + DbErrorText(ex), true);
                return false;
            }
        }

        protected void BtnConfirmPublish_Click(object sender, EventArgs e)
        {
            if (courseStatus == StatusPublished)
            {
                ShowMessage("This course is already published.", false);
                return;
            }

            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(loadedTitle) || loadedTitle == DraftTitle)
                missing.Add("a course title");
            if (string.IsNullOrWhiteSpace(loadedTopic))
                missing.Add("a topic");
            if (string.IsNullOrWhiteSpace(loadedLevel))
                missing.Add("a course level (IGCSE or SPM)");
            if (string.IsNullOrWhiteSpace(loadedDuration))
                missing.Add("the course duration");
            if (string.IsNullOrWhiteSpace(loadedImage))
                missing.Add("a course image");
            if (lessonCount == 0)
                missing.Add("at least one lesson");

            if (missing.Count > 0)
            {
                ShowMessage("Cannot publish yet. Please add: " + string.Join(", ", missing.ToArray()) +
                            ". You can update the course details with \"Edit course info\".", true);
                return;
            }

            if (emptyLessons.Count > 0)
            {
                ShowPopup("Can't publish yet",
                    "Every lesson needs at least one lesson content (a heading, paragraph, list, image or video) before the course can be published. " +
                    "These lessons are still empty: " + string.Join(", ", emptyLessons.ToArray()) + ".");
                return;
            }

            if (!SetCourseStatus(StatusPublished)) return;
            Response.Redirect(BuilderUrl("published"));
        }

        protected void BtnSaveDraft_Click(object sender, EventArgs e)
        {
            if (courseStatus == StatusPublished)
            {
                ShowMessage("This course is published, so it can't be saved as a draft here. Use \"Unpublish Course\" if you need to hide it.", true);
                return;
            }
            Response.Redirect(BuilderUrl("draft"));
        }

        protected void BtnConfirmUnpublish_Click(object sender, EventArgs e)
        {
            if (courseStatus != StatusPublished)
            {
                ShowMessage("This course is not published.", false);
                return;
            }

            if (!SetCourseStatus(StatusDraft)) return;
            Response.Redirect(BuilderUrl("unpublished"));
        }

        // ---------- Messages ----------
        private void ShowMessage(string message, bool isError)
        {
            LblStatus.Text = Server.HtmlEncode(message);
            LblStatus.CssClass = isError ? "page-status error" : "page-status";
            LblStatus.Visible = true;
        }

        private void ShowPopup(string title, string message)
        {
            var json = new JavaScriptSerializer();
            string script =
                "window.addEventListener('load', function () {" +
                "  showPopup(" + json.Serialize(title) + ", " + json.Serialize(message) + ");" +
                "});";
            ClientScript.RegisterStartupScript(GetType(), "messagePopup", script, true);
        }
    }
}