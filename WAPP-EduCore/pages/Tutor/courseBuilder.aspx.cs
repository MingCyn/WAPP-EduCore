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

        private const string SqlLessonQuizId =
            "SELECT l.quiz_id FROM " + LessonsTable + " l " +
            "WHERE l.lesson_id = @lid AND l.course_id = @cid " +
            "AND EXISTS (SELECT 1 FROM courses WHERE course_id = @cid AND user_id = @uid)";

        private const string SqlLessonClearQuiz =
            "UPDATE " + LessonsTable + " SET quiz_id = NULL WHERE lesson_id = @lid AND course_id = @cid";

        private const string SqlQuestionsDelete = "DELETE FROM quiz_questions WHERE quiz_id = @qid";
        private const string SqlQuizDelete = "DELETE FROM quiz WHERE quiz_id = @qid";

        // ---------- Types ----------
        public class LessonRow
        {
            public int LessonId { get; set; }
            public string Title { get; set; }
            public bool HasVideo { get; set; }
            public bool IsActive { get; set; }
        }

        public class BlockRow
        {
            public int Index { get; set; }
            public string TypeKey { get; set; }
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

                // Bind the breadcrumb title safely
                LitCrumbTitle.Text = Server.HtmlEncode(string.IsNullOrWhiteSpace(courseTitle) ? "Course Details" : courseTitle);

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

            string suffix = "&lessonId=" + lessonId + "&courseId=" + courseId;
            LnkAssessment.HRef = "assessmentBuilder.aspx?tab=assessment" + suffix;
            LnkAddQuiz.HRef = LnkAssessment.HRef;

            PnlDraftNotice.Visible = false;
        }

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
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
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
                case "subheading": return Truncate(Str(block, "text", ""), 180);
                case "paragraph": return Truncate(Str(block, "content", ""), 180);
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
                default: return "";
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

        protected void BtnSaveText_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0)
            {
                ShowMessage("Please select a lesson first.", true);
                return;
            }

            string mode = HdnTextMode.Value;
            int index;
            int.TryParse(HdnTextIndex.Value, out index);

            string heading = Regex.Replace((TxtHeading.Text ?? "").Trim(), @"\s+", " ");
            string paragraph = (TxtParagraph.Text ?? "").Replace("\r\n", "\n").Trim();

            ModifyBlocks(blocks =>
            {
                if (mode == "both")
                {
                    if (heading.Length > 0)
                    {
                        blocks.Add(new Dictionary<string, object> { { "type", "subheading" }, { "text", heading } });
                    }
                    if (paragraph.Length > 0)
                    {
                        blocks.Add(new Dictionary<string, object> { { "type", "paragraph" }, { "content", paragraph } });
                    }
                }
                else if (index >= 0 && index < blocks.Count)
                {
                    if (mode == "heading") blocks[index]["text"] = heading;
                    else if (mode == "paragraph") blocks[index]["content"] = paragraph;
                }
                return null;
            });
            ReloadLesson();
        }

        protected void BtnSaveList_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0) return;
            int index;
            int.TryParse(HdnListIndex.Value, out index);

            var items = ReadPostedList("listItem");

            ModifyBlocks(blocks =>
            {
                if (index < 0)
                {
                    blocks.Add(new Dictionary<string, object> { { "type", "list" }, { "items", items } });
                }
                else if (index < blocks.Count)
                {
                    blocks[index]["items"] = items;
                }
                return null;
            });
            ReloadLesson();
        }

        protected void BtnSaveImage_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0) return;
            int index;
            int.TryParse(HdnImageIndex.Value, out index);
            string caption = TxtImageCaption.Text.Trim();

            string savedUrl = "";
            HttpPostedFile posted = FileBlockImage.PostedFile;
            if (posted != null && posted.ContentLength > 0)
            {
                TrySaveLessonImage(posted, lessonId.ToString(), out savedUrl, out _);
            }

            ModifyBlocks(blocks =>
            {
                if (index < 0)
                {
                    var img = new Dictionary<string, object> { { "type", "image" }, { "url", savedUrl } };
                    if (caption.Length > 0) img["caption"] = caption;
                    blocks.Add(img);
                }
                else if (index < blocks.Count)
                {
                    if (caption.Length > 0) blocks[index]["caption"] = caption;
                }
                return null;
            });
            ReloadLesson();
        }

        private bool TrySaveLessonImage(HttpPostedFile file, string targetLessonId, out string savedUrl, out string errorMessage)
        {
            savedUrl = "";
            errorMessage = "";
            try
            {
                string folder = Server.MapPath(LessonImageFolder);
                Directory.CreateDirectory(folder);
                string fileName = targetLessonId + "-" + Guid.NewGuid().ToString("N") + ".jpg";
                file.SaveAs(Path.Combine(folder, fileName));
                savedUrl = LessonImageFolder + fileName;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        protected void BtnConfirmUpload_Click(object sender, EventArgs e)
        {
            string url = TxtVideoUrl.Text.Trim();
            string caption = TxtVideoCaption.Text.Trim();
            if (lessonId <= 0) return;

            ModifyBlocks(blocks =>
            {
                var video = FindBlock(blocks, "video");
                if (video == null)
                {
                    video = new Dictionary<string, object> { { "type", "video" } };
                    blocks.Insert(0, video);
                }
                video["url"] = url;
                if (caption.Length > 0) video["caption"] = caption;
                return null;
            });
            ReloadLesson();
        }

        protected void RptBlocks_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int index;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out index)) return;
            if (e.CommandName == "up" || e.CommandName == "down")
            {
                int dir = e.CommandName == "up" ? -1 : 1;
                ModifyBlocks(blocks =>
                {
                    int target = index + dir;
                    if (target >= 0 && target < blocks.Count)
                    {
                        var temp = blocks[index];
                        blocks[index] = blocks[target];
                        blocks[target] = temp;
                    }
                    return null;
                });
                ReloadLesson();
            }
        }

        protected void BtnConfirmRemoveBlock_Click(object sender, EventArgs e)
        {
            int index;
            if (int.TryParse(HdnRemoveBlockIndex.Value, out index))
            {
                ModifyBlocks(blocks =>
                {
                    if (index >= 0 && index < blocks.Count) blocks.RemoveAt(index);
                    return null;
                });
                ReloadLesson();
            }
        }

        protected string GetLessonUrl(object id)
        {
            return string.Format("courseBuilder.aspx?courseId={0}&lessonId={1}", courseId, id);
        }

        protected string GetLessonIcon(object hasVideo)
        {
            return (bool)hasVideo ? "🎥" : "📄";
        }

        protected void BtnSaveDetails_Click(object sender, EventArgs e)
        {
            string title = TxtLessonTitle.Text.Trim();
            string desc = TxtLessonDesc.Text.Trim();
            if (lessonId <= 0 || title.Length == 0) return;

            using (NpgsqlConnection conn = OpenConnection())
            using (var cmd = new NpgsqlCommand(SqlLessonDetailsUpdate, conn))
            {
                cmd.Parameters.AddWithValue("title", title);
                cmd.Parameters.AddWithValue("desc", string.IsNullOrEmpty(desc) ? (object)DBNull.Value : desc);
                cmd.Parameters.AddWithValue("lid", lessonId);
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.ExecuteNonQuery();
            }
            ReloadLesson();
        }

        protected void BtnConfirmAddModule_Click(object sender, EventArgs e)
        {
            string title = TxtModuleTitle.Text.Trim();
            string desc = TxtModuleDesc.Text.Trim();
            if (title.Length == 0) return;

            object newLessonId;
            using (NpgsqlConnection conn = OpenConnection())
            using (var cmd = new NpgsqlCommand(SqlLessonInsert, conn))
            {
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.Parameters.AddWithValue("uid", userId);
                cmd.Parameters.AddWithValue("title", title);
                cmd.Parameters.AddWithValue("desc", string.IsNullOrEmpty(desc) ? (object)DBNull.Value : desc);
                newLessonId = cmd.ExecuteScalar();
            }

            if (newLessonId != null)
            {
                Response.Redirect("courseBuilder.aspx?courseId=" + courseId + "&lessonId=" + newLessonId);
            }
        }

        protected void BtnConfirmDeleteAssessment_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0)
            {
                ShowPopup("Couldn't delete it", "Please select a lesson first.");
                return;
            }

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                using (NpgsqlTransaction tx = conn.BeginTransaction())
                {
                    object value;
                    // 1. Get the quiz ID tied to this lesson before unlinking it
                    using (var read = new NpgsqlCommand(SqlLessonQuizId, conn, tx))
                    {
                        read.Parameters.AddWithValue("lid", lessonId);
                        read.Parameters.AddWithValue("cid", courseId);
                        value = read.ExecuteScalar();
                    }

                    if (value == null || value == DBNull.Value)
                    {
                        ShowPopup("Couldn't delete it", "This lesson doesn't have an assessment.");
                        return;
                    }
                    long quizId = Convert.ToInt64(value);

                    // 2. Unlink the quiz from the lesson
                    using (var cmd = new NpgsqlCommand(SqlLessonClearQuiz, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("lid", lessonId);
                        cmd.Parameters.AddWithValue("cid", courseId);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Delete all questions belonging to this quiz
                    using (var cmd = new NpgsqlCommand(SqlQuestionsDelete, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("qid", quizId);
                        cmd.ExecuteNonQuery();
                    }

                    // 4. Delete the main quiz record itself
                    using (var cmd = new NpgsqlCommand(SqlQuizDelete, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("qid", quizId);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                var pg = ex as PostgresException;
                if (pg != null && pg.SqlState == "23503")
                {
                    ShowPopup("Couldn't delete it",
                        "This assessment can't be deleted because students have already taken it and their results depend on it.");
                    return;
                }
                ShowPopup("Couldn't delete it", "We couldn't delete the assessment.");
                return;
            }

            Response.Redirect(BuilderUrl("assessmentdeleted"));
        }

        private void ShowPopup(string title, string message)
        {
            var json = new JavaScriptSerializer();
            string script =
                "window.addEventListener('load', function () { showPopup(" +
                json.Serialize(title) + ", " + json.Serialize(message) + "); });";
            ClientScript.RegisterStartupScript(GetType(), "serverPopup", script, true);
        }

        private void FillDurationLists()
        {
            for (int w = 1; w <= MaxWeeks; w++)
                DdlCourseWeeks.Items.Add(new ListItem(w + (w == 1 ? " week" : " weeks"), w.ToString()));
            for (int h = 1; h <= MaxHoursPerWeek; h++)
                DdlCourseHours.Items.Add(new ListItem(h + (h == 1 ? " hour" : " hours") + " per week", h.ToString()));
        }

        private static string FormatDuration(int weeks, int hoursPerWeek)
        {
            return weeks + (weeks == 1 ? " week" : " weeks") + " (" + hoursPerWeek + " hrs/week)";
        }

        private static void ParseDuration(string text, out string weeks, out string hours)
        {
            weeks = hours = "";
            Match m = DurationPattern.Match(text);
            if (m.Success)
            {
                weeks = m.Groups[1].Value;
                hours = m.Groups[2].Value;
            }
        }

        private static void SelectIfPresent(DropDownList list, string value)
        {
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
            return "";
        }

        protected void BtnSaveCourseInfo_Click(object sender, EventArgs e)
        {
            string title = TxtCourseTitle.Text.Trim();
            string topic = TxtCourseTopic.Text.Trim();
            string level = DdlCourseLevel.SelectedValue;
            int weeks, hours;

            int.TryParse(DdlCourseWeeks.SelectedValue, out weeks);
            int.TryParse(DdlCourseHours.SelectedValue, out hours);

            string duration = FormatDuration(weeks, hours);
            string combinedDescription = string.Join(Environment.NewLine, ReadPostedList("learningOutcome"));
            List<string> referenceList = ReadPostedList("courseReference");
            string referenceJson = referenceList.Count > 0 ? new JavaScriptSerializer().Serialize(referenceList) : null;

            using (NpgsqlConnection conn = OpenConnection())
            using (var cmd = new NpgsqlCommand(SqlCourseUpdate, conn))
            {
                cmd.Parameters.AddWithValue("title", title);
                cmd.Parameters.AddWithValue("topic", topic);
                cmd.Parameters.AddWithValue("duration", duration);
                cmd.Parameters.AddWithValue("level", level);
                cmd.Parameters.AddWithValue("desc", string.IsNullOrEmpty(combinedDescription) ? (object)DBNull.Value : combinedDescription);
                cmd.Parameters.AddWithValue("reference", referenceJson == null ? (object)DBNull.Value : referenceJson);
                cmd.Parameters.AddWithValue("img", DBNull.Value);
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.Parameters.AddWithValue("uid", userId);
                cmd.ExecuteNonQuery();
            }
            ReloadLesson();
        }

        private List<string> ReadPostedList(string fieldName)
        {
            var result = new List<string>();
            string[] raw = Request.Unvalidated.Form.GetValues(fieldName);
            if (raw != null)
            {
                foreach (string value in raw)
                {
                    string trimmed = (value ?? "").Trim();
                    if (trimmed.Length > 0) result.Add(trimmed);
                }
            }
            return result;
        }

        private static string NormalizeStatus(string status)
        {
            string s = (status ?? "").Trim().ToLowerInvariant();
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
        }

        private void ShowNoticeFromQuery() { }

        private string BuilderUrl(string notice)
        {
            return "courseBuilder.aspx?courseId=" + courseId + (lessonId > 0 ? "&lessonId=" + lessonId : "") + "&notice=" + notice;
        }

        private bool SetCourseStatus(string newStatus)
        {
            using (NpgsqlConnection conn = OpenConnection())
            using (var cmd = new NpgsqlCommand(SqlCourseStatusUpdate, conn))
            {
                cmd.Parameters.AddWithValue("status", newStatus);
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.Parameters.AddWithValue("uid", userId);
                cmd.ExecuteNonQuery();
            }
            return true;
        }

        protected void BtnConfirmPublish_Click(object sender, EventArgs e)
        {
            SetCourseStatus(StatusPublished);
            Response.Redirect(BuilderUrl("published"));
        }

        protected void BtnSaveDraft_Click(object sender, EventArgs e)
        {
            Response.Redirect(BuilderUrl("draft"));
        }

        protected void BtnConfirmUnpublish_Click(object sender, EventArgs e)
        {
            SetCourseStatus(StatusDraft);
            Response.Redirect(BuilderUrl("unpublished"));
        }

        private void ShowMessage(string message, bool isError)
        {
            LblStatus.Text = Server.HtmlEncode(message);
            LblStatus.CssClass = isError ? "page-status error" : "page-status";
            LblStatus.Visible = true;
        }
        protected void BtnConfirmDeleteLesson_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0) return;

            using (NpgsqlConnection conn = OpenConnection())
            {
                // Delete the lesson from the database
                const string sql = "DELETE FROM public.lesson WHERE lesson_id = @lid AND course_id = @cid";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("lid", lessonId);
                    cmd.Parameters.AddWithValue("cid", courseId);
                    cmd.ExecuteNonQuery();
                }
            }

            // Redirect back to the course builder page without a lessonId selected
            Response.Redirect("courseBuilder.aspx?courseId=" + courseId);
        }
    }
}