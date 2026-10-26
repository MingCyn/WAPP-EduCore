using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using Npgsql;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class messages : System.Web.UI.Page
    {
        private const string ConnectionName = "SupabasePostgres";
        private const string SessionUserKey = "UserID";
        private const string SessionRoleKey = "UserRole";
        private const string LoginPage = "~/auth/login.aspx";

        private const bool ShowDatabaseErrors = true;

        private const string SqlTutorCourses =
            "SELECT course_id, title, category FROM courses WHERE user_id = @uid ORDER BY title";

        private const string SqlTutorLessons =
            "SELECT lesson_id, lesson_title FROM lesson WHERE course_id = @cid ORDER BY created_at, lesson_id";

        private const string SqlDiscussionThreads =
            "SELECT p.post_id, p.title, p.content, p.created_at, " +
            "       u.full_name, " +
            "       (SELECT COUNT(*) FROM discussion_replies r WHERE r.post_id = p.post_id) AS reply_count " +
            "FROM discussion_posts p " +
            "JOIN users u ON p.user_id = u.user_id " +
            "WHERE (@cid = 0 OR p.course_id = @cid) " +
            "  AND p.course_id IN (SELECT course_id FROM courses WHERE user_id = @uid) " +
            "ORDER BY p.created_at DESC";

        private const string SqlThreadReplies =
            "SELECT r.reply_id, r.content, r.created_at, u.full_name " +
            "FROM discussion_replies r " +
            "JOIN users u ON r.user_id = u.user_id " +
            "WHERE r.post_id = @pid " +
            "ORDER BY r.created_at ASC";

        private const string SqlInsertReply =
            "INSERT INTO discussion_replies (post_id, user_id, content, created_at) VALUES (@pid, @uid, @content, NOW())";

        private const string SqlInsertAnnouncement =
            "INSERT INTO discussion_posts (user_id, course_id, lesson_id, title, content, post_type, created_at) " +
            "VALUES (@uid, @cid, @lid, @title, @content, 'Announcement', NOW())";

        protected int ActiveThreads, UnansweredCount;
        private int userId;

        public class ReplyRow
        {
            public int ReplyId { get; set; }
            public string Author { get; set; }
            public string Initials { get; set; }
            public string Content { get; set; }
            public string When { get; set; }
        }

        public class ThreadRow
        {
            public int ThreadId { get; set; }
            public string Author { get; set; }
            public string Initials { get; set; }
            public int ColorIndex { get; set; }
            public string When { get; set; }
            public string Title { get; set; }
            public string Excerpt { get; set; }
            public int Replies { get; set; }
            public string RepliesText { get; set; }
            public string StatusKey { get; set; }
            public string StatusLabel { get; set; }
            public string StatusClass { get; set; }
            public List<ReplyRow> ReplyList { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string role = Convert.ToString(Session[SessionRoleKey]);
            if (!int.TryParse(Convert.ToString(Session[SessionUserKey]), out userId) || userId <= 0 ||
                !role.Equals("Tutor", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect(LoginPage);
                return;
            }

            if (!IsPostBack)
            {
                ViewState["UnansweredOnly"] = false;
                LoadFilterCourses();
                LoadAnnouncementCourses();
            }
            LoadThreads();
        }

        private void LoadFilterCourses()
        {
            DdlCourse.Items.Add(new ListItem("All my courses", "0"));
            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                using (var cmd = new NpgsqlCommand(SqlTutorCourses, conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    conn.Open();
                    using (NpgsqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string title = r.IsDBNull(1) ? "(Untitled course)" : Convert.ToString(r[1]);
                            DdlCourse.Items.Add(new ListItem(title, Convert.ToString(r[0])));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private void LoadAnnouncementCourses()
        {
            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                using (var cmd = new NpgsqlCommand(SqlTutorCourses, conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    conn.Open();
                    using (NpgsqlDataReader r = cmd.ExecuteReader())
                    {
                        DdlAnnounceCourse.Items.Clear();
                        DdlAnnounceCourse.Items.Add(new ListItem("Select Course", ""));

                        while (r.Read())
                        {
                            string courseId = Convert.ToString(r[0]);
                            string title = r.IsDBNull(1) ? "(Untitled course)" : Convert.ToString(r[1]);
                            string category = r.IsDBNull(2) ? "General" : Convert.ToString(r[2]);
                            DdlAnnounceCourse.Items.Add(new ListItem($"{title} [{category}]", courseId));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        protected void DdlAnnounceCourse_SelectedIndexChanged(object sender, EventArgs e)
        {
            DdlAnnounceLesson.Items.Clear();
            DdlAnnounceLesson.Items.Add(new ListItem("Select Lesson (Optional)", ""));

            if (int.TryParse(DdlAnnounceCourse.SelectedValue, out int courseId) && courseId > 0)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                    using (var cmd = new NpgsqlCommand(SqlTutorLessons, conn))
                    {
                        cmd.Parameters.AddWithValue("cid", courseId);
                        conn.Open();
                        using (NpgsqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string lessonId = Convert.ToString(r[0]);
                                string lessonTitle = r.IsDBNull(1) ? "(Untitled Lesson)" : Convert.ToString(r[1]);
                                DdlAnnounceLesson.Items.Add(new ListItem(lessonTitle, lessonId));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }

        private void LoadThreads()
        {
            int courseFilter;
            int.TryParse(DdlCourse.SelectedValue, out courseFilter);

            List<ThreadRow> allThreads = GetThreadsFromDatabase(courseFilter);

            ActiveThreads = allThreads.Count;
            UnansweredCount = allThreads.FindAll(t => t.StatusKey == "unanswered").Count;

            bool unansweredOnly = ViewState["UnansweredOnly"] != null && (bool)ViewState["UnansweredOnly"];
            List<ThreadRow> displayThreads = unansweredOnly ? allThreads.FindAll(t => t.StatusKey == "unanswered") : allThreads;

            RptThreads.DataSource = displayThreads;
            RptThreads.DataBind();
            PnlNoThreads.Visible = displayThreads.Count == 0;
        }

        private List<ThreadRow> GetThreadsFromDatabase(int courseId)
        {
            var threads = new List<ThreadRow>();
            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                using (var cmd = new NpgsqlCommand(SqlDiscussionThreads, conn))
                {
                    cmd.Parameters.AddWithValue("cid", courseId);
                    cmd.Parameters.AddWithValue("uid", userId);
                    conn.Open();

                    using (NpgsqlDataReader r = cmd.ExecuteReader())
                    {
                        int index = 0;
                        while (r.Read())
                        {
                            int threadId = r.GetInt32(0);
                            string title = r.IsDBNull(1) ? "(Untitled Thread)" : r.GetString(1);
                            string content = r.IsDBNull(2) ? "" : r.GetString(2);
                            DateTime createdAtUtc = r.GetDateTime(3);
                            string author = r.IsDBNull(4) ? "Unknown User" : r.GetString(4);
                            int replies = Convert.ToInt32(r[5]);

                            var replyList = LoadRepliesForPost(threadId);

                            string key = replies > 0 ? "answered" : "unanswered";

                            threads.Add(new ThreadRow
                            {
                                ThreadId = threadId,
                                Author = author,
                                Initials = GetInitials(author),
                                ColorIndex = index++ % 4,
                                When = RelativeTime(createdAtUtc),
                                Title = title,
                                Excerpt = content,
                                Replies = replies,
                                RepliesText = replies == 1 ? "1 reply" : replies + " replies",
                                StatusKey = key,
                                StatusLabel = key.ToUpperInvariant(),
                                StatusClass = key,
                                ReplyList = replyList
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return threads;
        }

        private List<ReplyRow> LoadRepliesForPost(int postId)
        {
            var list = new List<ReplyRow>();
            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                using (var cmd = new NpgsqlCommand(SqlThreadReplies, conn))
                {
                    cmd.Parameters.AddWithValue("pid", postId);
                    conn.Open();
                    using (NpgsqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string author = r.IsDBNull(3) ? "Unknown" : r.GetString(3);
                            list.Add(new ReplyRow
                            {
                                ReplyId = r.GetInt32(0),
                                Content = r.IsDBNull(1) ? "" : r.GetString(1),
                                When = RelativeTime(r.GetDateTime(2)),
                                Author = author,
                                Initials = GetInitials(author)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            return list;
        }

        protected void DdlCourse_SelectedIndexChanged(object sender, EventArgs e) { }

        protected void BtnResetFilter_Click(object sender, EventArgs e)
        {
            DdlCourse.SelectedValue = "0";
            ViewState["UnansweredOnly"] = false;
            LoadThreads();
        }

        protected void BtnFilterUnanswered_Click(object sender, EventArgs e)
        {
            bool current = ViewState["UnansweredOnly"] != null && (bool)ViewState["UnansweredOnly"];
            ViewState["UnansweredOnly"] = !current;
            LoadThreads();
        }

        protected void RptThreads_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "PostInlineReply")
            {
                if (!int.TryParse(e.CommandArgument.ToString(), out int postId)) return;

                TextBox txtBox = e.Item.FindControl("TxtInlineReply") as TextBox;
                if (txtBox == null || string.IsNullOrWhiteSpace(txtBox.Text))
                {
                    ShowNote("Please write a reply first.", true);
                    return;
                }

                try
                {
                    using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                    using (var cmd = new NpgsqlCommand(SqlInsertReply, conn))
                    {
                        cmd.Parameters.AddWithValue("pid", postId);
                        cmd.Parameters.AddWithValue("uid", userId);
                        cmd.Parameters.AddWithValue("content", txtBox.Text.Trim());
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }

                    ShowNote("Reply posted successfully!", false);
                    LoadThreads();
                }
                catch (Exception)
                {
                    ShowNote("Your reply could not be saved.", true);
                }
            }
        }

        protected void BtnPostAnnouncement_Click(object sender, EventArgs e)
        {
            if (TxtAnnounceTitle.Text.Trim().Length == 0 || TxtAnnounceBody.Text.Trim().Length == 0)
            {
                ShowNote("Please enter both a title and a message for the announcement.", true);
                return;
            }

            if (!int.TryParse(DdlAnnounceCourse.SelectedValue, out int courseId) || courseId <= 0)
            {
                ShowNote("Please select a target course for the announcement.", true);
                return;
            }

            object lessonIdParam = DBNull.Value;
            if (int.TryParse(DdlAnnounceLesson.SelectedValue, out int lessonId) && lessonId > 0)
            {
                lessonIdParam = lessonId;
            }

            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                using (var cmd = new NpgsqlCommand(SqlInsertAnnouncement, conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.Parameters.AddWithValue("cid", courseId);
                    cmd.Parameters.AddWithValue("lid", lessonIdParam);
                    cmd.Parameters.AddWithValue("title", TxtAnnounceTitle.Text.Trim());
                    cmd.Parameters.AddWithValue("content", TxtAnnounceBody.Text.Trim());
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                TxtAnnounceTitle.Text = "";
                TxtAnnounceBody.Text = "";
                DdlAnnounceCourse.SelectedIndex = 0;
                DdlAnnounceLesson.Items.Clear();
                DdlAnnounceLesson.Items.Add(new ListItem("Select Lesson (Optional)", ""));
                ShowNote("Announcement posted successfully!", false);
            }
            catch (Exception)
            {
                ShowNote("Your announcement could not be saved.", true);
            }
        }

        private void ShowNote(string message, bool isError)
        {
            LblNote.Text = HttpUtility.HtmlEncode(message);
            LblNote.CssClass = isError ? "fm-note error" : "fm-note";
            LblNote.Visible = true;
        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            string[] parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string initials = parts[0].Substring(0, 1);
            if (parts.Length > 1) initials += parts[parts.Length - 1].Substring(0, 1);
            return initials.ToUpperInvariant();
        }

        private static string RelativeTime(DateTime utc)
        {
            TimeSpan span = DateTime.UtcNow - utc;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return (int)span.TotalMinutes + "m ago";
            if (span.TotalHours < 24) return (int)span.TotalHours + "h ago";
            if (span.TotalDays < 30) return (int)span.TotalDays + "d ago";
            return utc.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        }
    }
}