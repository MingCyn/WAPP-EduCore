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
        private const string ConnectionName = "SupabasePostgres";
        private const string SessionUserKey = "UserID";
        private const string LoginPage = "~/auth/login.aspx";
        private const string CourseListPage = "courses.aspx";

        private const string LessonsTable = "public.lesson";
        private const bool ShowDatabaseErrors = true;
        private const string DraftTitle = "Untitled course";

        private const string StatusPublished = "published";

        protected int CourseIdValue;
        private const string StatusDraft = "draft";

        private const int MaxWeeks = 52;
        private const int MaxHoursPerWeek = 40;

        private const string ImageFolder = "~/uploads/courses/";
        private const int MaxImageBytes = 2 * 1024 * 1024;   // 2 MB

        private static readonly Regex DurationPattern = new Regex(
            @"^\s*(\d+)\s*weeks?\s*\(\s*(\d+)\s*hrs?\s*/\s*week\s*\)\s*$", RegexOptions.IgnoreCase);

        private int userId;

        private bool TryGetUserId(out int id)
        {
            return int.TryParse(Convert.ToString(Session[SessionUserKey]), out id) && id > 0;
        }

        private static NpgsqlConnection OpenConnection()
        {
            var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString);
            conn.Open();
            return conn;
        }

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
            "SELECT COUNT(*) FROM enrollments WHERE course_id = @cid";

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
        private string currentImage = "";

        private string loadedTitle = "", loadedTopic = "", loadedLevel = "", loadedDuration = "", loadedImage = "", courseStatus = "";
        private int lessonCount;

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

            // Assign the value here once courseId is validated
            CourseIdValue = courseId;

            if (!IsPostBack)
            {
                FillDurationLists();
                ShowNoticeFromQuery();
            }

            LoadPage();
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

                if (!IsPostBack)
                {
                    TxtCourseTitle.Text = courseTitle == DraftTitle ? "" : courseTitle;
                    TxtCourseTopic.Text = topic;

                    // 1. Load Learning Outcomes
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        var rawOutcomes = description.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var cleanedOutcomes = new List<string>();
                        foreach (var item in rawOutcomes)
                        {
                            string trimmed = item.Trim();
                            if (trimmed.Length > 0) cleanedOutcomes.Add(trimmed);
                        }

                        string jsArray = new JavaScriptSerializer().Serialize(cleanedOutcomes);
                        string script = "window.addEventListener('load', function() { " +
                                        "  var container = document.getElementById('outcomesContainer');" +
                                        "  container.innerHTML = '';" +
                                        "  var items = " + jsArray + ";" +
                                        "  if(items.length > 0) {" +
                                        "    items.forEach(function(val) { addOutcomeInput(val); });" +
                                        "  } else { addOutcomeInput(); }" +
                                        "});";
                        ClientScript.RegisterStartupScript(GetType(), "loadOutcomes", script, true);
                    }
                    else
                    {
                        string script = "window.addEventListener('load', function() { addOutcomeInput(); });";
                        ClientScript.RegisterStartupScript(GetType(), "defaultOutcome", script, true);
                    }

                    // 2. Load References from JSON Array
                    if (!string.IsNullOrWhiteSpace(referenceJson))
                    {
                        try
                        {
                            var refs = new JavaScriptSerializer().Deserialize<List<string>>(referenceJson);
                            if (refs != null && refs.Count > 0)
                            {
                                string jsRefArray = new JavaScriptSerializer().Serialize(refs);
                                string refScript = "window.addEventListener('load', function() { " +
                                                   "  var refContainer = document.getElementById('referencesContainer');" +
                                                   "  refContainer.innerHTML = '';" +
                                                   "  var refItems = " + jsRefArray + ";" +
                                                   "  if(refItems.length > 0) {" +
                                                   "    refItems.forEach(function(val) { addReferenceInput(val); });" +
                                                   "  } else { addReferenceInput(); }" +
                                                   "});";
                                ClientScript.RegisterStartupScript(GetType(), "loadReferences", refScript, true);
                            }
                        }
                        catch { /* Fallback if deserialization fails */ }
                    }
                    else
                    {
                        string refScript = "window.addEventListener('load', function() { addReferenceInput(); });";
                        ClientScript.RegisterStartupScript(GetType(), "defaultReference", refScript, true);
                    }

                    string weeks, hours;
                    ParseDuration(duration, out weeks, out hours);
                    SelectIfPresent(DdlCourseWeeks, weeks);
                    SelectIfPresent(DdlCourseHours, hours);
                    SelectIfPresent(DdlCourseLevel, (level ?? "").Trim().ToUpperInvariant());
                }

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
                cmd.Parameters.AddWithValue("cid", courseId);
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

        private void BindLesson(LessonDetail lesson, string courseTitle)
        {
            LitLessonTitle.Text = Server.HtmlEncode(lesson.Title);
            LitLessonMeta.Text = "Part of " + Server.HtmlEncode(courseTitle) +
                " <span class=\"dot-sep\">&bull;</span> Created " +
                lesson.CreatedAt.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);

            bool hasSummary = !string.IsNullOrWhiteSpace(lesson.Description);
            PnlSummary.Visible = hasSummary;
            LitSummary.Text = Server.HtmlEncode(lesson.Description);

            if (!IsPostBack)
            {
                TxtLessonTitle.Text = lesson.Title;
                TxtLessonDesc.Text = lesson.Description;
                TxtVideoUrl.Text = lesson.VideoUrl;
            }

            bool hasVideo = !string.IsNullOrWhiteSpace(lesson.VideoUrl);
            PnlVideo.Visible = hasVideo;
            BtnAddVideo.Visible = !hasVideo;
            if (hasVideo)
            {
                Uri uri;
                string source = Uri.TryCreate(lesson.VideoUrl, UriKind.Absolute, out uri) ? uri.Host : "Uploaded video";
                LitVideoSource.Text = Server.HtmlEncode(source);

                string thumbSrc = "";
                if (lesson.VideoUrl.Contains("youtube.com") || lesson.VideoUrl.Contains("youtu.be"))
                {
                    string vidId = ExtractYouTubeId(lesson.VideoUrl);
                    if (!string.IsNullOrEmpty(vidId))
                    {
                        thumbSrc = "https://img.youtube.com/vi/" + vidId + "/hqdefault.jpg";
                    }
                }

                if (string.IsNullOrEmpty(thumbSrc))
                {
                    thumbSrc = SafeImageUrl(lesson.VideoUrl);
                }

                ImgVideoThumb.ImageUrl = thumbSrc;
            }

            List<Dictionary<string, object>> blocks = ParseBlocks(lesson.ContentJson);
            Dictionary<string, object> paragraphBlock = FindBlock(blocks, "paragraph");
            Dictionary<string, object> tableBlock = FindBlock(blocks, "table");

            PnlParagraph.Visible = paragraphBlock != null;
            if (paragraphBlock != null)
            {
                string text = Str(paragraphBlock, "content", "");
                LitParagraphMeta.Text = Server.HtmlEncode(text.Length > 50 ? text.Substring(0, 47) + "..." : text);
                if (!IsPostBack) TxtParagraphContent.Text = text;
            }

            PnlTable.Visible = tableBlock != null;
            if (tableBlock != null)
            {
                string tblTitle = Str(tableBlock, "title", "Data Table");
                LitTableTitle.Text = Server.HtmlEncode(tblTitle);
                if (!IsPostBack)
                {
                    TxtTableTitle.Text = tblTitle;
                    TxtTableData.Text = Str(tableBlock, "data", "");
                }
            }

            bool hasQuiz = lesson.QuizId.HasValue;
            PnlAssessment.Visible = hasQuiz;
            LnkAddQuiz.Visible = !hasQuiz;
            if (hasQuiz)
                LitAssessmentMeta.Text = "Assessment Component <span class=\"dot-sep\">&bull;</span> Quiz #" + lesson.QuizId.Value;

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

            string suffix = "&lessonId=" + lessonId + "&courseId=" + courseId;
            LnkInteractive.HRef = "assessmentBuilder.aspx?tab=interactive" + suffix;
            LnkAddInteractive.HRef = LnkInteractive.HRef;
            LnkAssessment.HRef = "assessmentBuilder.aspx?tab=assessment" + suffix;
            LnkAddQuiz.HRef = LnkAssessment.HRef;
            LnkLab.HRef = "assessmentBuilder.aspx?tab=lab" + suffix;
            LnkAddLab.HRef = LnkLab.HRef;

            bool anything = hasVideo || hasQuiz || interactive != null || lab != null || paragraphBlock != null || tableBlock != null;
            PnlEmpty.Visible = !anything;
            DivAddRow.Visible = !(hasVideo && hasQuiz && interactive != null && lab != null);

            PnlDraftNotice.Visible = drafts.Count > 0;
            LitDraftList.Text = Server.HtmlEncode(string.Join(", ", drafts.ToArray()));
        }

        private string ExtractYouTubeId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            try
            {
                Uri uri = new Uri(url);
                if (uri.Host.Contains("youtube.com"))
                {
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    return query["v"];
                }
                else if (uri.Host.Contains("youtu.be"))
                {
                    return uri.AbsolutePath.Trim('/');
                }
            }
            catch { }
            return "";
        }

        protected void BtnPreviewCourse_Click(object sender, EventArgs e)
        {
            int targetCourseId = 0;
            if (int.TryParse(Request.QueryString["courseId"], out targetCourseId) && targetCourseId > 0)
            {
                Response.Redirect("../student/courseView.aspx?courseId=" + targetCourseId, false);
                Context.ApplicationInstance.CompleteRequest();
            }
            else
            {
                Response.Redirect("courses.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

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
        }

        private static string GetVideoUrl(string json)
        {
            Dictionary<string, object> video = FindBlock(ParseBlocks(json), "video");
            return video == null ? "" : Str(video, "url", "");
        }

        protected void BtnSaveParagraph_Click(object sender, EventArgs e)
        {
            string content = TxtParagraphContent.Text.Trim();
            if (string.IsNullOrEmpty(content))
            {
                ShowMessage("Please enter paragraph text.", true);
                return;
            }

            SaveContentBlock("paragraph", block => {
                block["content"] = content;
            });
        }

        protected void BtnSaveTable_Click(object sender, EventArgs e)
        {
            string title = TxtTableTitle.Text.Trim();
            string data = TxtTableData.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                ShowMessage("Please enter a table title.", true);
                return;
            }

            SaveContentBlock("table", block => {
                block["title"] = title;
                block["data"] = data;
            });
        }

        private void SaveContentBlock(string blockType, Action<Dictionary<string, object>> populateBlock)
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
                    if (value == null)
                    {
                        ShowMessage("Lesson not found.", true);
                        return;
                    }
                    json = value == DBNull.Value ? "" : Convert.ToString(value);
                }

                List<Dictionary<string, object>> blocks;
                if (!TryParseBlocks(json, out blocks))
                {
                    ShowMessage("Invalid lesson content format.", true);
                    return;
                }

                Dictionary<string, object> block = FindBlock(blocks, blockType);
                if (block == null)
                {
                    block = new Dictionary<string, object>();
                    block["type"] = blockType;
                    blocks.Add(block);
                }

                populateBlock(block);

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

        private static bool IsPublished(Dictionary<string, object> block)
        {
            object v;
            if (block.TryGetValue("published", out v) && v is bool) return (bool)v;
            return true;
        }

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

            // Capture Learning Outcomes
            string[] rawOutcomes = Request.Unvalidated.Form.GetValues("learningOutcome");
            string combinedDescription = "";
            if (rawOutcomes != null)
            {
                var outcomeList = new List<string>();
                foreach (var outcome in rawOutcomes)
                {
                    string trimmed = outcome.Trim();
                    if (!string.IsNullOrEmpty(trimmed)) outcomeList.Add(trimmed);
                }
                combinedDescription = string.Join(Environment.NewLine, outcomeList);
            }

            // Capture References and serialize to JSON array format
            string[] rawReferences = Request.Unvalidated.Form.GetValues("courseReference");
            var referenceList = new List<string>();
            if (rawReferences != null)
            {
                foreach (var r in rawReferences)
                {
                    string trimmedRef = r.Trim();
                    if (!string.IsNullOrEmpty(trimmedRef))
                    {
                        referenceList.Add(trimmedRef);
                    }
                }
            }
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

                var pg = ex as PostgresException;
                string detail = pg != null ? pg.MessageText : ex.Message;
                ReopenCourseInfoDialog("We couldn't save the course." + (ShowDatabaseErrors ? " (" + detail + ")" : " Please try again."));
                return;
            }

            if (newImage != null) DeleteStoredImage(currentImage);

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
                string json;
                using (var read = new NpgsqlCommand(SqlLessonContentLock, conn, tx))
                {
                    read.Parameters.AddWithValue("lid", lessonId);
                    read.Parameters.AddWithValue("cid", courseId);
                    object value = read.ExecuteScalar();
                    if (value == null)
                    {
                        ShowMessage("This lesson could not be found.", true);
                        return;
                    }
                    json = value == DBNull.Value ? "" : Convert.ToString(value);
                }

                List<Dictionary<string, object>> blocks;
                if (!TryParseBlocks(json, out blocks))
                {
                    ShowMessage("This lesson's content is in an unexpected format, so the video was not saved.", true);
                    return;
                }

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

            Response.Redirect("courseBuilder.aspx?courseId=" + courseId + "&lessonId=" + newLessonId);
        }

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
            string notice = Request.QueryString["notice"];
            if (notice == "published")
                ShowMessage("Course published. Students can now see it.", false);
            else if (notice == "draft")
                ShowMessage("Your changes are saved. The course is still a draft and hidden from students.", false);
            else if (notice == "unpublished")
                ShowMessage("Course unpublished. It is now hidden from students.", false);
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

        private void ShowMessage(string message, bool isError)
        {
            LblStatus.Text = Server.HtmlEncode(message);
            LblStatus.CssClass = isError ? "page-status error" : "page-status";
            LblStatus.Visible = true;
        }
    }
}