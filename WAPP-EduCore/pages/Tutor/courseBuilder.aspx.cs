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
using NpgsqlTypes;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class courseBuilder : System.Web.UI.Page
    {
        // =====================================================================
        // Configuration
        // =====================================================================
        private const string ConnectionName = "SupabasePostgres";
        private const string LoginPage = "~/auth/login.aspx";
        private const string CourseListPage = "courses.aspx";

        private const string LessonsTable = "public.lesson";
        private const bool ShowDatabaseErrors = true;   // set to false before going live
        private const string DraftTitle = "Untitled course";

        private const string StatusPublished = "published";
        private const string StatusDraft = "draft";

        private const int MaxWeeks = 52;
        private const int MaxHoursPerWeek = 40;

        // Course image
        private const string ImageFolder = "~/uploads/courses/";
        private const int MaxImageBytes = 2 * 1024 * 1024;

        // Image inside a lesson (one per lesson)
        private const string LessonImageFolder = "~/uploads/lessons/";
        private const int MaxLessonImageBytes = 2 * 1024 * 1024;

        // Lesson content limits
        private const int MaxBlockChars = 10000;
        private const int MaxHeadingChars = 200;
        private const int MaxCaptionChars = 200;
        private const int MaxListItems = 100;
        private const int MaxListItemChars = 500;

        private static readonly Regex DurationPattern = new Regex(
            @"^\s*(\d+)\s*weeks?\s*\(\s*(\d+)\s*hrs?\s*/\s*week\s*\)\s*$", RegexOptions.IgnoreCase);

        // =====================================================================
        // SQL
        // Every lesson statement also checks that the course belongs to the
        // logged-in tutor, so a tutor can't edit another tutor's lesson by
        // changing the URL.
        // =====================================================================
        private const string OwnsCourse =
            " AND EXISTS (SELECT 1 FROM courses WHERE course_id = @cid AND user_id = @uid)";

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
            "SELECT lesson_id, lesson_title, lesson_content::text FROM " + LessonsTable +
            " WHERE course_id = @cid" + OwnsCourse + " ORDER BY created_at, lesson_id";

        private const string SqlLesson =
            "SELECT lesson_title, lesson_description, quiz_id, lesson_content::text, created_at FROM " + LessonsTable +
            " WHERE lesson_id = @lid AND course_id = @cid" + OwnsCourse;

        private const string SqlLessonOwned =
            "SELECT 1 FROM " + LessonsTable + " WHERE lesson_id = @lid AND course_id = @cid" + OwnsCourse;

        private const string SqlLessonInsert =
            "INSERT INTO " + LessonsTable + " (course_id, lesson_title, lesson_description, lesson_content) " +
            "SELECT CAST(@cid AS bigint), @title, CAST(@desc AS text), CAST('[]' AS jsonb) " +
            "WHERE EXISTS (SELECT 1 FROM courses WHERE course_id = @cid AND user_id = @uid) " +
            "RETURNING lesson_id";

        private const string SqlLessonDetailsUpdate =
            "UPDATE " + LessonsTable + " SET lesson_title = @title, lesson_description = CAST(@desc AS text) " +
            "WHERE lesson_id = @lid AND course_id = @cid" + OwnsCourse;

        private const string SqlLessonContentLock =
            "SELECT lesson_content::text FROM " + LessonsTable +
            " WHERE lesson_id = @lid AND course_id = @cid" + OwnsCourse + " FOR UPDATE";

        private const string SqlLessonContentUpdate =
            "UPDATE " + LessonsTable + " SET lesson_content = CAST(@content AS jsonb) " +
            "WHERE lesson_id = @lid AND course_id = @cid" + OwnsCourse;

        private const string SqlLessonDelete =
            "DELETE FROM " + LessonsTable + " WHERE lesson_id = @lid AND course_id = @cid" + OwnsCourse;

        private const string SqlLessonClearQuiz =
            "UPDATE " + LessonsTable + " SET quiz_id = NULL WHERE lesson_id = @lid AND course_id = @cid" + OwnsCourse;

        // Quiz tables: quiz(quiz_id, lesson_id, quiz_title, passing_score, created_at, question_id)
        //              quiz_questions(question_id, quiz_id, question_text, option_a..d, correct_answer, explaination)
        // The quiz is found either through quiz.lesson_id or through lesson.quiz_id.
        private const string SqlQuizIdsOfLesson =
            "SELECT quiz_id FROM quiz WHERE lesson_id = @lid " +
            "UNION " +
            "SELECT quiz_id FROM " + LessonsTable + " WHERE lesson_id = @lid AND quiz_id IS NOT NULL";

        // Delete questions first due to foreign key constraints, then delete the quiz record itself.
        private const string SqlQuestionsDelete = "DELETE FROM quiz_questions WHERE quiz_id = @qid";
        private const string SqlQuizDelete = "DELETE FROM quiz WHERE quiz_id = @qid";

        // =====================================================================
        // Types
        // =====================================================================
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

        // =====================================================================
        // State
        // =====================================================================
        protected int CourseIdValue;

        protected bool IsCoursePublished
        {
            get { return courseStatus == StatusPublished; }
        }

        private int userId;
        private int courseId;
        private int lessonId;
        private string currentImage = "";
        private string currentContentJson = "";
        private string courseStatus = "";
        private int lessonCount;
        private readonly List<string> emptyLessons = new List<string>();

        // =====================================================================
        // Database helpers
        // =====================================================================
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

        private bool TryGetUserId(out int id)
        {
            return int.TryParse(Convert.ToString(Session["UserID"]), out id) && id > 0;
        }

        private static void AddText(NpgsqlCommand cmd, string name, string value)
        {
            cmd.Parameters.Add(name, NpgsqlDbType.Text).Value = string.IsNullOrEmpty(value) ? (object)DBNull.Value : value;
        }

        // Command with the lesson parameters (lid, cid, uid); use only with SQL that contains all three.
        private NpgsqlCommand LessonCommand(string sql, NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("lid", lessonId);
            cmd.Parameters.AddWithValue("cid", courseId);
            cmd.Parameters.AddWithValue("uid", userId);
            return cmd;
        }

        private bool LessonIsOwned(NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            using (var cmd = LessonCommand(SqlLessonOwned, conn, tx))
                return cmd.ExecuteScalar() != null;
        }

        // =====================================================================
        // Client-side messages
        // =====================================================================
        private void RunScript(string key, string js)
        {
            ClientScript.RegisterStartupScript(GetType(), key,
                "window.addEventListener('load', function () { " + js + " });", true);
        }

        private void ShowPopup(string title, string message)
        {
            var json = new JavaScriptSerializer();
            RunScript("serverPopup", "showPopup(" + json.Serialize(title) + ", " + json.Serialize(message) + ");");
        }

        private void ShowNoticeFromQuery()
        {
            string message;
            switch ((Request.QueryString["notice"] ?? "").ToLowerInvariant())
            {
                case "published": message = "Course published. Students can see it now."; break;
                case "unpublished": message = "Course unpublished. It is hidden from students."; break;
                case "draft": message = "Your changes are saved."; break;
                case "assessmentdeleted": message = "The assessment and all of its questions were deleted."; break;
                case "lessondeleted": message = "The lesson was deleted."; break;
                default: return;
            }
            LblStatus.Text = Server.HtmlEncode(message);
            LblStatus.CssClass = "page-status";
            LblStatus.Visible = true;
        }

        private void Redirect(string url)
        {
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void ReloadLesson()
        {
            Redirect("courseBuilder.aspx?courseId=" + courseId + (lessonId > 0 ? "&lessonId=" + lessonId : ""));
        }

        private string BuilderUrl(string notice)
        {
            return "courseBuilder.aspx?courseId=" + courseId + (lessonId > 0 ? "&lessonId=" + lessonId : "") + "&notice=" + notice;
        }

        // =====================================================================
        // Page load
        // =====================================================================
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserRole"] == null ||
                !Session["UserRole"].ToString().Equals("Tutor", StringComparison.OrdinalIgnoreCase) ||
                !TryGetUserId(out userId))
            {
                Redirect(LoginPage);
                return;
            }

            int.TryParse(Request.QueryString["courseId"], out courseId);
            int.TryParse(Request.QueryString["lessonId"], out lessonId);

            if (courseId <= 0)
            {
                Redirect(CourseListPage);
                return;
            }

            CourseIdValue = courseId;

            if (!IsPostBack)
            {
                FillDurationLists();
                ShowNoticeFromQuery();
            }

            if (!LoadPage()) return;

            if (!IsPostBack && Request.QueryString["addLesson"] == "1")
                RunScript("openAddLesson", "openModal('modalAddModule');");

            if (!IsPostBack && Request.QueryString["edit"] == "1")
                RunScript("openCourseInfo", "openModal('modalCourseInfo');");
        }

        private bool LoadPage()
        {
            using (NpgsqlConnection conn = OpenConnection())
            {
                string courseTitle, topic, duration, level, image, status, description, referenceJson;
                if (!TryGetCourse(conn, out courseTitle, out topic, out duration, out level, out image,
                                  out status, out description, out referenceJson))
                {
                    Redirect(CourseListPage);
                    return false;
                }

                currentImage = image;
                courseStatus = NormalizeStatus(status);

                string displayName = string.IsNullOrWhiteSpace(courseTitle) ? "this course" : courseTitle;
                LitPublishLesson.Text = Server.HtmlEncode(displayName);
                LitUnpublishName.Text = Server.HtmlEncode(displayName);
                LblCourseName.Text = Server.HtmlEncode(courseTitle);
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
            return true;
        }

        private bool TryGetCourse(NpgsqlConnection conn, out string title, out string topic, out string duration,
                                  out string level, out string image, out string status, out string description,
                                  out string referenceJson)
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
            emptyLessons.Clear();
            using (var cmd = new NpgsqlCommand(SqlLessons, conn))
            {
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.Parameters.AddWithValue("uid", userId);
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
            using (var cmd = LessonCommand(SqlLesson, conn, null))
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

        // =====================================================================
        // Course info form
        // =====================================================================
        private void FillCourseInfoForm(string courseTitle, string topic, string duration, string level,
                                        string description, string referenceJson)
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
            RegisterRowFiller("loadOutcomes", "outcomesContainer", "addOutcomeInput", outcomes);

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
            RegisterRowFiller("loadReferences", "referencesContainer", "addReferenceInput", refs);

            string weeks, hours;
            ParseDuration(duration, out weeks, out hours);
            SelectIfPresent(DdlCourseWeeks, weeks);
            SelectIfPresent(DdlCourseHours, hours);
            SelectIfPresent(DdlCourseLevel, (level ?? "").Trim().ToUpperInvariant());
        }

        private void RegisterRowFiller(string key, string containerId, string addFunction, List<string> values)
        {
            string jsArray = new JavaScriptSerializer().Serialize(values);
            RunScript(key,
                "var c = document.getElementById('" + containerId + "'); if (!c) return; c.innerHTML = '';" +
                " var items = " + jsArray + ";" +
                " if (items.length > 0) items.forEach(function (v) { " + addFunction + "(v); }); else " + addFunction + "();");
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
            Match m = DurationPattern.Match(text ?? "");
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

            List<string> outcomes = ReadPostedList("learningOutcome");
            List<string> references = ReadPostedList("courseReference");

            string problem = null;
            if (title.Length == 0) problem = "Please enter a course title.";
            else if (topic.Length == 0) problem = "Please enter a topic.";
            else if (level.Length == 0) problem = "Please select a course level.";
            else if (weeks <= 0 || hours <= 0) problem = "Please select the duration in weeks and hours per week.";

            // Optional new course image
            string newImage = null;
            if (problem == null && FileCourseImg.HasFile)
            {
                string imageError;
                if (!TrySaveImage(FileCourseImg.PostedFile, ImageFolder, "course-" + courseId, MaxImageBytes,
                                  out newImage, out imageError))
                    problem = imageError;
            }

            if (problem != null)
            {
                // Dynamic input rows are lost on postback, so rebuild them with what the tutor typed.
                RegisterRowFiller("loadOutcomes", "outcomesContainer", "addOutcomeInput", outcomes);
                RegisterRowFiller("loadReferences", "referencesContainer", "addReferenceInput", references);
                RunScript("reopenCourseInfo", "openModal('modalCourseInfo');");
                ShowPopup("Couldn't save the course", problem);
                return;
            }

            string duration = FormatDuration(weeks, hours);
            string outcomesText = string.Join(Environment.NewLine, outcomes);
            string referenceJson = references.Count > 0 ? new JavaScriptSerializer().Serialize(references) : null;

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                using (var cmd = new NpgsqlCommand(SqlCourseUpdate, conn))
                {
                    cmd.Parameters.AddWithValue("title", title);
                    cmd.Parameters.AddWithValue("topic", topic);
                    cmd.Parameters.AddWithValue("duration", duration);
                    cmd.Parameters.AddWithValue("level", level);
                    AddText(cmd, "desc", outcomesText);
                    AddText(cmd, "reference", referenceJson);
                    AddText(cmd, "img", newImage);
                    cmd.Parameters.AddWithValue("cid", courseId);
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                if (newImage != null) DeleteUploadedFile(newImage, ImageFolder);
                ShowPopup("Couldn't save the course", "We couldn't save the course." + DbErrorText(ex));
                return;
            }

            if (newImage != null) DeleteUploadedFile(currentImage, ImageFolder);
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

        // =====================================================================
        // Publish / unpublish
        // =====================================================================
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

            if (enrolled > 0)
                LitUnpublishInfo.Text = enrolled + (enrolled == 1 ? " student is" : " students are") +
                    " enrolled and won't be able to open the course until you publish it again.";
            else
                LitUnpublishInfo.Text = "You can publish it again at any time.";
        }

        private void SetCourseStatus(string newStatus)
        {
            using (NpgsqlConnection conn = OpenConnection())
            using (var cmd = new NpgsqlCommand(SqlCourseStatusUpdate, conn))
            {
                cmd.Parameters.AddWithValue("status", newStatus);
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.Parameters.AddWithValue("uid", userId);
                cmd.ExecuteNonQuery();
            }
        }

        protected void BtnConfirmPublish_Click(object sender, EventArgs e)
        {
            SetCourseStatus(StatusPublished);
            Redirect(BuilderUrl("published"));
        }

        protected void BtnConfirmUnpublish_Click(object sender, EventArgs e)
        {
            SetCourseStatus(StatusDraft);
            Redirect(BuilderUrl("unpublished"));
        }

        protected void BtnSaveDraft_Click(object sender, EventArgs e)
        {
            Redirect(BuilderUrl("draft"));
        }

        // =====================================================================
        // Lessons: add / edit details / delete
        // =====================================================================
        protected string GetLessonUrl(object id)
        {
            return string.Format("courseBuilder.aspx?courseId={0}&lessonId={1}", courseId, id);
        }

        protected string GetLessonIcon(object hasVideo)
        {
            return (bool)hasVideo ? "🎥" : "📄";
        }

        protected void BtnConfirmAddModule_Click(object sender, EventArgs e)
        {
            string title = TxtModuleTitle.Text.Trim();
            string desc = TxtModuleDesc.Text.Trim();
            if (title.Length == 0)
            {
                RunScript("reopenAddLesson", "openModal('modalAddModule');");
                ShowPopup("Couldn't add the lesson", "Please enter a lesson title.");
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
                    AddText(cmd, "desc", desc);
                    newLessonId = cmd.ExecuteScalar();
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                ShowPopup("Couldn't add the lesson", "We couldn't add the lesson." + DbErrorText(ex));
                return;
            }

            if (newLessonId == null)
            {
                ShowPopup("Couldn't add the lesson", "This course could not be found.");
                return;
            }
            Redirect("courseBuilder.aspx?courseId=" + courseId + "&lessonId=" + newLessonId);
        }

        protected void BtnSaveDetails_Click(object sender, EventArgs e)
        {
            string title = TxtLessonTitle.Text.Trim();
            string desc = TxtLessonDesc.Text.Trim();
            if (lessonId <= 0) return;

            if (title.Length == 0)
            {
                RunScript("reopenDetails", "openModal('modalEditDetails');");
                ShowPopup("Couldn't save", "The lesson title can't be empty.");
                return;
            }

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                using (var cmd = LessonCommand(SqlLessonDetailsUpdate, conn, null))
                {
                    cmd.Parameters.AddWithValue("title", title);
                    AddText(cmd, "desc", desc);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                ShowPopup("Couldn't save", "We couldn't save the lesson details." + DbErrorText(ex));
                return;
            }
            ReloadLesson();
        }

        protected void BtnConfirmDeleteLesson_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0) return;

            var imageFiles = new List<string>();
            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                using (NpgsqlTransaction tx = conn.BeginTransaction())
                {
                    // Read the content first so uploaded images can be removed afterwards.
                    object content;
                    using (var cmd = LessonCommand(SqlLessonContentLock, conn, tx))
                        content = cmd.ExecuteScalar();
                    if (content == null)
                    {
                        ShowPopup("Couldn't delete it", "This lesson could not be found.");
                        return;
                    }
                    foreach (var block in ParseBlocks(content == DBNull.Value ? "" : Convert.ToString(content)))
                        if (BlockTypeOf(block) == "image") imageFiles.Add(Str(block, "url", ""));

                    // The quiz references the lesson (and the lesson references the quiz), so it goes first.
                    DeleteQuizzesOfLesson(conn, tx);

                    using (var cmd = LessonCommand(SqlLessonDelete, conn, tx))
                        cmd.ExecuteNonQuery();

                    tx.Commit();
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                ShowPopup("Couldn't delete it", DeleteErrorText("lesson", ex));
                return;
            }

            foreach (string url in imageFiles) DeleteUploadedFile(url, LessonImageFolder);
            Redirect("courseBuilder.aspx?courseId=" + courseId + "&notice=lessondeleted");
        }

        // =====================================================================
        // Assessment (quiz) delete
        // =====================================================================
        // Deletes every quiz that belongs to the current lesson together with its questions.
        // Must run inside a transaction. Returns how many quizzes were deleted.
        //
        // Order matters:
        //   1. lesson.quiz_id            -> NULL   (the lesson stops pointing at the quiz)
        //   2. quiz.question_id          -> NULL   (the quiz stops pointing at a question)
        //   3. DELETE FROM quiz_questions          (questions no longer referenced)
        //   4. DELETE FROM quiz                    (nothing references it any more)
        private int DeleteQuizzesOfLesson(NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            var quizIds = new List<long>();
            using (var cmd = new NpgsqlCommand(SqlQuizIdsOfLesson, conn, tx))
            {
                cmd.Parameters.AddWithValue("lid", lessonId);
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        if (!r.IsDBNull(0)) quizIds.Add(Convert.ToInt64(r[0]));
            }

            using (var cmd = LessonCommand(SqlLessonClearQuiz, conn, tx))
                cmd.ExecuteNonQuery();

            foreach (long quizId in quizIds)
            {
                // Removed SqlQuizClearQuestionPointer since column doesn't exist on quiz table
                using (var cmd = new NpgsqlCommand(SqlQuestionsDelete, conn, tx))
                {
                    cmd.Parameters.AddWithValue("qid", quizId);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new NpgsqlCommand(SqlQuizDelete, conn, tx))
                {
                    cmd.Parameters.AddWithValue("qid", quizId);
                    cmd.ExecuteNonQuery();
                }
            }
            return quizIds.Count;
        }

        private static string DeleteErrorText(string what, NpgsqlException ex)
        {
            var pg = ex as PostgresException;
            if (pg != null && pg.SqlState == "23503")   // foreign key violation
            {
                string detail = ShowDatabaseErrors
                    ? " (table: " + pg.TableName + ", constraint: " + pg.ConstraintName + ")"
                    : "";
                return "This " + what + " can't be deleted because other records (for example student attempts or results) still depend on it." + detail;
            }
            if (pg != null && pg.SqlState == "23502")   // not-null violation
            {
                return "The database refused to clear a required column while deleting this " + what +
                       ". Make quiz.question_id nullable (ALTER TABLE quiz ALTER COLUMN question_id DROP NOT NULL)." +
                       (ShowDatabaseErrors ? " (" + pg.MessageText + ")" : "");
            }
            return "We couldn't delete the " + what + "." + DbErrorText(ex);
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
                    if (!LessonIsOwned(conn, tx))
                    {
                        ShowPopup("Couldn't delete it", "This lesson could not be found.");
                        return;
                    }

                    int deleted = DeleteQuizzesOfLesson(conn, tx);
                    if (deleted == 0)
                    {
                        ShowPopup("Couldn't delete it", "This lesson doesn't have an assessment.");
                        return;   // transaction is rolled back when disposed
                    }
                    tx.Commit();
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                ShowPopup("Couldn't delete it", DeleteErrorText("assessment", ex));
                return;
            }

            Redirect(BuilderUrl("assessmentdeleted"));
        }

        // =====================================================================
        // Lesson content blocks: parsing and display
        // =====================================================================
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
                if (b != null && BlockTypeOf(b) == type) return b;
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

        // Interactive / lab blocks are managed elsewhere and are not shown in this list.
        private static bool IsSeparateCard(string type)
        {
            return type == "interactive" || type == "lab";
        }

        private static bool IsEditableType(string type)
        {
            return type == "subheading" || type == "paragraph" || type == "list" || type == "image" || type == "video";
        }

        private static int CountContentBlocks(List<Dictionary<string, object>> blocks)
        {
            int n = 0;
            foreach (var b in blocks)
                if (b != null && !IsSeparateCard(BlockTypeOf(b))) n++;
            return n;
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

        // Positions (in the stored array) of the blocks that appear in the list, in order.
        private static List<int> VisiblePositions(List<Dictionary<string, object>> blocks)
        {
            var positions = new List<int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] == null || IsSeparateCard(BlockTypeOf(blocks[i]))) continue;
                positions.Add(i);
            }
            return positions;
        }

        private List<BlockRow> BuildBlockRows(List<Dictionary<string, object>> blocks)
        {
            var rows = new List<BlockRow>();
            List<int> positions = VisiblePositions(blocks);

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

        // =====================================================================
        // Lesson content blocks: saving
        // =====================================================================

        // Locks the lesson row, lets `change` modify the block list, then saves it.
        // `change` returns null on success or an error message (nothing is saved then).
        private string ModifyBlocks(Func<List<Dictionary<string, object>>, string> change)
        {
            if (lessonId <= 0) return "Please select a lesson first.";

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                using (NpgsqlTransaction tx = conn.BeginTransaction())
                {
                    string json;
                    using (var read = LessonCommand(SqlLessonContentLock, conn, tx))
                    {
                        object value = read.ExecuteScalar();
                        if (value == null) return "This lesson could not be found.";
                        json = value == DBNull.Value ? "" : Convert.ToString(value);
                    }

                    List<Dictionary<string, object>> blocks;
                    if (!TryParseBlocks(json, out blocks))
                        return "This lesson's content is in an unexpected format, so nothing was changed.";

                    string error = change(blocks);
                    if (error != null) return error;

                    using (var write = LessonCommand(SqlLessonContentUpdate, conn, tx))
                    {
                        write.Parameters.AddWithValue("content", new JavaScriptSerializer().Serialize(blocks));
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

        // ModifyBlocks, then either reload the lesson or show the error.
        private bool SaveBlocks(Func<List<Dictionary<string, object>>, string> change, string reopenModalId)
        {
            string error = ModifyBlocks(change);
            if (error == null)
            {
                ReloadLesson();
                return true;
            }
            if (!string.IsNullOrEmpty(reopenModalId)) RunScript("reopen" + reopenModalId, "openModal('" + reopenModalId + "');");
            ShowPopup("Couldn't save", error);
            return false;
        }

        private static bool IndexIsType(List<Dictionary<string, object>> blocks, int index, string type)
        {
            return index >= 0 && index < blocks.Count && BlockTypeOf(blocks[index]) == type;
        }

        // ---------- Heading & paragraph ----------
        protected void BtnSaveText_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0) { ShowPopup("Couldn't save", "Please select a lesson first."); return; }

            string mode = HdnTextMode.Value;
            int index;
            if (!int.TryParse(HdnTextIndex.Value, out index)) index = -1;

            string heading = Regex.Replace((TxtHeading.Text ?? "").Trim(), @"\s+", " ");
            string paragraph = (TxtParagraph.Text ?? "").Replace("\r\n", "\n").Trim();

            if (heading.Length > MaxHeadingChars)
            {
                ShowPopup("Couldn't save", "The heading must be " + MaxHeadingChars + " characters or fewer.");
                return;
            }
            if (paragraph.Length > MaxBlockChars)
            {
                ShowPopup("Couldn't save", "The paragraph must be " + MaxBlockChars + " characters or fewer.");
                return;
            }

            SaveBlocks(blocks =>
            {
                if (mode == "heading")
                {
                    if (heading.Length == 0) return "The heading can't be empty. Delete it instead if you don't need it.";
                    if (!IndexIsType(blocks, index, "subheading")) return "That heading no longer exists. Please reload the page.";
                    blocks[index]["text"] = heading;
                }
                else if (mode == "paragraph")
                {
                    if (paragraph.Length == 0) return "The paragraph can't be empty. Delete it instead if you don't need it.";
                    if (!IndexIsType(blocks, index, "paragraph")) return "That paragraph no longer exists. Please reload the page.";
                    blocks[index]["content"] = paragraph;
                }
                else
                {
                    if (heading.Length == 0 && paragraph.Length == 0) return "Add a heading, a paragraph, or both.";
                    if (heading.Length > 0)
                        blocks.Add(new Dictionary<string, object> { { "type", "subheading" }, { "text", heading } });
                    if (paragraph.Length > 0)
                        blocks.Add(new Dictionary<string, object> { { "type", "paragraph" }, { "content", paragraph } });
                }
                return null;
            }, "modalText");
        }

        // ---------- List ----------
        protected void BtnSaveList_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0) { ShowPopup("Couldn't save", "Please select a lesson first."); return; }

            int index;
            if (!int.TryParse(HdnListIndex.Value, out index)) index = -1;
            List<string> items = ReadPostedList("listItem");

            if (items.Count == 0) { ShowPopup("Couldn't save", "Add at least one list item."); return; }
            if (items.Count > MaxListItems) { ShowPopup("Couldn't save", "A list can have at most " + MaxListItems + " items."); return; }
            foreach (string item in items)
                if (item.Length > MaxListItemChars)
                {
                    ShowPopup("Couldn't save", "Each list item must be " + MaxListItemChars + " characters or fewer.");
                    return;
                }

            SaveBlocks(blocks =>
            {
                if (index < 0)
                {
                    blocks.Add(new Dictionary<string, object> { { "type", "list" }, { "items", items } });
                }
                else
                {
                    if (!IndexIsType(blocks, index, "list")) return "That list no longer exists. Please reload the page.";
                    blocks[index]["items"] = items;
                }
                return null;
            }, null);
        }

        // ---------- Image ----------
        protected void BtnSaveImage_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0) { ShowPopup("Couldn't save", "Please select a lesson first."); return; }

            int index;
            if (!int.TryParse(HdnImageIndex.Value, out index)) index = -1;
            string caption = TxtImageCaption.Text.Trim();

            if (caption.Length > MaxCaptionChars)
            {
                ShowPopup("Couldn't save", "The caption must be " + MaxCaptionChars + " characters or fewer.");
                return;
            }

            // Editing: only the caption can change (to replace the picture, delete it and add a new one).
            if (index >= 0)
            {
                SaveBlocks(blocks =>
                {
                    if (!IndexIsType(blocks, index, "image")) return "That image no longer exists. Please reload the page.";
                    if (caption.Length > 0) blocks[index]["caption"] = caption;
                    else blocks[index].Remove("caption");
                    return null;
                }, null);
                return;
            }

            // Adding: a file is required.
            if (!FileBlockImage.HasFile)
            {
                ShowPopup("Couldn't save", "Please choose an image first.");
                return;
            }

            string savedUrl, imageError;
            if (!TrySaveImage(FileBlockImage.PostedFile, LessonImageFolder, "lesson-" + lessonId, MaxLessonImageBytes,
                              out savedUrl, out imageError))
            {
                ShowPopup("Couldn't save", imageError);
                return;
            }

            string error = ModifyBlocks(blocks =>
            {
                if (FindBlock(blocks, "image") != null) return "This lesson already has an image. Delete it first to add a new one.";
                var img = new Dictionary<string, object> { { "type", "image" }, { "url", savedUrl } };
                if (caption.Length > 0) img["caption"] = caption;
                blocks.Add(img);
                return null;
            });

            if (error != null)
            {
                DeleteUploadedFile(savedUrl, LessonImageFolder);   // don't leave an orphan file behind
                ShowPopup("Couldn't save", error);
                return;
            }
            ReloadLesson();
        }

        // ---------- Video ----------
        protected void BtnConfirmUpload_Click(object sender, EventArgs e)
        {
            if (lessonId <= 0) { ShowPopup("Couldn't save", "Please select a lesson first."); return; }

            string url = TxtVideoUrl.Text.Trim();
            string caption = TxtVideoCaption.Text.Trim();

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                RunScript("reopenVideo", "openModal('modalUploadVideo');");
                ShowPopup("Couldn't save", "Please paste a secure link that starts with https://");
                return;
            }
            if (caption.Length > MaxCaptionChars)
            {
                RunScript("reopenVideo", "openModal('modalUploadVideo');");
                ShowPopup("Couldn't save", "The caption must be " + MaxCaptionChars + " characters or fewer.");
                return;
            }

            SaveBlocks(blocks =>
            {
                var video = FindBlock(blocks, "video");
                if (video == null)
                {
                    video = new Dictionary<string, object> { { "type", "video" } };
                    blocks.Insert(0, video);
                }
                video["url"] = uri.AbsoluteUri;
                if (caption.Length > 0) video["caption"] = caption;
                else video.Remove("caption");
                return null;
            }, null);
        }

        // ---------- Move / edit buttons in the list ----------
        protected void RptBlocks_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int index;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out index)) return;

            if (e.CommandName == "up" || e.CommandName == "down")
            {
                int direction = e.CommandName == "up" ? -1 : 1;
                SaveBlocks(blocks =>
                {
                    // Swap with the neighbouring block that is actually shown in the list.
                    List<int> positions = VisiblePositions(blocks);
                    int k = positions.IndexOf(index);
                    int t = k + direction;
                    if (k < 0 || t < 0 || t >= positions.Count) return null;

                    int target = positions[t];
                    var temp = blocks[index];
                    blocks[index] = blocks[target];
                    blocks[target] = temp;
                    return null;
                }, null);
            }
            else if (e.CommandName == "edit")
            {
                BeginEditBlock(index);
            }
        }

        // Fills the matching pop-up with the block's current values and opens it.
        private void BeginEditBlock(int index)
        {
            List<Dictionary<string, object>> blocks = ParseBlocks(currentContentJson);
            if (index < 0 || index >= blocks.Count || blocks[index] == null) return;

            Dictionary<string, object> block = blocks[index];
            string type = BlockTypeOf(block);
            var json = new JavaScriptSerializer();

            switch (type)
            {
                case "subheading":
                    HdnTextIndex.Value = index.ToString();
                    HdnTextMode.Value = "heading";
                    TxtHeading.Text = RawStr(block, "text");
                    TxtParagraph.Text = "";
                    RunScript("editBlock", "setTextMode('heading'); openModal('modalText');");
                    break;

                case "paragraph":
                    HdnTextIndex.Value = index.ToString();
                    HdnTextMode.Value = "paragraph";
                    TxtHeading.Text = "";
                    TxtParagraph.Text = RawStr(block, "content");
                    RunScript("editBlock", "setTextMode('paragraph'); openModal('modalText');");
                    break;

                case "list":
                    RunScript("editBlock",
                        "fillListModal(" + json.Serialize(ToStringList(GetValue(block, "items"))) + ", " + index + ");" +
                        " openModal('modalList');");
                    break;

                case "image":
                    {
                        HdnImageIndex.Value = index.ToString();
                        TxtImageCaption.Text = RawStr(block, "caption");
                        string url = SafeBlockImageUrl(Str(block, "url", ""));
                        ImgBlockCurrent.Visible = url.Length > 0;
                        if (url.Length > 0) ImgBlockCurrent.Src = url;
                        RunScript("editBlock",
                            "document.getElementById('imageUploadArea').style.display = 'none';" +
                            " document.getElementById('btnRemoveImage').style.display = '';" +
                            " document.getElementById('imageError').style.display = 'none';" +
                            " document.getElementById('title_modalImage').textContent = 'Edit image';" +
                            " openModal('modalImage');");
                        break;
                    }

                case "video":
                    TxtVideoUrl.Text = Str(block, "url", "");
                    TxtVideoCaption.Text = Str(block, "caption", "");
                    RunScript("editBlock", "openModal('modalUploadVideo');");
                    break;
            }
        }

        // ---------- Delete a block ----------
        protected void BtnConfirmRemoveBlock_Click(object sender, EventArgs e)
        {
            int index;
            if (!int.TryParse(HdnRemoveBlockIndex.Value, out index)) return;
            string expectedType = (HdnRemoveBlockType.Value ?? "").Trim().ToLowerInvariant();

            string removedImageUrl = "";
            string error = ModifyBlocks(blocks =>
            {
                if (index < 0 || index >= blocks.Count) return "That item no longer exists. Please reload the page.";
                if (expectedType.Length > 0 && BlockTypeOf(blocks[index]) != expectedType)
                    return "The lesson changed in the meantime. Please reload the page and try again.";

                if (BlockTypeOf(blocks[index]) == "image") removedImageUrl = Str(blocks[index], "url", "");
                blocks.RemoveAt(index);
                return null;
            });

            if (error != null)
            {
                ShowPopup("Couldn't delete it", error);
                return;
            }

            if (removedImageUrl.Length > 0) DeleteUploadedFile(removedImageUrl, LessonImageFolder);
            ReloadLesson();
        }

        // =====================================================================
        // Image upload helpers
        // =====================================================================

        // Validates (size, type, real file header) and saves an uploaded image.
        private bool TrySaveImage(HttpPostedFile file, string folderVirtualPath, string namePrefix, int maxBytes,
                                  out string savedUrl, out string errorMessage)
        {
            savedUrl = "";
            errorMessage = "";

            if (file == null || file.ContentLength <= 0)
            {
                errorMessage = "Please choose an image first.";
                return false;
            }
            if (file.ContentLength > maxBytes)
            {
                errorMessage = "The image must be " + (maxBytes / (1024 * 1024)) + " MB or smaller.";
                return false;
            }

            string extension;
            switch ((file.ContentType ?? "").ToLowerInvariant())
            {
                case "image/jpeg": extension = ".jpg"; break;
                case "image/png": extension = ".png"; break;
                case "image/webp": extension = ".webp"; break;
                default:
                    errorMessage = "Please choose a JPG, PNG or WEBP image.";
                    return false;
            }

            // Check the first bytes too, because the content type is supplied by the browser.
            var header = new byte[12];
            int read = file.InputStream.Read(header, 0, header.Length);
            file.InputStream.Position = 0;
            if (read < header.Length || !HeaderMatches(extension, header))
            {
                errorMessage = "That file isn't a valid " + extension.TrimStart('.').ToUpperInvariant() + " image.";
                return false;
            }

            try
            {
                string folder = Server.MapPath(folderVirtualPath);
                Directory.CreateDirectory(folder);
                string fileName = namePrefix + "-" + Guid.NewGuid().ToString("N") + extension;
                file.SaveAs(Path.Combine(folder, fileName));
                savedUrl = folderVirtualPath + fileName;
                return true;
            }
            catch (IOException ex)
            {
                errorMessage = "The image could not be saved." + (ShowDatabaseErrors ? " (" + ex.Message + ")" : "");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                errorMessage = "The image could not be saved." + (ShowDatabaseErrors ? " (" + ex.Message + ")" : "");
                return false;
            }
        }

        private static bool HeaderMatches(string extension, byte[] h)
        {
            switch (extension)
            {
                case ".jpg": return h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;
                case ".png": return h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47;
                case ".webp":
                    return h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' &&
                           h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P';
                default: return false;
            }
        }

        // Deletes a previously uploaded file, but only if it lives inside the expected upload folder.
        private void DeleteUploadedFile(string virtualPath, string folderVirtualPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath)) return;
            if (!virtualPath.StartsWith(folderVirtualPath, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                string fileName = Path.GetFileName(virtualPath);
                if (string.IsNullOrEmpty(fileName)) return;
                string fullPath = Path.Combine(Server.MapPath(folderVirtualPath), fileName);
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch (IOException ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            catch (UnauthorizedAccessException ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }
    }
}