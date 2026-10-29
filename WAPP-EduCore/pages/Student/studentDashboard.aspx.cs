using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace WAPP_EduCore.pages.Student
{
    public partial class WebForm2 : System.Web.UI.Page
    {
        // Malaysia is UTC+8. Days are counted in local time.
        private const int UtcOffsetHours = 8;
        private const string Tz = "Asia/Kuala_Lumpur";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public class CourseItem
        {
            public long CourseId { get; set; }
            public string Title { get; set; }
            public string Tutor { get; set; }
            public int Percent { get; set; }
            public string ThumbStyle { get; set; }
            public string Url { get; set; }
            public bool IsComplete { get; set; }
            public DateTime? LastActivity { get; set; }
            public DateTime Enrolled { get; set; }
        }

        public class DeadlineItem
        {
            public string Title { get; set; }
            public string Sub { get; set; }
            public string Badge { get; set; }
        }

        public class Achievement
        {
            public string Title { get; set; }
            public string Sub { get; set; }
            public string Tint { get; set; }
            public DateTime When { get; set; }
        }

        private long UserId { get { return Convert.ToInt64(Session["UserID"]); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }

            if (!IsPostBack) LoadPage();
        }

        private void LoadPage()
        {
            long uid = UserId;
            var p = new Dictionary<string, object> { { "@uid", uid } };
            DateTime nowLocal = DateTime.UtcNow.AddHours(UtcOffsetHours);
            DateTime today = nowLocal.Date;

            litGreeting.Text = GreetingText(nowLocal, uid);

            // ---------- enrolled courses with progress ----------
            DataTable ct = Db.Query(
                "SELECT c.course_id, c.title, c.course_img, COALESCE(u.full_name, 'TBA') AS tutor, e.enrolled_at, " +
                "  (SELECT COUNT(*) FROM lesson l WHERE l.course_id = c.course_id) AS total, " +
                "  (SELECT COUNT(*) FROM lesson_progress lp JOIN lesson l2 ON l2.lesson_id = lp.lesson_id " +
                "     WHERE l2.course_id = c.course_id AND lp.user_id = @uid AND lp.is_completed) AS done, " +
                "  (SELECT MAX(lp2.completed_at) FROM lesson_progress lp2 JOIN lesson l3 ON l3.lesson_id = lp2.lesson_id " +
                "     WHERE l3.course_id = c.course_id AND lp2.user_id = @uid AND lp2.is_completed) AS last_activity " +
                "FROM enrollment e JOIN courses c ON c.course_id = e.course_id " +
                "LEFT JOIN users u ON u.user_id = c.user_id " +
                "WHERE e.user_id = @uid AND c.status = 'draft'", p);

            var all = new List<CourseItem>();
            foreach (DataRow r in ct.Rows)
            {
                long total = Convert.ToInt64(r["total"]);
                long done = Convert.ToInt64(r["done"]);
                long id = Convert.ToInt64(r["course_id"]);

                all.Add(new CourseItem
                {
                    CourseId = id,
                    Title = Convert.ToString(r["title"]),
                    Tutor = Convert.ToString(r["tutor"]),
                    Percent = total > 0 ? (int)Math.Min(100, done * 100 / total) : 0,
                    ThumbStyle = ThumbStyle(r["course_img"]),
                    Url = ResolveUrl("~/pages/Student/learningProgression.aspx?course=" + id),
                    IsComplete = total > 0 && done >= total,
                    LastActivity = r["last_activity"] == DBNull.Value
                        ? (DateTime?)null
                        : DateTime.SpecifyKind(Convert.ToDateTime(r["last_activity"]), DateTimeKind.Utc),
                    Enrolled = DateTime.SpecifyKind(Convert.ToDateTime(r["enrolled_at"]), DateTimeKind.Utc)
                });
            }

            List<CourseItem> active = all
                .Where(c => !c.IsComplete)
                .OrderByDescending(c => c.LastActivity ?? DateTime.MinValue)
                .ThenByDescending(c => c.Enrolled)
                .ToList();
            int completedCount = all.Count(c => c.IsComplete);

            rptCourses.DataSource = active.Take(3).ToList();
            rptCourses.DataBind();

            if (active.Count == 0)
            {
                pnlNoCourses.Visible = true;
                litNoCourses.Text = all.Count == 0
                    ? "You have not enrolled in a course yet. Browse the catalogue to get started."
                    : "You have completed all of your courses. Great work!";
            }

            // ---------- lessons this week vs last week ----------
            DataTable wk = Db.Query(
                "SELECT " +
                " COUNT(*) FILTER (WHERE d >= (now() AT TIME ZONE '" + Tz + "')::date - 6) AS this_week, " +
                " COUNT(*) FILTER (WHERE d BETWEEN (now() AT TIME ZONE '" + Tz + "')::date - 13 " +
                "                            AND (now() AT TIME ZONE '" + Tz + "')::date - 7) AS last_week " +
                "FROM (SELECT (completed_at AT TIME ZONE '" + Tz + "')::date AS d FROM lesson_progress " +
                "      WHERE user_id = @uid AND is_completed AND completed_at IS NOT NULL) t", p);
            long thisWeek = Convert.ToInt64(wk.Rows[0]["this_week"]);
            long lastWeek = Convert.ToInt64(wk.Rows[0]["last_week"]);

            litWeek.Text = thisWeek + (thisWeek == 1 ? " lesson" : " lessons");
            long diff = thisWeek - lastWeek;
            litWeekNote.Text = (thisWeek == 0 && lastWeek == 0) ? "No lessons completed yet"
                : diff > 0 ? "+" + diff + " vs last week"
                : diff < 0 ? diff + " vs last week"
                : "Same as last week";

            // ---------- streak (lessons completed + quizzes taken) ----------
            DataTable act = Db.Query(
                "SELECT DISTINCT d FROM (" +
                "  SELECT (completed_at AT TIME ZONE '" + Tz + "')::date AS d FROM lesson_progress " +
                "    WHERE user_id = @uid AND is_completed AND completed_at IS NOT NULL " +
                "  UNION ALL " +
                "  SELECT (done_time AT TIME ZONE '" + Tz + "')::date FROM quiz_score " +
                "    WHERE user_id = @uid AND done_time IS NOT NULL) t ORDER BY d", p);

            var days = new HashSet<DateTime>();
            foreach (DataRow r in act.Rows) days.Add(Convert.ToDateTime(r["d"]).Date);

            DateTime cursor = days.Contains(today) ? today : today.AddDays(-1);
            int current = 0;
            while (days.Contains(cursor)) { current++; cursor = cursor.AddDays(-1); }

            int best = 0, run = 0;
            DateTime? prev = null;
            foreach (DateTime d in days.OrderBy(x => x))
            {
                run = (prev.HasValue && (d - prev.Value).Days == 1) ? run + 1 : 1;
                if (run > best) best = run;
                prev = d;
            }
            best = Math.Max(best, current);

            litStreak.Text = current + (current == 1 ? " Day" : " Days");
            litStreakNote.Text = best == 0 ? "Complete a lesson to start a streak"
                : "Personal best is " + best + (best == 1 ? " day" : " days");

            // ---------- active courses stat ----------
            litActive.Text = active.Count + (active.Count == 1 ? " Course" : " Courses");
            litActiveNote.Text = completedCount + " completed";

            // ---------- hero ----------
            if (all.Count == 0)
            {
                litHeroText.Text = "You have not enrolled in a course yet. Browse the catalogue and pick your first one.";
                lnkContinue.Text = "Browse Courses";
                lnkContinue.NavigateUrl = "~/pages/Student/myCourses.aspx";
            }
            else if (active.Count == 0)
            {
                litHeroText.Text = "You have completed all of your enrolled courses. Ready for the next one?";
                lnkContinue.Text = "Browse Courses";
                lnkContinue.NavigateUrl = "~/pages/Student/myCourses.aspx";
            }
            else
            {
                CourseItem resume = active[0];
                int top = active.Max(c => c.Percent);
                string lead = thisWeek > 0
                    ? "You completed " + thisWeek + (thisWeek == 1 ? " lesson" : " lessons") + " in the last 7 days. "
                    : "Pick up your next lesson today. ";

                litHeroText.Text = lead + "Your most advanced course is " + top + "% complete. " +
                                   "Continue where you left off on " + resume.Title + ".";
                lnkContinue.Text = "Continue Learning";
                lnkContinue.NavigateUrl = "~/pages/Student/learningProgression.aspx?course=" + resume.CourseId;
            }

            // ---------- upcoming deadlines (from the calendar) ----------
            var deadlines = new List<DeadlineItem>();
            try
            {
                DataTable dt = Db.Query(
                    "SELECT e.event_title, e.event_type, e.start_time, c.title AS course_title " +
                    "FROM calendar_events e LEFT JOIN courses c ON c.course_id = e.course_id " +
                    "WHERE e.user_id = @uid AND e.event_type IN ('Deadline', 'Exam') AND e.start_time >= now() " +
                    "ORDER BY e.start_time LIMIT 3", p);

                foreach (DataRow r in dt.Rows)
                {
                    string course = Convert.ToString(r["course_title"]);
                    deadlines.Add(new DeadlineItem
                    {
                        Title = Convert.ToString(r["event_title"]),
                        Sub = string.IsNullOrWhiteSpace(course) ? Convert.ToString(r["event_type"]) : course,
                        Badge = ToLocal(r["start_time"]).ToString("MMM d", Inv)
                    });
                }
            }
            catch (Exception) { /* no calendar table or column yet: show the empty message */ }

            rptDeadlines.DataSource = deadlines;
            rptDeadlines.DataBind();
            pnlNoDeadlines.Visible = deadlines.Count == 0;

            // ---------- recent achievements ----------
            var items = new List<Achievement>();

            foreach (CourseItem c in all.Where(x => x.IsComplete && x.LastActivity.HasValue))
            {
                DateTime when = c.LastActivity.Value.AddHours(UtcOffsetHours);
                items.Add(new Achievement
                {
                    Title = "Completed: " + c.Title,
                    Sub = when.ToString("d MMM yyyy", Inv),
                    When = when
                });
            }

            DataTable perfect = Db.Query(
                "SELECT q.quiz_title, " +
                "  (SELECT c.title FROM lesson l JOIN courses c ON c.course_id = l.course_id " +
                "     WHERE l.lesson_id = q.lesson_id OR l.quiz_id = q.quiz_id LIMIT 1) AS course_title, " +
                "  MIN(qs.done_time) AS happened " +
                "FROM quiz_score qs JOIN quiz q ON q.quiz_id = qs.quiz_id " +
                "WHERE qs.user_id = @uid AND qs.total_score >= 100 " +
                "GROUP BY q.quiz_id, q.quiz_title, q.lesson_id", p);
            foreach (DataRow r in perfect.Rows)
            {
                DateTime when = ToLocal(r["happened"]);
                string course = Convert.ToString(r["course_title"]);
                items.Add(new Achievement
                {
                    Title = "Perfect Score: " + Convert.ToString(r["quiz_title"]),
                    Sub = (string.IsNullOrWhiteSpace(course) ? "" : course + " \u00B7 ") + when.ToString("d MMM yyyy", Inv),
                    When = when
                });
            }

            DataTable certs = Db.Query(
                "SELECT c.title, ce.issued_date FROM certificates ce " +
                "JOIN courses c ON c.course_id = ce.course_id WHERE ce.user_id = @uid", p);
            foreach (DataRow r in certs.Rows)
            {
                DateTime when = ToLocal(r["issued_date"]);
                items.Add(new Achievement
                {
                    Title = "Certificate Earned",
                    Sub = Convert.ToString(r["title"]) + " \u00B7 " + when.ToString("d MMM yyyy", Inv),
                    When = when
                });
            }

            string[] tints = { "#fef1e4", "#eef6ff", "#eefaf1" };
            List<Achievement> latest = items.OrderByDescending(a => a.When).Take(3).ToList();
            for (int i = 0; i < latest.Count; i++) latest[i].Tint = tints[i % tints.Length];

            rptAch.DataSource = latest;
            rptAch.DataBind();
            pnlNoAch.Visible = latest.Count == 0;
        }

        // ---------- helpers ----------
        private string GreetingText(DateTime nowLocal, long uid)
        {
            string full = Session["UserFullName"] as string;
            if (string.IsNullOrWhiteSpace(full))
            {
                DataTable t = Db.Query("SELECT full_name FROM users WHERE user_id = @uid",
                    new Dictionary<string, object> { { "@uid", uid } });
                if (t.Rows.Count > 0)
                {
                    full = Convert.ToString(t.Rows[0]["full_name"]);
                    Session["UserFullName"] = full;
                }
            }

            string first = string.IsNullOrWhiteSpace(full)
                ? "there"
                : full.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];

            string part = nowLocal.Hour < 12 ? "morning" : nowLocal.Hour < 18 ? "afternoon" : "evening";
            return "Good " + part + ", " + first + "!";
        }

        private static DateTime ToLocal(object utcValue)
        {
            if (utcValue == null || utcValue == DBNull.Value) return DateTime.MinValue;
            return DateTime.SpecifyKind(Convert.ToDateTime(utcValue), DateTimeKind.Utc).AddHours(UtcOffsetHours);
        }

        private string ThumbStyle(object img)
        {
            string url = Convert.ToString(img);
            if (string.IsNullOrWhiteSpace(url))
                return "background:linear-gradient(135deg,#eef3fb,#e6f0ff)";

            url = url.Trim();
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = ResolveUrl("~/" + url.TrimStart('/', '~'));

            url = url.Replace("'", "%27").Replace("(", "%28").Replace(")", "%29");
            return "background:url('" + HttpUtility.HtmlAttributeEncode(url) + "') center/cover";
        }
    }
}