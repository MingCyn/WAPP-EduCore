using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Web.UI;
using Npgsql;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class tutorDashboard : Page
    {
        // ------------------------------------------------------------------
        // Settings
        // ------------------------------------------------------------------
        private const string ConnectionName = "SupabasePostgres";
        private const string SessionUserKey = "UserID";
        private const string LoginPage = "../../auth/login.aspx";

        // While developing, the real database message is shown if a figure cannot be loaded.
        // Set to false before you deploy.
        private const bool ShowDatabaseErrors = true;

        // quiz_score.total_score (used for Average Score and Recent Quiz Results only):
        //   true  = already a percentage (0-100)
        //   false = number of correct answers (divided by the quiz's number of questions)
        private const bool ScoreIsPercentage = true;

        // The completion target shown under "Completion Rate" (a goal, not stored data).
        private const int CompletionTarget = 70;

        // Student status (Active Student Progress table) looks only at the student's latest quiz score
        // in that course, compared with that quiz's passing_score:
        //   score below passing_score                          -> NEEDS ATTENTION
        //   score passes by less than this many points         -> AT RISK
        //   anything else (or no quiz taken yet)               -> ON TRACK
        private const int AtRiskMarginPoints = 10;

        // ------------------------------------------------------------------
        // Values shown in the stat cards (set in LoadDashboard)
        // ------------------------------------------------------------------
        protected string TotalStudentsText = "-", StudentTrendText = "", StudentTrendClass = "neutral";
        protected string ActiveCoursesText = "-", ActiveCoursesNote = "";
        protected string AverageScoreText = "-", AverageScoreNote = "";
        protected string CompletionText = "-", CompletionNote = "", CompletionClass = "neutral";

        private int userId;

        private long totalStudents, newStudents;
        private long publishedCourses, draftCourses;
        private long lessonCount, completedLessonPairs, attemptCount;
        private double? averageScore;

        // ------------------------------------------------------------------
        // SQL
        //
        // PROGRESS comes from the lesson_progress table:
        //   progress = lessons a student has completed (is_completed = true) / lessons in the course
        // "lscope" = the lessons that belong to THIS tutor's PUBLISHED courses.
        //
        // SCORES (Average Score, Recent Quiz Results) still come from quiz_score:
        // "scope" = the quizzes that belong to this tutor's published courses (quiz -> lesson -> course).
        // ------------------------------------------------------------------
        private const string LessonScopeCte =
            "WITH lscope AS (" +
            "  SELECT l.lesson_id, c.course_id " +
            "  FROM public.lesson l " +
            "  JOIN courses c ON c.course_id = l.course_id " +
            "  WHERE c.user_id = @uid AND LOWER(c.status) = 'published') ";

        private const string ScopeCte =
            "WITH scope AS (" +
            "  SELECT q.quiz_id, c.course_id " +
            "  FROM quiz q " +
            "  JOIN public.lesson l ON l.lesson_id = q.lesson_id " +
            "  JOIN courses c ON c.course_id = l.course_id " +
            "  WHERE c.user_id = @uid AND LOWER(c.status) = 'published') ";

        // Score of one attempt as a percentage
        private const string PercentSql = ScoreIsPercentage
            ? "CAST(qs.total_score AS double precision)"
            : "qs.total_score * 100.0 / NULLIF(z.n, 0)";

        // Number of questions per quiz (only needed when total_score is a raw count)
        private const string SizesCte = ", sizes AS (SELECT quiz_id, COUNT(*) AS n FROM quiz_questions GROUP BY quiz_id) ";

        // Total students = every account with role = student; "new" = joined in the last 7 days
        private const string SqlStudents =
            "SELECT COUNT(*) FILTER (WHERE LOWER(role) = 'student'), " +
            "       COUNT(*) FILTER (WHERE LOWER(role) = 'student' AND created_at >= NOW() - INTERVAL '7 days') " +
            "FROM public.users";

        // Active courses = this tutor's published courses; the rest are drafts
        private const string SqlCourseCounts =
            "SELECT COUNT(*) FILTER (WHERE LOWER(COALESCE(status, 'draft')) = 'published'), " +
            "       COUNT(*) FILTER (WHERE LOWER(COALESCE(status, 'draft')) <> 'published') " +
            "FROM courses WHERE user_id = @uid";

        // Lessons in this tutor's published courses
        private const string SqlLessonCount = LessonScopeCte + "SELECT COUNT(*) FROM lscope";

        // Completed = each student/lesson pair counted once (is_completed = true in lesson_progress)
        private const string SqlCompletedLessonPairs =
            LessonScopeCte +
            "SELECT COUNT(*) FROM (" +
            "  SELECT DISTINCT lp.user_id, lp.lesson_id " +
            "  FROM public.lesson_progress lp " +
            "  JOIN lscope s ON s.lesson_id = lp.lesson_id " +
            "  JOIN public.users u ON u.user_id = lp.user_id AND LOWER(u.role) = 'student' " +
            "  WHERE lp.is_completed) d";

        // Average score = average of each student's own average (so one very active student can't dominate)
        private const string SqlAverageScore =
            ScopeCte + SizesCte +
            "SELECT AVG(t.student_avg), COALESCE(SUM(t.attempts), 0) FROM (" +
            "  SELECT x.user_id, AVG(x.pct) AS student_avg, COUNT(*) AS attempts FROM (" +
            "    SELECT qs.user_id, " + PercentSql + " AS pct " +
            "    FROM quiz_score qs " +
            "    JOIN scope s ON s.quiz_id = qs.quiz_id " +
            "    JOIN public.users u ON u.user_id = qs.user_id AND LOWER(u.role) = 'student' " +
            "    LEFT JOIN sizes z ON z.quiz_id = qs.quiz_id " +
            "    WHERE qs.done_time IS NOT NULL) x " +
            "  WHERE x.pct IS NOT NULL GROUP BY x.user_id) t";

        // Three most recently updated published courses, with their lesson progress
        private const string SqlCourseCards =
            "SELECT c.course_id, c.title, " +
            "  (SELECT COUNT(*) FROM public.lesson l WHERE l.course_id = c.course_id) AS lesson_count, " +
            "  (SELECT COUNT(DISTINCT lp.user_id) FROM public.lesson_progress lp " +
            "    JOIN public.lesson l ON l.lesson_id = lp.lesson_id " +
            "    JOIN public.users u ON u.user_id = lp.user_id AND LOWER(u.role) = 'student' " +
            "    WHERE l.course_id = c.course_id) AS students, " +
            "  (SELECT COUNT(*) FROM (SELECT DISTINCT lp.user_id, lp.lesson_id FROM public.lesson_progress lp " +
            "    JOIN public.lesson l ON l.lesson_id = lp.lesson_id " +
            "    JOIN public.users u ON u.user_id = lp.user_id AND LOWER(u.role) = 'student' " +
            "    WHERE l.course_id = c.course_id AND lp.is_completed) d) AS completed " +
            "FROM courses c " +
            "WHERE c.user_id = @uid AND LOWER(c.status) = 'published' " +
            "ORDER BY c.updated_at DESC NULLS LAST, c.course_id DESC LIMIT 3";

        // Five students with the most recently completed lessons (one row per student and course).
        //   progress = lessons completed (lesson_progress)
        //   status   = the student's latest quiz score in that course vs that quiz's passing_score (quiz table)
        private const string SqlStudentRows =
            LessonScopeCte +
            ", totals AS (SELECT course_id, COUNT(*) AS lesson_count FROM lscope GROUP BY course_id), " +
            "activity AS (" +
            "  SELECT lp.user_id, s.course_id, " +
            "         COUNT(DISTINCT lp.lesson_id) FILTER (WHERE lp.is_completed) AS done_lessons, " +
            "         MAX(lp.completed_at) FILTER (WHERE lp.is_completed) AS last_done " +
            "  FROM public.lesson_progress lp JOIN lscope s ON s.lesson_id = lp.lesson_id " +
            "  GROUP BY lp.user_id, s.course_id), " +
            "sizes AS (SELECT quiz_id, COUNT(*) AS n FROM quiz_questions GROUP BY quiz_id), " +
            "latest AS (" +
            "  SELECT DISTINCT ON (qs.user_id, s.course_id) qs.user_id, s.course_id, " +
            "         " + PercentSql + " AS pct, q.passing_score " +
            "  FROM quiz_score qs " +
            "  JOIN quiz q ON q.quiz_id = qs.quiz_id " +
            "  JOIN lscope s ON s.lesson_id = q.lesson_id " +
            "  LEFT JOIN sizes z ON z.quiz_id = qs.quiz_id " +
            "  WHERE qs.done_time IS NOT NULL " +
            "  ORDER BY qs.user_id, s.course_id, qs.done_time DESC) " +
            "SELECT u.full_name, c.title, a.done_lessons, t.lesson_count, lt.pct, lt.passing_score " +
            "FROM activity a " +
            "JOIN public.users u ON u.user_id = a.user_id AND LOWER(u.role) = 'student' " +
            "JOIN courses c ON c.course_id = a.course_id " +
            "JOIN totals t ON t.course_id = a.course_id " +
            "LEFT JOIN latest lt ON lt.user_id = a.user_id AND lt.course_id = a.course_id " +
            "ORDER BY a.last_done DESC NULLS LAST, u.full_name LIMIT 5";

        // Latest quiz attempts
        private const string SqlRecent =
            ScopeCte + SizesCte +
            "SELECT u.full_name, q.quiz_title, qs.done_time, qs.total_score, z.n, q.passing_score " +
            "FROM quiz_score qs " +
            "JOIN scope s ON s.quiz_id = qs.quiz_id " +
            "JOIN quiz q ON q.quiz_id = qs.quiz_id " +
            "JOIN public.users u ON u.user_id = qs.user_id AND LOWER(u.role) = 'student' " +
            "LEFT JOIN sizes z ON z.quiz_id = qs.quiz_id " +
            "WHERE qs.done_time IS NOT NULL " +
            "ORDER BY qs.done_time DESC LIMIT 4";

        // ------------------------------------------------------------------
        // Row shapes for the repeaters
        // ------------------------------------------------------------------
        public class CourseCard
        {
            public int CourseId { get; set; }
            public string Title { get; set; }
            public string StudentsText { get; set; }
            public int Progress { get; set; }
        }

        public class StudentRow
        {
            public string Name { get; set; }
            public string CourseTitle { get; set; }
            public int Progress { get; set; }
            public string StatusLabel { get; set; }
            public string StatusClass { get; set; }
        }

        public class RecentRow
        {
            public string Student { get; set; }
            public string QuizTitle { get; set; }
            public string When { get; set; }
            public string ScoreText { get; set; }
            public string ScoreClass { get; set; }
        }

        // ------------------------------------------------------------------
        // Page load
        // ------------------------------------------------------------------
        protected void Page_Load(object sender, EventArgs e)
        {
            // Ensure only signed-in tutors can access the dashboard
            if (Session["UserRole"] == null || !Session["UserRole"].ToString().Equals("Tutor", StringComparison.OrdinalIgnoreCase)
                || !int.TryParse(Convert.ToString(Session[SessionUserKey]), out userId) || userId <= 0)
            {
                Response.Redirect(LoginPage, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            // Render the shared navigation (tutorNav.aspx) into the panel container
            try
            {
                using (var sw = new StringWriter())
                {
                    Server.Execute("~/pages/Tutor/tutorNav.aspx", sw, false);
                    pnlNavContainer.Controls.Add(new LiteralControl(sw.ToString()));
                }
            }
            catch (Exception)
            {
                // Fail gracefully if the shared nav cannot be rendered
                pnlNavContainer.Controls.Add(new LiteralControl("<!-- tutorNav rendering failed -->"));
            }

            LoadDashboard();
        }

        // ------------------------------------------------------------------
        // Loads every figure. A failing query never breaks the whole page: the rest still shows,
        // and a short message lists what could not be loaded.
        // ------------------------------------------------------------------
        private void LoadDashboard()
        {
            var problems = new List<string>();
            var courseCards = new List<CourseCard>();
            var studentRows = new List<StudentRow>();
            var recentRows = new List<RecentRow>();

            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString))
                {
                    conn.Open();

                    Run(problems, "students", () => LoadStudentCounts(conn));
                    Run(problems, "courses", () => LoadCourseCounts(conn));
                    Run(problems, "lessons", () => lessonCount = ScalarLong(conn, SqlLessonCount));
                    Run(problems, "completion", () => completedLessonPairs = ScalarLong(conn, SqlCompletedLessonPairs));
                    Run(problems, "average score", () => LoadAverageScore(conn));
                    Run(problems, "course overview", () => LoadCourseCards(conn, courseCards));
                    Run(problems, "student progress", () => LoadStudentRows(conn, studentRows));
                    Run(problems, "recent results", () => LoadRecent(conn, recentRows));
                }
            }
            catch (Exception ex)
            {
                // Could not connect at all (or the connection string name is wrong)
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                problems.Add("database connection" + (ShowDatabaseErrors ? " (" + ex.Message + ")" : ""));
            }

            BuildStatTexts();

            RptCourses.DataSource = courseCards;
            RptCourses.DataBind();
            PnlNoCourses.Visible = courseCards.Count == 0;

            RptStudents.DataSource = studentRows;
            RptStudents.DataBind();
            PnlStudentsTable.Visible = studentRows.Count > 0;
            PnlNoStudents.Visible = studentRows.Count == 0;

            RptRecent.DataSource = recentRows;
            RptRecent.DataBind();
            PnlNoRecent.Visible = recentRows.Count == 0;

            if (problems.Count > 0)
            {
                LblStatus.Text = Server.HtmlEncode("Some figures could not be loaded: " + string.Join("; ", problems.ToArray()) + ".");
                LblStatus.Visible = true;
            }
        }

        private static void Run(List<string> problems, string label, Action action)
        {
            try
            {
                action();
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(label + ": " + ex);
                var pg = ex as PostgresException;
                problems.Add(label + (ShowDatabaseErrors ? " (" + (pg != null ? pg.MessageText : ex.Message) + ")" : ""));
            }
        }

        // ------------------------------------------------------------------
        // Queries
        // ------------------------------------------------------------------
        private NpgsqlCommand CreateCommand(NpgsqlConnection conn, string sql)
        {
            var cmd = new NpgsqlCommand(sql, conn);
            if (sql.IndexOf("@uid", StringComparison.Ordinal) >= 0) cmd.Parameters.AddWithValue("uid", userId);
            return cmd;
        }

        private long ScalarLong(NpgsqlConnection conn, string sql)
        {
            using (NpgsqlCommand cmd = CreateCommand(conn, sql))
            {
                object value = cmd.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt64(value);
            }
        }

        private void LoadStudentCounts(NpgsqlConnection conn)
        {
            using (NpgsqlCommand cmd = CreateCommand(conn, SqlStudents))
            using (NpgsqlDataReader r = cmd.ExecuteReader())
            {
                if (!r.Read()) return;
                totalStudents = Convert.ToInt64(r[0]);
                newStudents = Convert.ToInt64(r[1]);
            }
        }

        private void LoadCourseCounts(NpgsqlConnection conn)
        {
            using (NpgsqlCommand cmd = CreateCommand(conn, SqlCourseCounts))
            using (NpgsqlDataReader r = cmd.ExecuteReader())
            {
                if (!r.Read()) return;
                publishedCourses = Convert.ToInt64(r[0]);
                draftCourses = Convert.ToInt64(r[1]);
            }
        }

        private void LoadAverageScore(NpgsqlConnection conn)
        {
            using (NpgsqlCommand cmd = CreateCommand(conn, SqlAverageScore))
            using (NpgsqlDataReader r = cmd.ExecuteReader())
            {
                if (!r.Read()) return;
                averageScore = r.IsDBNull(0) ? (double?)null : Convert.ToDouble(r[0]);
                attemptCount = r.IsDBNull(1) ? 0 : Convert.ToInt64(r[1]);
            }
        }

        private void LoadCourseCards(NpgsqlConnection conn, List<CourseCard> list)
        {
            using (NpgsqlCommand cmd = CreateCommand(conn, SqlCourseCards))
            using (NpgsqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    long lessons = Convert.ToInt64(r["lesson_count"]);
                    long students = Convert.ToInt64(r["students"]);
                    long completed = Convert.ToInt64(r["completed"]);

                    // Progress among the students who have started this course:
                    // completed (student, lesson) pairs / (lessons x students)
                    double progress = (lessons > 0 && students > 0) ? completed * 100.0 / (lessons * students) : 0;

                    list.Add(new CourseCard
                    {
                        CourseId = Convert.ToInt32(r["course_id"]),
                        Title = r.IsDBNull(1) ? "(Untitled course)" : Convert.ToString(r["title"]),
                        StudentsText = students == 1 ? "1 student" : students + " students",
                        Progress = ClampPercent(progress)
                    });
                }
            }
        }

        private void LoadStudentRows(NpgsqlConnection conn, List<StudentRow> list)
        {
            using (NpgsqlCommand cmd = CreateCommand(conn, SqlStudentRows))
            using (NpgsqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    long done = Convert.ToInt64(r["done_lessons"]);
                    long total = Convert.ToInt64(r["lesson_count"]);
                    int progress = ClampPercent(total > 0 ? done * 100.0 / total : 0);

                    // Latest quiz score of this student in this course and that quiz's passing score
                    // (both empty when the student has not taken a quiz in this course yet)
                    double? score = r.IsDBNull(r.GetOrdinal("pct")) ? (double?)null : Convert.ToDouble(r["pct"]);
                    double? passing = r.IsDBNull(r.GetOrdinal("passing_score")) ? (double?)null : Convert.ToDouble(r["passing_score"]);

                    string label, css;
                    MapStatus(score, passing, out label, out css);

                    list.Add(new StudentRow
                    {
                        Name = r.IsDBNull(0) ? "(Unnamed student)" : Convert.ToString(r[0]),
                        CourseTitle = r.IsDBNull(1) ? "" : Convert.ToString(r[1]),
                        Progress = progress,
                        StatusLabel = label,
                        StatusClass = css
                    });
                }
            }
        }

        private void LoadRecent(NpgsqlConnection conn, List<RecentRow> list)
        {
            using (NpgsqlCommand cmd = CreateCommand(conn, SqlRecent))
            using (NpgsqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    double? pct = ToPercent(r["total_score"], r["n"]);
                    bool hasPass = !r.IsDBNull(r.GetOrdinal("passing_score"));
                    bool passed = pct.HasValue && hasPass && pct.Value >= Convert.ToDouble(r["passing_score"]);

                    list.Add(new RecentRow
                    {
                        Student = r.IsDBNull(0) ? "(Unnamed student)" : Convert.ToString(r[0]),
                        QuizTitle = r.IsDBNull(1) ? "Quiz" : Convert.ToString(r[1]),
                        When = RelativeTime(ToUtc(r.GetDateTime(r.GetOrdinal("done_time")))),
                        ScoreText = pct.HasValue ? ClampPercent(pct.Value) + "%" : "-",
                        ScoreClass = !pct.HasValue || !hasPass ? "status-attention" : (passed ? "status-on-track" : "status-risk")
                    });
                }
            }
        }

        // ------------------------------------------------------------------
        // Text for the four stat cards
        // ------------------------------------------------------------------
        private void BuildStatTexts()
        {
            // Total students
            TotalStudentsText = totalStudents.ToString("N0", CultureInfo.InvariantCulture);
            StudentTrendText = newStudents > 0 ? "+" + newStudents + " this week" : "No new students this week";
            StudentTrendClass = newStudents > 0 ? "positive" : "neutral";

            // Active courses (published) and how many are still drafts
            ActiveCoursesText = publishedCourses.ToString(CultureInfo.InvariantCulture);
            ActiveCoursesNote = draftCourses == 0 ? "No draft courses"
                              : draftCourses == 1 ? "1 draft course" : draftCourses + " draft courses";

            // Average score (average of each student's own average)
            if (averageScore.HasValue)
            {
                AverageScoreText = Math.Round(averageScore.Value, 1).ToString("0.#", CultureInfo.InvariantCulture) + "%";
                AverageScoreNote = "From " + attemptCount + (attemptCount == 1 ? " quiz attempt" : " quiz attempts");
            }
            else
            {
                AverageScoreText = "-";
                AverageScoreNote = "No quiz results yet";
            }

            // Completion rate = completed (student, lesson) pairs / (students x lessons), from lesson_progress
            long expected = totalStudents * lessonCount;
            if (expected > 0)
            {
                int pct = ClampPercent(completedLessonPairs * 100.0 / expected);
                CompletionText = pct + "%";
                CompletionNote = "Target: " + CompletionTarget + "%";
                CompletionClass = pct >= CompletionTarget ? "positive" : "neutral";
            }
            else
            {
                CompletionText = "-";
                CompletionNote = lessonCount == 0 ? "No lessons in published courses" : "No students yet";
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private static double? ToPercent(object totalScore, object questionCount)
        {
            if (totalScore == null || totalScore == DBNull.Value) return null;
            double total = Convert.ToDouble(totalScore);

            bool isPercent = ScoreIsPercentage;      // a local keeps the compiler from flagging the other branch
            if (isPercent) return total;

            if (questionCount == null || questionCount == DBNull.Value) return null;
            double n = Convert.ToDouble(questionCount);
            return n > 0 ? total * 100.0 / n : (double?)null;
        }

        private static int ClampPercent(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return 0;
            return (int)Math.Max(0, Math.Min(100, Math.Round(value)));
        }

        // score = the student's latest quiz score (%), passing = that quiz's passing_score (%).
        // Either one is null when the student has not taken a quiz in the course yet.
        private static void MapStatus(double? score, double? passing, out string label, out string css)
        {
            if (score.HasValue && passing.HasValue && score.Value < passing.Value)
            {
                // below the passing score
                label = "NEEDS ATTENTION"; css = "status-attention";
            }
            else if (score.HasValue && passing.HasValue && score.Value < passing.Value + AtRiskMarginPoints)
            {
                // passed, but by less than AtRiskMarginPoints
                label = "AT RISK"; css = "status-risk";
            }
            else
            {
                label = "ON TRACK"; css = "status-on-track";
            }
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

        private static DateTime ToUtc(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Local) return dt.ToUniversalTime();
            if (dt.Kind == DateTimeKind.Unspecified) return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return dt;
        }
    }
}