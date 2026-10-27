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
    public partial class courseDetails : System.Web.UI.Page
    {
        // ------------------------------------------------------------------
        // Settings
        // ------------------------------------------------------------------
        private const string ConnectionName = "SupabasePostgres";
        private const string SessionUserKey = "UserID";
        private const string SessionNameKey = "UserFullName";
        private const string LoginPage = "~/auth/login.aspx";
        private const string CourseListPage = "courses.aspx";

        private const string LessonsTable = "public.lesson";

        private const bool ShowDatabaseErrors = true;
        private const string StatusPublished = "published";

        private const int MaxWeeks = 52;
        private const int MaxHoursPerWeek = 40;
        private const string ImageFolder = "~/uploads/courses/";
        private const int MaxImageBytes = 2 * 1024 * 1024;   // 2 MB

        private static readonly Regex DurationPattern = new Regex(
            @"^\s*(\d+)\s*weeks?\s*\(\s*(\d+)\s*hrs?\s*/\s*week\s*\)\s*$", RegexOptions.IgnoreCase);

        private int userId;
        private int courseId;
        private string courseStatus = "";
        private string currentImage = "";

        protected int CourseIdValue;

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

        // ------------------------------------------------------------------
        // SQL Statements
        // ------------------------------------------------------------------
        private const string SqlCourse =
            "SELECT title, category, course_level, duration, course_img, status, course_description, reference " +
            "FROM courses WHERE course_id = @cid AND user_id = @uid";

        private const string SqlCourseFullUpdate =
            "UPDATE courses SET course_description = @desc, reference = CAST(@reference AS jsonb), updated_at = now() " +
            "WHERE course_id = @cid AND user_id = @uid";

        private const string SqlLessons =
            "SELECT lesson_id, lesson_title, lesson_content::text, quiz_id " +
            "FROM " + LessonsTable + " WHERE course_id = @cid ORDER BY created_at, lesson_id";

        private const string SqlEnrolledCount =
            "SELECT COUNT(*) FROM enrollment WHERE course_id = @cid";

        private const string SqlUserName =
            "SELECT full_name FROM public.users WHERE user_id = @uid";

        private const string SqlCourseInfoUpdate =
            "UPDATE courses SET title = @title, category = @topic, duration = @duration, course_level = @level, " +
            "course_img = COALESCE(CAST(@img AS text), course_img), updated_at = now() " +
            "WHERE course_id = @cid AND user_id = @uid";

        // ------------------------------------------------------------------
        // Data shapes
        // ------------------------------------------------------------------
        public class ComponentView
        {
            public string IconHtml { get; set; }
            public string Title { get; set; }
            public string Meta { get; set; }
            public bool IsDraft { get; set; }
        }

        public class LessonView
        {
            public int LessonId { get; set; }
            public string Number { get; set; }
            public string Title { get; set; }
            public string Subtitle { get; set; }
            public string IconHtml { get; set; }
            public string EditUrl { get; set; }
            public bool ShowComponents { get; set; }
            public List<ComponentView> Components { get; set; }
        }

        public class OutcomeView
        {
            public int Id { get; set; }
            public string Text { get; set; }
        }

        public class ReferenceView
        {
            public int Id { get; set; }
            public string TitleHtml { get; set; }
            public string HostText { get; set; }
        }

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

            if (!int.TryParse(Request.QueryString["courseId"], out courseId))
            {
                Response.Redirect(CourseListPage);
                return;
            }
            CourseIdValue = courseId;

            try
            {
                if (!IsPostBack)
                {
                    FillDurationLists();
                }

                LoadPage();
                if (!IsPostBack) ShowNoticeFromQuery();
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                var pg = ex as PostgresException;
                string detail = pg != null ? pg.MessageText : ex.Message;
                ShowMessage("We couldn't load this course right now." +
                          (ShowDatabaseErrors ? " (" + detail + ")" : " Please try again."), true);
            }
        }

        private void LoadPage()
        {
            using (NpgsqlConnection conn = OpenConnection())
            {
                string title, topic, level, duration, image, status, description, referenceJson;
                if (!TryGetCourse(conn, out title, out topic, out level, out duration, out image, out status, out description, out referenceJson))
                {
                    Response.Redirect(CourseListPage);
                    return;
                }

                currentImage = image;
                courseStatus = NormalizeStatus(status);
                bool published = courseStatus == StatusPublished;
                string displayTitle = string.IsNullOrWhiteSpace(title) ? "Untitled course" : title;

                Page.Title = "EduCore - " + displayTitle;
                LitCrumbTitle.Text = Server.HtmlEncode(displayTitle);
                LitTitle.Text = Server.HtmlEncode(displayTitle);

                LblStatusPill.Text = published ? "Published" : "Draft";
                LblStatusPill.CssClass = "cd-pill " + (published ? "published" : "draft");
                LitMeta.Text = Server.HtmlEncode(JoinParts(topic, level, duration));

                // 2) Overview card
                LitThumb.Text = BuildThumb(image);
                LitDescription.Text = string.IsNullOrWhiteSpace(description)
                    ? "<span class=\"cd-muted\">No overview yet. Add learning outcomes to populate this section.</span>"
                    : EncodeWithBreaks(description);

                string tutorName = GetTutorName(conn);
                LitCreatedBy.Text = Server.HtmlEncode(tutorName.Length > 0 ? tutorName : "you");

                if (!IsPostBack)
                {
                    TxtCourseTitle.Text = title;
                    TxtCourseTopic.Text = topic;
                    SelectIfPresent(DdlCourseLevel, (level ?? "").Trim().ToUpperInvariant());

                    string weeks, hours;
                    ParseDuration(duration, out weeks, out hours);
                    SelectIfPresent(DdlCourseWeeks, weeks);
                    SelectIfPresent(DdlCourseHours, hours);
                }

                // 3) Publishing overview
                PnlLiveNotice.Visible = published;
                LblPublishPill.Text = published ? "Published course" : "Draft course";
                LblPublishPill.CssClass = "cd-pill " + (published ? "published" : "draft");
                LitPublishText.Text = Server.HtmlEncode(published
                    ? "Students can access published lessons. Draft components stay hidden until published separately."
                    : "This course is a draft and hidden from students. Publish it from the course builder when it is ready.");

                int enrolled = CountEnrolled(conn);
                LitEnrolled.Text = enrolled >= 0 ? enrolled.ToString(CultureInfo.InvariantCulture) : "-";

                // 4) Syllabus
                List<LessonView> lessons = LoadLessons(conn);
                RptLessons.DataSource = lessons;
                RptLessons.DataBind();
                PnlNoLessons.Visible = lessons.Count == 0;
                LitLessonCount.Text = lessons.Count.ToString(CultureInfo.InvariantCulture);

                // 5) Learning outcomes
                var outcomes = new List<OutcomeView>();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    var lines = description.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    int index = 0;
                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.Length > 0)
                        {
                            outcomes.Add(new OutcomeView { Id = index++, Text = trimmed });
                        }
                    }
                }

                RptOutcomes.DataSource = outcomes;
                RptOutcomes.DataBind();
                PnlOutcomeList.Visible = outcomes.Count > 0;
                PnlNoOutcomes.Visible = outcomes.Count == 0;

                // 6) References
                var references = new List<ReferenceView>();
                if (!string.IsNullOrWhiteSpace(referenceJson))
                {
                    try
                    {
                        var refStrings = new JavaScriptSerializer().Deserialize<List<string>>(referenceJson);
                        if (refStrings != null)
                        {
                            for (int i = 0; i < refStrings.Count; i++)
                            {
                                references.Add(new ReferenceView { Id = i, TitleHtml = HttpUtility.HtmlEncode(refStrings[i]), HostText = "" });
                            }
                        }
                    }
                    catch { }
                }

                RptReferences.DataSource = references;
                RptReferences.DataBind();
                PnlReferenceList.Visible = references.Count > 0;
                PnlNoReferences.Visible = references.Count == 0;
            }
        }

        // ------------------------------------------------------------------
        // Queries & Helpers
        // ------------------------------------------------------------------
        private bool TryGetCourse(NpgsqlConnection conn, out string title, out string topic, out string level,
                                out string duration, out string image, out string status, out string description, out string referenceJson)
        {
            title = topic = level = duration = image = status = description = referenceJson = "";
            using (var cmd = new NpgsqlCommand(SqlCourse, conn))
            {
                cmd.Parameters.AddWithValue("cid", courseId);
                cmd.Parameters.AddWithValue("uid", userId);
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return false;
                    title = r.IsDBNull(0) ? "" : Convert.ToString(r[0]);
                    topic = r.IsDBNull(1) ? "" : Convert.ToString(r[1]);
                    level = r.IsDBNull(2) ? "" : Convert.ToString(r[2]);
                    duration = r.IsDBNull(3) ? "" : Convert.ToString(r[3]);
                    image = r.IsDBNull(4) ? "" : Convert.ToString(r[4]);
                    status = r.IsDBNull(5) ? "" : Convert.ToString(r[5]);
                    description = r.IsDBNull(6) ? "" : Convert.ToString(r[6]);
                    referenceJson = r.IsDBNull(7) ? "" : Convert.ToString(r[7]);
                    return true;
                }
            }
        }

        private string GetTutorName(NpgsqlConnection conn)
        {
            string name = Convert.ToString(Session[SessionNameKey]);
            if (!string.IsNullOrWhiteSpace(name)) return name.Trim();

            using (var cmd = new NpgsqlCommand(SqlUserName, conn))
            {
                cmd.Parameters.AddWithValue("uid", userId);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    name = Convert.ToString(result).Trim();
                    Session[SessionNameKey] = name;
                    return name;
                }
            }
            return "";
        }

        private int CountEnrolled(NpgsqlConnection conn)
        {
            try
            {
                using (var cmd = new NpgsqlCommand(SqlEnrolledCount, conn))
                {
                    cmd.Parameters.AddWithValue("cid", courseId);
                    object countResult = cmd.ExecuteScalar();
                    return (countResult != null && countResult != DBNull.Value) ? Convert.ToInt32(countResult) : 0;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private List<LessonView> LoadLessons(NpgsqlConnection conn)
        {
            var raw = new List<object[]>();
            using (var cmd = new NpgsqlCommand(SqlLessons, conn))
            {
                cmd.Parameters.AddWithValue("cid", courseId);
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        raw.Add(new object[]
                        {
                            Convert.ToInt32(r[0]),
                            r.IsDBNull(1) ? "(Untitled lesson)" : r.GetString(1),
                            r.IsDBNull(2) ? "" : r.GetString(2),
                            r.IsDBNull(3) ? (long?)null : Convert.ToInt64(r[3])
                        });
                    }
                }
            }

            var list = new List<LessonView>();
            for (int i = 0; i < raw.Count; i++)
            {
                list.Add(BuildLessonView(i + 1, (int)raw[i][0], (string)raw[i][1], (string)raw[i][2], (long?)raw[i][3]));
            }
            return list;
        }

        private LessonView BuildLessonView(int number, int lessonId, string title, string json, long? quizId)
        {
            List<Dictionary<string, object>> blocks = ParseBlocks(json);
            Dictionary<string, object> video = FindBlock(blocks, "video");
            Dictionary<string, object> interactive = FindBlock(blocks, "interactive");
            Dictionary<string, object> lab = FindBlock(blocks, "lab");

            int textBlocks = 0;
            foreach (Dictionary<string, object> b in blocks)
            {
                if (b == null) continue;
                string type = BlockTypeOf(b);
                if (type != "video" && type != "interactive" && type != "lab") textBlocks++;
            }

            var components = new List<ComponentView>();

            if (video != null)
            {
                string url = Str(video, "url", "");
                string host = "";
                Uri uri;
                if (Uri.TryCreate(url, UriKind.Absolute, out uri)) host = uri.Host;
                components.Add(new ComponentView
                {
                    IconHtml = IconVideo(),
                    Title = Str(video, "caption", "Lesson video"),
                    Meta = "Video" + (host.Length > 0 ? " \u2022 " + host : ""),
                    IsDraft = false
                });
            }

            if (interactive != null)
            {
                bool published = IsPublished(interactive);
                string language = Str(interactive, "language", "");
                components.Add(new ComponentView
                {
                    IconHtml = IconCode(),
                    Title = Str(interactive, "title", "Interactive object"),
                    Meta = (language.Length > 0 ? language + " Sandbox" : "Interactive object") +
                           " \u2022 " + (published ? "Published" : "Not visible to students"),
                    IsDraft = !published
                });
            }

            if (quizId.HasValue)
            {
                components.Add(new ComponentView
                {
                    IconHtml = IconQuestion(),
                    Title = "Lesson assessment",
                    Meta = "Assessment \u2022 Quiz #" + quizId.Value,
                    IsDraft = false
                });
            }

            if (lab != null)
            {
                bool published = IsPublished(lab);
                string env = Str(lab, "environment", "");
                components.Add(new ComponentView
                {
                    IconHtml = IconFlask(),
                    Title = Str(lab, "title", "Virtual lab"),
                    Meta = (env.Length > 0 ? env + " Sandbox" : "Virtual lab") +
                           " \u2022 " + (published ? "Published" : "Not visible to students"),
                    IsDraft = !published
                });
            }

            int count = components.Count;
            bool mixed = count >= 2 || (count >= 1 && textBlocks > 0);

            string kind;
            string icon;
            if (count == 0 && textBlocks == 0) { kind = "No content yet"; icon = IconDocument(); }
            else if (count == 0) { kind = "Article"; icon = IconDocument(); }
            else if (mixed) { kind = "Mixed content"; icon = IconCode(); }
            else if (video != null) { kind = "Video"; icon = IconVideo(); }
            else if (interactive != null) { kind = "Interactive object"; icon = IconCode(); }
            else if (quizId.HasValue) { kind = "Assessment"; icon = IconQuestion(); }
            else { kind = "Virtual lab"; icon = IconFlask(); }

            string subtitle = kind;
            if (mixed) subtitle += " \u2022 " + count + (count == 1 ? " component" : " components");

            int drafts = 0;
            foreach (ComponentView c in components) if (c.IsDraft) drafts++;
            if (drafts > 0) subtitle += " \u2022 " + drafts + (drafts == 1 ? " draft component" : " draft components");

            return new LessonView
            {
                LessonId = lessonId,
                Number = number + ".",
                Title = title,
                Subtitle = subtitle,
                IconHtml = icon,
                EditUrl = "courseBuilder.aspx?courseId=" + courseId + "&lessonId=" + lessonId,
                ShowComponents = mixed,
                Components = components
            };
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
                if (b != null && BlockTypeOf(b) == type) return b;
            }
            return null;
        }

        private static string BlockTypeOf(Dictionary<string, object> block) => block == null ? "" : Str(block, "type", "").ToLowerInvariant();
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
        private static bool IsPublished(Dictionary<string, object> block)
        {
            object v;
            if (block.TryGetValue("published", out v) && v is bool) return (bool)v;
            return true;
        }

        private static string NormalizeStatus(string status)
        {
            string s = (status ?? "").Trim().ToLowerInvariant().Replace(' ', '_');
            return s == StatusPublished ? StatusPublished : "draft";
        }

        private static string JoinParts(params string[] parts)
        {
            var kept = new List<string>();
            foreach (string p in parts)
                if (!string.IsNullOrWhiteSpace(p)) kept.Add(p.Trim());
            return string.Join(" \u2022 ", kept.ToArray());
        }

        private static string EncodeWithBreaks(string text)
        {
            string encoded = HttpUtility.HtmlEncode(text.Trim());
            return encoded.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br />");
        }

        private string BuildThumb(string path)
        {
            string url = "";
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (path.StartsWith(ImageFolder, StringComparison.OrdinalIgnoreCase)) url = ResolveUrl(path);
                else url = SafeHttpsUrl(path);
            }
            if (url.Length > 0) return "<img src=\"" + HttpUtility.HtmlAttributeEncode(url) + "\" alt=\"Course image\" />";
            return IconImagePlaceholder();
        }

        private static string SafeHttpsUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            Uri uri;
            if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri) && uri.Scheme == Uri.UriSchemeHttps) return uri.AbsoluteUri;
            return "";
        }

        private static string IconVideo() => "<svg viewBox=\"0 0 16 16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linejoin=\"round\"><rect x=\"1.5\" y=\"3.5\" width=\"9\" height=\"9\" rx=\"1.5\" /><path d=\"M10.5 7l4-2v6l-4-2z\" /></svg>";
        private static string IconDocument() => "<svg viewBox=\"0 0 16 16\" fill=\"currentColor\"><path d=\"M3 1.5h6.5L13 5v9.5H3z\" /><path d=\"M9.5 1.5V5H13\" fill=\"none\" stroke=\"#fff\" stroke-width=\"1\" /><path d=\"M5 8h6M5 10.5h6\" stroke=\"#fff\" stroke-width=\"1\" /></svg>";
        private static string IconCode() => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M5 7l5 5-5 5\" /><path d=\"M13 18h6\" /></svg>";
        private static string IconQuestion() => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"12\" r=\"9\" /><path d=\"M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.7.3-1 .9-1 1.7\" /><circle cx=\"12\" cy=\"17\" r=\".6\" fill=\"currentColor\" /></svg>";
        private static string IconFlask() => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M9 3h6M10 3v6l-5.5 9.5A1.5 1.5 0 0 0 5.8 21h12.4a1.5 1.5 0 0 0 1.3-2.5L14 9V3\" /><path d=\"M7.5 15h9\" /></svg>";
        private static string IconImagePlaceholder() => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><rect x=\"3\" y=\"4\" width=\"18\" height=\"16\" rx=\"2\" /><circle cx=\"9\" cy=\"10\" r=\"1.6\" /><path d=\"M21 16l-5-5-8 8\" /></svg>";

        private void FillDurationLists()
        {
            for (int w = 1; w <= MaxWeeks; w++)
                DdlCourseWeeks.Items.Add(new ListItem(w + (w == 1 ? " week" : " weeks"), w.ToString(CultureInfo.InvariantCulture)));

            for (int h = 1; h <= MaxHoursPerWeek; h++)
                DdlCourseHours.Items.Add(new ListItem(h + (h == 1 ? " hour" : " hours") + " per week", h.ToString(CultureInfo.InvariantCulture)));
        }

        private static string FormatDuration(int weeks, int hoursPerWeek) => weeks + (weeks == 1 ? " week" : " weeks") + " (" + hoursPerWeek + (hoursPerWeek == 1 ? " hr" : " hrs") + "/week)";

        private static void ParseDuration(string text, out string weeks, out string hours)
        {
            weeks = hours = "";
            if (string.IsNullOrWhiteSpace(text)) return;
            Match m = DurationPattern.Match(text);
            if (m.Success) { weeks = m.Groups[1].Value; hours = m.Groups[2].Value; return; }
            int plain;
            if (int.TryParse(text.Trim(), out plain) && plain > 0) weeks = plain.ToString(CultureInfo.InvariantCulture);
        }

        private static void SelectIfPresent(DropDownList list, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            ListItem item = list.Items.FindByValue(value);
            if (item != null) { list.ClearSelection(); item.Selected = true; }
        }

        private bool TrySaveCourseImage(out string savedPath, out string error)
        {
            savedPath = null;
            error = null;
            HttpPostedFile file = FileCourseImg.PostedFile;

            if (file == null || file.ContentLength == 0)
                return true; // No new file uploaded

            // Enforce single file selection safeguard
            if (FileCourseImg.PostedFiles != null && FileCourseImg.PostedFiles.Count > 1)
            {
                error = "Please upload only one image file at a time.";
                return false;
            }

            if (file.ContentLength > MaxImageBytes)
            {
                error = "The image must be 2 MB or smaller.";
                return false;
            }

            try
            {
                var head = new byte[12];
                int read = file.InputStream.Read(head, 0, head.Length);
                file.InputStream.Position = 0; // Reset stream pointer

                string extension = DetectImageExtension(head, read);
                if (extension == null)
                {
                    error = "Please choose a valid JPG, PNG or WEBP image format.";
                    return false;
                }

                string folder = Server.MapPath(ImageFolder);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = courseId + "-" + Guid.NewGuid().ToString("N") + extension;
                string fullPath = Path.Combine(folder, fileName);

                file.SaveAs(fullPath);
                savedPath = ImageFolder + fileName;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("File Upload Exception: " + ex.ToString());
                error = "The image could not be saved due to a server error.";
                return false;
            }
        }

        // Event handler when the tutor clicks the "X" button on the current image preview
        protected void BtnRemoveImg_Click(object sender, EventArgs e)
        {
            HfRemoveImage.Value = "true";
            PnlCurrentImg.Visible = false; // Hide preview instantly on UI
            ReopenCourseInfoDialog("Image marked for removal. Click 'Save changes' to apply.");
        }

        private static string DetectImageExtension(byte[] head, int length)
        {
            if (length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return ".jpg";
            if (length >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47 && head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A) return ".png";
            return null;
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
            catch { }
        }

        private string DetailsUrl(string notice) => "courseDetails.aspx?courseId=" + courseId + (notice != null ? "&notice=" + notice : "");

        protected void BtnSaveOutcome_Click(object sender, EventArgs e)
        {
            string newOutcome = Regex.Replace((TxtOutcome.Text ?? "").Trim(), @"\s+", " ");
            if (newOutcome.Length == 0)
            {
                ReopenDialog("modalOutcome", "outcomeError", "Please enter a learning outcome.");
                return;
            }

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                {
                    string currentDesc = "";
                    string currentRef = "";
                    using (var readCmd = new NpgsqlCommand("SELECT course_description, reference FROM courses WHERE course_id = @cid AND user_id = @uid", conn))
                    {
                        readCmd.Parameters.AddWithValue("cid", courseId);
                        readCmd.Parameters.AddWithValue("uid", userId);
                        using (var reader = readCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentDesc = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                currentRef = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            }
                        }
                    }

                    string updatedDesc = string.IsNullOrWhiteSpace(currentDesc) ? newOutcome : currentDesc + Environment.NewLine + newOutcome;

                    using (var updateCmd = new NpgsqlCommand(SqlCourseFullUpdate, conn))
                    {
                        updateCmd.Parameters.AddWithValue("desc", updatedDesc);
                        updateCmd.Parameters.AddWithValue("reference", string.IsNullOrEmpty(currentRef) ? (object)DBNull.Value : currentRef);
                        updateCmd.Parameters.AddWithValue("cid", courseId);
                        updateCmd.Parameters.AddWithValue("uid", userId);
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                ReopenDialog("modalOutcome", "outcomeError", "We couldn't save the outcome.");
                return;
            }

            Response.Redirect(DetailsUrl("outcome-added"));
        }

        protected void RptOutcomes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "delete" || !int.TryParse(Convert.ToString(e.CommandArgument), out int removeIndex)) return;

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                {
                    string currentDesc = "";
                    string currentRef = "";
                    using (var readCmd = new NpgsqlCommand("SELECT course_description, reference FROM courses WHERE course_id = @cid AND user_id = @uid", conn))
                    {
                        readCmd.Parameters.AddWithValue("cid", courseId);
                        readCmd.Parameters.AddWithValue("uid", userId);
                        using (var reader = readCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentDesc = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                currentRef = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            }
                        }
                    }

                    var lines = new List<string>(currentDesc.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                    if (removeIndex >= 0 && removeIndex < lines.Count)
                    {
                        lines.RemoveAt(removeIndex);
                    }
                    string updatedDesc = string.Join(Environment.NewLine, lines);

                    using (var updateCmd = new NpgsqlCommand(SqlCourseFullUpdate, conn))
                    {
                        updateCmd.Parameters.AddWithValue("desc", string.IsNullOrEmpty(updatedDesc) ? (object)DBNull.Value : updatedDesc);
                        updateCmd.Parameters.AddWithValue("reference", string.IsNullOrEmpty(currentRef) ? (object)DBNull.Value : currentRef);
                        updateCmd.Parameters.AddWithValue("cid", courseId);
                        updateCmd.Parameters.AddWithValue("uid", userId);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                LoadPage();
                ShowMessage("Learning outcome removed successfully.", false);
            }
            catch (Exception)
            {
                ShowMessage("We couldn't remove the outcome.", true);
            }
        }

        protected void RptReferences_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "delete" || !int.TryParse(Convert.ToString(e.CommandArgument), out int removeIndex)) return;

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                {
                    string currentDesc = "";
                    string currentRefJson = "";
                    using (var readCmd = new NpgsqlCommand("SELECT course_description, reference FROM courses WHERE course_id = @cid AND user_id = @uid", conn))
                    {
                        readCmd.Parameters.AddWithValue("cid", courseId);
                        readCmd.Parameters.AddWithValue("uid", userId);
                        using (var reader = readCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentDesc = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                currentRefJson = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            }
                        }
                    }

                    var refList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(currentRefJson))
                    {
                        try { refList = new JavaScriptSerializer().Deserialize<List<string>>(currentRefJson) ?? new List<string>(); }
                        catch { }
                    }

                    if (removeIndex >= 0 && removeIndex < refList.Count)
                    {
                        refList.RemoveAt(removeIndex);
                    }

                    string updatedRefJson = refList.Count > 0 ? new JavaScriptSerializer().Serialize(refList) : null;

                    using (var updateCmd = new NpgsqlCommand(SqlCourseFullUpdate, conn))
                    {
                        updateCmd.Parameters.AddWithValue("desc", string.IsNullOrEmpty(currentDesc) ? (object)DBNull.Value : currentDesc);
                        updateCmd.Parameters.AddWithValue("reference", updatedRefJson == null ? (object)DBNull.Value : updatedRefJson);
                        updateCmd.Parameters.AddWithValue("cid", courseId);
                        updateCmd.Parameters.AddWithValue("uid", userId);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                LoadPage();
                ShowMessage("Reference removed successfully.", false);
            }
            catch (Exception)
            {
                ShowMessage("We couldn't remove the reference.", true);
            }
        }

        protected void BtnSaveReference_Click(object sender, EventArgs e)
        {
            string title = Regex.Replace((TxtRefTitle.Text ?? "").Trim(), @"\s+", " ");
            if (title.Length == 0)
            {
                ReopenDialog("modalReference", "referenceError", "Please enter a reference.");
                return;
            }

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                {
                    string currentDesc = "";
                    string currentRefJson = "";
                    using (var readCmd = new NpgsqlCommand("SELECT course_description, reference FROM courses WHERE course_id = @cid AND user_id = @uid", conn))
                    {
                        readCmd.Parameters.AddWithValue("cid", courseId);
                        readCmd.Parameters.AddWithValue("uid", userId);
                        using (var reader = readCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentDesc = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                currentRefJson = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            }
                        }
                    }

                    var refList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(currentRefJson))
                    {
                        try { refList = new JavaScriptSerializer().Deserialize<List<string>>(currentRefJson) ?? new List<string>(); }
                        catch { }
                    }

                    refList.Add(title);
                    string updatedRefJson = new JavaScriptSerializer().Serialize(refList);

                    using (var updateCmd = new NpgsqlCommand(SqlCourseFullUpdate, conn))
                    {
                        updateCmd.Parameters.AddWithValue("desc", string.IsNullOrEmpty(currentDesc) ? (object)DBNull.Value : currentDesc);
                        updateCmd.Parameters.AddWithValue("reference", updatedRefJson);
                        updateCmd.Parameters.AddWithValue("cid", courseId);
                        updateCmd.Parameters.AddWithValue("uid", userId);
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                ReopenDialog("modalReference", "referenceError", "We couldn't save the reference.");
                return;
            }

            Response.Redirect(DetailsUrl("reference-added"));
        }

        private void ReopenDialog(string modalId, string errorBoxId, string message)
        {
            string script =
                "window.addEventListener('load', function () {" +
                "  var box = document.getElementById('" + errorBoxId + "');" +
                "  if (box) { box.textContent = " + new JavaScriptSerializer().Serialize(message) + "; box.style.display = 'block'; }" +
                "  openModal('" + modalId + "');" +
                "});";
            ClientScript.RegisterStartupScript(GetType(), "dialogError", script, true);
        }

        private void ReopenCourseInfoDialog(string message)
        {
            ReopenDialog("modalCourseInfo", "courseInfoError", message);
        }

        private void ShowNoticeFromQuery()
        {
            switch (Request.QueryString["notice"])
            {
                case "outcome-added": ShowMessage("Learning outcome added.", false); break;
                case "reference-added": ShowMessage("Reference added.", false); break;
            }
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

            string newImage = null;
            string imageError = null;
            bool removeRequested = HfRemoveImage.Value == "true";

            // If a new file is uploaded, process it
            if (FileCourseImg.HasFile)
            {
                if (!TrySaveCourseImage(out newImage, out imageError))
                {
                    ReopenCourseInfoDialog(imageError);
                    return;
                }
            }

            string duration = FormatDuration(weeks, hours);
            string oldImage = "";

            try
            {
                int rows;
                using (NpgsqlConnection conn = OpenConnection())
                {
                    // 1. Fetch current image path from DB
                    using (var selectCmd = new NpgsqlCommand("SELECT course_img FROM courses WHERE course_id = @cid AND user_id = @uid", conn))
                    {
                        selectCmd.Parameters.AddWithValue("cid", courseId);
                        selectCmd.Parameters.AddWithValue("uid", userId);
                        object dbImg = selectCmd.ExecuteScalar();
                        if (dbImg != null && dbImg != DBNull.Value)
                        {
                            oldImage = Convert.ToString(dbImg);
                        }
                    }

                    // Determine image parameter value for update query
                    object imgParam;
                    if (removeRequested && !FileCourseImg.HasFile)
                    {
                        imgParam = DBNull.Value; // Clear image if requested and no new file uploaded
                    }
                    else if (newImage != null)
                    {
                        imgParam = newImage;     // Use newly uploaded image path
                    }
                    else
                    {
                        imgParam = DBNull.Value; // Keep existing via COALESCE logic in SQL update
                    }

                    // 2. Perform update query
                    string updateSql = "UPDATE courses SET title = @title, category = @topic, duration = @duration, course_level = @level, " +
                                       (removeRequested && !FileCourseImg.HasFile ? "course_img = NULL, " : "course_img = COALESCE(CAST(@img AS text), course_img), ") +
                                       "updated_at = now() WHERE course_id = @cid AND user_id = @uid";

                    using (var cmd = new NpgsqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("title", title);
                        cmd.Parameters.AddWithValue("topic", topic);
                        cmd.Parameters.AddWithValue("duration", duration);
                        cmd.Parameters.AddWithValue("level", level);
                        if (!removeRequested || FileCourseImg.HasFile)
                        {
                            cmd.Parameters.AddWithValue("img", newImage == null ? (object)DBNull.Value : newImage);
                        }
                        cmd.Parameters.AddWithValue("cid", courseId);
                        cmd.Parameters.AddWithValue("uid", userId);
                        rows = cmd.ExecuteNonQuery();
                    }
                }

                if (rows == 0)
                {
                    if (newImage != null) DeleteStoredImage(newImage);
                    ReopenCourseInfoDialog("This course could not be found.");
                    return;
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                if (newImage != null) DeleteStoredImage(newImage);

                var pg = ex as PostgresException;
                string detail = pg != null ? pg.MessageText : ex.Message;
                ReopenCourseInfoDialog("We couldn't save the course." + (ShowDatabaseErrors ? " (" + detail + ")" : " Please try again."));
                return;
            }

            // 3. Clean up physical files from disk if replaced or removed
            if (!string.IsNullOrWhiteSpace(oldImage))
            {
                if (removeRequested || newImage != null)
                {
                    DeleteStoredImage(oldImage);
                }
            }

            Response.Redirect(Request.RawUrl);
        }

        protected void BtnConfirmDelete_Click(object sender, EventArgs e)
        {
            string deleteType = HfDeleteType.Value;
            if (!int.TryParse(HfTargetId.Value, out int removeIndex)) return;

            try
            {
                using (NpgsqlConnection conn = OpenConnection())
                {
                    string currentDesc = "";
                    string currentRefJson = "";
                    using (var readCmd = new NpgsqlCommand("SELECT course_description, reference FROM courses WHERE course_id = @cid AND user_id = @uid", conn))
                    {
                        readCmd.Parameters.AddWithValue("cid", courseId);
                        readCmd.Parameters.AddWithValue("uid", userId);
                        using (var reader = readCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentDesc = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                currentRefJson = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            }
                        }
                    }

                    if (deleteType == "outcome")
                    {
                        var lines = new List<string>(currentDesc.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                        if (removeIndex >= 0 && removeIndex < lines.Count)
                        {
                            lines.RemoveAt(removeIndex);
                        }
                        string updatedDesc = string.Join(Environment.NewLine, lines);

                        using (var updateCmd = new NpgsqlCommand(SqlCourseFullUpdate, conn))
                        {
                            updateCmd.Parameters.AddWithValue("desc", string.IsNullOrEmpty(updatedDesc) ? (object)DBNull.Value : updatedDesc);
                            updateCmd.Parameters.AddWithValue("reference", string.IsNullOrEmpty(currentRefJson) ? (object)DBNull.Value : currentRefJson);
                            updateCmd.Parameters.AddWithValue("cid", courseId);
                            updateCmd.Parameters.AddWithValue("uid", userId);
                            updateCmd.ExecuteNonQuery();
                        }

                        LoadPage();
                        ShowMessage("Learning outcome removed successfully.", false);
                    }
                    else if (deleteType == "reference")
                    {
                        var refList = new List<string>();
                        if (!string.IsNullOrWhiteSpace(currentRefJson))
                        {
                            try { refList = new JavaScriptSerializer().Deserialize<List<string>>(currentRefJson) ?? new List<string>(); }
                            catch { }
                        }

                        if (removeIndex >= 0 && removeIndex < refList.Count)
                        {
                            refList.RemoveAt(removeIndex);
                        }

                        string updatedRefJson = refList.Count > 0 ? new JavaScriptSerializer().Serialize(refList) : null;

                        using (var updateCmd = new NpgsqlCommand(SqlCourseFullUpdate, conn))
                        {
                            updateCmd.Parameters.AddWithValue("desc", string.IsNullOrEmpty(currentDesc) ? (object)DBNull.Value : currentDesc);
                            updateCmd.Parameters.AddWithValue("reference", updatedRefJson == null ? (object)DBNull.Value : updatedRefJson);
                            updateCmd.Parameters.AddWithValue("cid", courseId);
                            updateCmd.Parameters.AddWithValue("uid", userId);
                            updateCmd.ExecuteNonQuery();
                        }

                        LoadPage();
                        ShowMessage("Reference removed successfully.", false);
                    }
                }
            }
            catch (Exception)
            {
                ShowMessage("We couldn't process your request.", true);
            }
        }

        private void ShowMessage(string message, bool isError)
        {
            LblStatus.Text = Server.HtmlEncode(message);
            LblStatus.CssClass = isError ? "page-status error" : "page-status";
            LblStatus.Visible = true;
        }
    }
}