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
        // ------------------------------------------------------------------
        // Settings (same values as the other tutor pages)
        // ------------------------------------------------------------------
        private const string ConnectionName = "SupabasePostgres";
        private const string SessionUserKey = "UserID";
        private const string SessionRoleKey = "UserRole";
        private const string LoginPage = "~/auth/login.aspx";

        // While developing, the real database message is shown if the course list cannot be loaded.
        // Set to false before you deploy.
        private const bool ShowDatabaseErrors = true;

        // The tutor's own courses (for the course picker)
        private const string SqlTutorCourses =
            "SELECT course_id, title FROM courses WHERE user_id = @uid ORDER BY title";

        // Counters shown in the toolbar
        protected int ActiveThreads, UnansweredCount, FlaggedCount;

        private int userId;

        // ------------------------------------------------------------------
        // One thread card
        // ------------------------------------------------------------------
        public class ThreadRow
        {
            public int ThreadId { get; set; }
            public string Author { get; set; }
            public string Initials { get; set; }
            public int ColorIndex { get; set; }          // 0-3 -> .fm-c0 ... .fm-c3 (shared with the student page)
            public string When { get; set; }
            public string Title { get; set; }
            public string Excerpt { get; set; }
            public int Replies { get; set; }
            public string RepliesText { get; set; }
            public string StatusKey { get; set; }        // answered | unanswered | flagged
            public string StatusLabel { get; set; }
            public string StatusClass { get; set; }
            public bool IsFlagged { get; set; }
        }

        // ------------------------------------------------------------------
        // Page load
        // ------------------------------------------------------------------
        protected void Page_Load(object sender, EventArgs e)
        {
            // Signed-in tutors only
            string role = Convert.ToString(Session[SessionRoleKey]);
            if (!int.TryParse(Convert.ToString(Session[SessionUserKey]), out userId) || userId <= 0 ||
                !role.Equals("Tutor", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect(LoginPage);
                return;
            }

            if (!IsPostBack) LoadCourses();
            LoadThreads();
        }

        // Course picker: "All my courses" + every course owned by this tutor
        private void LoadCourses()
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
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                var pg = ex as PostgresException;
                ShowNote("Your courses could not be loaded." +
                         (ShowDatabaseErrors ? " (" + (pg != null ? pg.MessageText : ex.Message) + ")" : ""), true);
            }
        }

        // Builds the thread cards, the counters and the Quick Reply "Target Thread" list.
        // Runs on every request so the counters are always right.
        private void LoadThreads()
        {
            // The course filter is DdlCourse.SelectedValue ("0" = all courses). It is passed to GetThreads
            // so the real query can use it.
            int courseFilter;
            int.TryParse(DdlCourse.SelectedValue, out courseFilter);

            List<ThreadRow> threads = GetThreads(courseFilter);

            ActiveThreads = threads.Count;
            UnansweredCount = threads.FindAll(t => t.StatusKey == "unanswered").Count;
            FlaggedCount = threads.FindAll(t => t.StatusKey == "flagged").Count;

            RptThreads.DataSource = threads;
            RptThreads.DataBind();
            PnlNoThreads.Visible = threads.Count == 0;

            // Keep the tutor's choice in Quick Reply while the list is rebuilt
            string keep = IsPostBack ? Request.Form[DdlThread.UniqueID] : null;
            DdlThread.Items.Clear();
            foreach (ThreadRow t in threads)
                DdlThread.Items.Add(new ListItem(t.Title, t.ThreadId.ToString(CultureInfo.InvariantCulture)));
            if (keep != null && DdlThread.Items.FindByValue(keep) != null) DdlThread.SelectedValue = keep;
        }

        // ------------------------------------------------------------------
        // SAMPLE DATA - replace this method with the real query once the forum tables are connected.
        // It returns the same ThreadRow shape the page already uses, so nothing else has to change.
        // ------------------------------------------------------------------
        private List<ThreadRow> GetThreads(int courseId)
        {
            DateTime now = DateTime.UtcNow;
            return new List<ThreadRow>
            {
                MakeThread(1, "Aarav Mehta", now.AddMinutes(-10), "Difficulty understanding Red-Black Tree rotations",
                    "I have reviewed module 2 twice, but the concept of left-right double rotations is still confusing...", 2, false),
                MakeThread(2, "Dev Patel", now.AddHours(-4), "Pointer manipulation error on Linked List exercise",
                    "I get a Segfault when reversing the next pointers in singly linked list sandbox. Is my loop condition correct?", 0, false),
                MakeThread(3, "Siddharth Rao", now.AddDays(-1), "Solution code leaked in general thread!",
                    "Someone posted the direct solution to BST balancing Lab in the general discussion channel.", 1, true),
                MakeThread(4, "Nisha Patel", now.AddDays(-2), "Modulo hash collision strategy",
                    "Is standard linear probing enough for our assignment performance benchmark or should we implement double hashing?", 4, false)
            };
        }

        private static ThreadRow MakeThread(int id, string author, DateTime whenUtc, string title, string excerpt, int replies, bool flagged)
        {
            string key = flagged ? "flagged" : (replies > 0 ? "answered" : "unanswered");
            return new ThreadRow
            {
                ThreadId = id,
                Author = author,
                Initials = GetInitials(author),
                ColorIndex = id % 4,
                When = RelativeTime(whenUtc),
                Title = title,
                Excerpt = excerpt,
                Replies = replies,
                RepliesText = replies == 1 ? "1 reply" : replies + " replies",
                StatusKey = key,
                StatusLabel = key.ToUpperInvariant(),
                StatusClass = key,
                IsFlagged = flagged
            };
        }

        // ------------------------------------------------------------------
        // Buttons
        // ------------------------------------------------------------------
        protected void DdlCourse_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Nothing else to do: Page_Load already rebuilt the list for the newly chosen course.
        }

        protected void BtnPostReply_Click(object sender, EventArgs e)
        {
            if (DdlThread.Items.Count == 0 || string.IsNullOrEmpty(DdlThread.SelectedValue))
            {
                ShowNote("Choose a thread to reply to.", true);
                return;
            }
            if (TxtReply.Text.Trim().Length == 0)
            {
                ShowNote("Please write a reply first.", true);
                return;
            }

            // TODO: save the reply to the forum replies table
            ShowNote("Replies are not saved yet: the discussion tables are not connected to this page.", false);
        }

        protected void BtnPostAnnouncement_Click(object sender, EventArgs e)
        {
            if (TxtAnnounceTitle.Text.Trim().Length == 0 || TxtAnnounceBody.Text.Trim().Length == 0)
            {
                ShowNote("Please enter both a title and a message for the announcement.", true);
                return;
            }

            // TODO: save the announcement to the announcements table
            ShowNote("Announcements are not saved yet: the announcements table is not connected to this page.", false);
        }

        private void ShowNote(string message, bool isError)
        {
            LblNote.Text = HttpUtility.HtmlEncode(message);
            LblNote.CssClass = isError ? "fm-note error" : "fm-note";
            LblNote.Visible = true;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
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
            if (span.TotalMinutes < 60) return Plural((int)span.TotalMinutes, "min") + " ago";
            if (span.TotalHours < 24) return Plural((int)span.TotalHours, "hr") + " ago";
            if (span.TotalDays < 30) return Plural((int)span.TotalDays, "day") + " ago";
            return utc.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        }

        private static string Plural(int n, string unit)
        {
            return n + " " + unit + (n == 1 ? "" : "s");
        }
    }
}