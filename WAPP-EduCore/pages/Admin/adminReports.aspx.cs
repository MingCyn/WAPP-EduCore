using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Npgsql;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    public partial class adminReports : Page
    {
        // =====================================================
        //  STEP 1: When the page opens, fill every tab with data
        // =====================================================
        // These two are shown in the sidebar and the top-right circle.
        // The .aspx page reads them with <%= AdminName %> and <%= AdminInitials %>
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        // Numbers are always written the same way (1234.5, never 1234,5)
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // We remember the "who was active?" SQL, so we only build it once per page load
        private string recentActivitySql = null;

        // The id of the admin who is looking at this page right now
        private int adminId = 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Not logged in, or the session ran out (for example after resting for a while)?
            // Then go to the login page. We do NOT guess an admin any more.
            int signedInAdmin = CurrentUserId();
            if (signedInAdmin == 0)
            {
                Response.Redirect("~/Default.aspx");     // the first page of the website (it has the Log In button)
                return;
            }

            // Remember this admin in the browser until he/she clicks Logout
            WAPP_EduCore.Data.LoginKeeper.Remember(Context, signedInAdmin);

            LoadAdminInfo();   // runs every time the page opens

            if (!IsPostBack)
            {
                LoadUsersTab();
                LoadCoursesTab();
                LoadLearningTab();

                ShowTab("users");   // start on the first tab
            }
        }

        // =====================================================
        //  Get the admin's name from the database
        //  - If the login page saved Session["user_id"], use that user.
        //  - If not, use the first user whose role is 'Admin'.
        // =====================================================
        private void LoadAdminInfo()
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;

                if (CurrentUserId() > 0)
                {
                    cmd.CommandText = "SELECT full_name, user_id FROM public.users WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("@id", CurrentUserId());
                }
                else
                {
                    cmd.CommandText = "SELECT full_name, user_id FROM public.users WHERE role = 'Admin' ORDER BY user_id LIMIT 1";
                }

                string name = null;
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        name = reader[0].ToString();
                        adminId = Convert.ToInt32(reader[1]);
                    }
                }

                if (name != null)
                {
                    string fullName = name.Trim();
                    AdminName = Server.HtmlEncode(fullName);

                    // Initials = first letter of the name (big letter, like Z for zhilin)
                    string[] words = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 1)
                        AdminInitials = words[0].Substring(0, 1).ToUpper();
                    else if (words.Length > 1)
                        AdminInitials = words[0].Substring(0, 1).ToUpper();
                }
            }
        }

        // =====================================================
        //  LOGOUT  (the "Logout" option in the drop-down menu)
        // =====================================================
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            WAPP_EduCore.Data.LoginKeeper.Forget(Context);   // remove the "remember me" cookie
            Session.Clear();        // forget who was logged in
            Session.Abandon();      // end the session
            Response.Redirect("~/auth/login.aspx");   // go to the login page
        }

        // =====================================================
        //  STEP 2: Clicking a tab shows ONE panel, hides the others
        // =====================================================
        protected void Tab_Click(object sender, EventArgs e)
        {
            LinkButton clicked = (LinkButton)sender;
            ShowTab(clicked.CommandArgument);   // "users", "courses" or "learning"
        }

        private void ShowTab(string tab)
        {
            pnlUsers.Visible = (tab == "users");
            pnlCourses.Visible = (tab == "courses");
            pnlLearning.Visible = (tab == "learning");

            // the active tab gets the blue underline
            lnkUsers.CssClass = (tab == "users") ? "active" : "";
            lnkCourses.CssClass = (tab == "courses") ? "active" : "";
            lnkLearning.CssClass = (tab == "learning") ? "active" : "";
        }

        // =====================================================
        //  TAB 1 : USERS  (users table + the "who was active" tables)
        // =====================================================
        private void LoadUsersTab()
        {
            int total = GetNumber("SELECT COUNT(*) FROM public.users");
            int students = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Student'");
            int tutors = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Tutor'");
            int admins = total - students - tutors;   // everyone else

            // ----- New registrations: this month so far, and the same days last month -----
            int newThisMonth = GetNumber(
                "SELECT COUNT(*) FROM public.users WHERE created_at >= date_trunc('month', now())");
            int newLastMonth = GetNumber(
                @"SELECT COUNT(*) FROM public.users
                  WHERE created_at >= date_trunc('month', now()) - interval '1 month'
                    AND created_at <  now() - interval '1 month'");

            // ----- Active users = did something in the last 30 days -----
            string recent = RecentActivitySql();
            int active = GetNumber("SELECT COUNT(*) FROM public.users WHERE user_id IN (" + recent + ")");
            int activeStudents = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Student' AND user_id IN (" + recent + ")");
            int activeTutors = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Tutor' AND user_id IN (" + recent + ")");
            int activeAdmins = active - activeStudents - activeTutors;
            int inactive = total - active;

            // ----- The 4 number boxes -----
            lblTotalUsers.Text = total.ToString("N0");

            lblActiveUsers.Text = active.ToString("N0");
            lblActiveNote.Text = Percent(active, total) + " of accounts";

            lblNewUsers.Text = newThisMonth.ToString("N0");
            lblNewNote.Text = "1–" + DateTime.Now.Day + " " + DateTime.Now.ToString("MMM yyyy", Inv) + " · " + GrowthText(newThisMonth, newLastMonth);

            lblInactiveUsers.Text = inactive.ToString("N0");
            lblInactiveNote.Text = Percent(inactive, total) + " of all accounts";

            // ----- Line chart: new accounts per month (the last 6 months, even empty ones) -----
            DataTable months = GetTable(
                @"SELECT to_char(m.month_start, 'Mon') AS month_name,
                         to_char(m.month_start, 'FMMonth') AS month_full,
                         COUNT(u.user_id) AS total
                  FROM generate_series(date_trunc('month', now()) - interval '5 months',
                                       date_trunc('month', now()),
                                       interval '1 month') AS m(month_start)
                  LEFT JOIN public.users u ON date_trunc('month', u.created_at) = m.month_start
                  GROUP BY m.month_start
                  ORDER BY m.month_start");
            litLineChart.Text = BuildLineChart(months);

            lblRegSub.Text = "Monthly new accounts · " + DateTime.Now.ToString("MMMM", Inv) + " through " +
                             DateTime.Now.Day + " " + DateTime.Now.ToString("MMM", Inv);

            if (months.Rows.Count >= 2)
            {
                DataRow thisMonth = months.Rows[months.Rows.Count - 1];
                DataRow lastMonth = months.Rows[months.Rows.Count - 2];
                litRegNote.Text = "<div class='note'>" + thisMonth["total"] + " registrations in " + thisMonth["month_full"] +
                                  "; " + lastMonth["total"] + " in " + lastMonth["month_full"] + ".</div>";
            }

            // ----- Bars: users by role (the numbers under the bars go 0 ... biggest) -----
            lblRoleSub.Text = total.ToString("N0");

            int roleMax = NiceCeiling(Math.Max(students, Math.Max(tutors, admins)));

            StringBuilder bars = new StringBuilder();
            bars.Append(BarRow("Student", students, roleMax, students.ToString("N0"), false));
            bars.Append(BarRow("Tutor", tutors, roleMax, tutors.ToString("N0"), false));
            bars.Append(BarRow("Admin", admins, roleMax, admins.ToString("N0"), false));
            bars.Append(BarAxis(roleMax));
            bars.Append("<div class='note'>Students represent " + Percent(students, total) + " of the platform population.</div>");
            litRoleBars.Text = bars.ToString();

            // ----- Role table -----
            StringBuilder rows = new StringBuilder();
            rows.Append(RoleTableRow("Student", students, total, activeStudents));
            rows.Append(RoleTableRow("Tutor", tutors, total, activeTutors));
            rows.Append(RoleTableRow("Admin", admins, total, activeAdmins));
            rows.Append("<tr class='total'><td>All roles</td><td>" + total.ToString("N0") + "</td><td>100%</td><td>" +
                        active.ToString("N0") + "</td><td>" + inactive.ToString("N0") + "</td><td>" + Percent(active, total) + "</td></tr>");
            litRoleTable.Text = rows.ToString();
        }

        private string RoleTableRow(string role, int count, int total, int active)
        {
            return "<tr><td>" + role + "</td><td>" + count.ToString("N0") + "</td><td>" + Percent(count, total) +
                   "</td><td>" + active.ToString("N0") + "</td><td>" + (count - active).ToString("N0") +
                   "</td><td>" + Percent(active, count) + "</td></tr>";
        }

        // "+4.8% growth" (this month so far compared with the same days last month)
        private string GrowthText(int thisMonth, int lastMonth)
        {
            if (lastMonth == 0)
                return (thisMonth == 0) ? "no change" : "new this month";

            double growth = (thisMonth - lastMonth) * 100.0 / lastMonth;
            return growth.ToString("+0.0;-0.0;0.0", Inv) + "% growth";
        }

        // =====================================================
        //  TAB 2 : COURSES  (courses + lesson + enrollment + users tables)
        // =====================================================
        private void LoadCoursesTab()
        {
            // ----- The 4 number boxes -----
            int total = GetNumber("SELECT COUNT(*) FROM public.courses");
            int topics = GetNumber("SELECT COUNT(DISTINCT category) FROM public.courses");

            // A course belongs to a subject when the subject word is in its title or its topic
            int biology = CountSubject("biology");
            int chemistry = CountSubject("chemistry");
            int physics = CountSubject("physics");

            // (still needed for the ranking note at the bottom)
            int published = GetNumber("SELECT COUNT(*) FROM public.courses WHERE status::text ILIKE 'publish%'");

            lblTotalCourses.Text = total.ToString("N0");
            lblTotalNote.Text = "Across " + topics + " topics";

            lblBiology.Text = biology.ToString("N0");
            lblBiologyNote.Text = Percent(biology, total) + " of catalogue";

            lblChemistry.Text = chemistry.ToString("N0");
            lblChemistryNote.Text = Percent(chemistry, total) + " of catalogue";

            lblPhysics.Text = physics.ToString("N0");
            lblPhysicsNote.Text = Percent(physics, total) + " of catalogue";

            // ----- Column chart: how many LESSONS does each subject have? -----
            int bioLessons = CountSubjectLessons("biology");
            int chemLessons = CountSubjectLessons("chemistry");
            int physLessons = CountSubjectLessons("physics");

            List<string> subjectNames = new List<string> { "Biology", "Chemistry", "Physics" };
            List<int> subjectValues = new List<int> { bioLessons, chemLessons, physLessons };
            List<string> subjectColors = new List<string> { "#4F67CC", "#8E9EE0", "#CBD2F0" };

            int allLessons = GetNumber("SELECT COUNT(*) FROM public.lesson");
            litSubjectLessons.Text = DrawColumns(subjectNames, subjectValues, subjectColors) +
                                     "<div class='note'>" + (bioLessons + chemLessons + physLessons).ToString("N0") + " of " +
                                     allLessons.ToString("N0") + " lessons belong to these three subjects.</div>";

            // ----- Top 5 and lowest 5 published courses by enrolment -----
            DataTable top5 = GetTable(RankSql("DESC"));
            DataTable low5 = GetTable(RankSql("ASC"));

            // ----- Bars: enrolment per course (top 5) -----
            int maxEnrol = 0;
            foreach (DataRow r in top5.Rows)
                maxEnrol = Math.Max(maxEnrol, Convert.ToInt32(r["enrolments"]));
            int enrolAxis = NiceCeiling(maxEnrol);

            StringBuilder enrolBars = new StringBuilder();
            if (top5.Rows.Count == 0)
            {
                enrolBars.Append("<div class='note'>No published courses yet.</div>");
            }
            else
            {
                foreach (DataRow r in top5.Rows)
                {
                    int n = Convert.ToInt32(r["enrolments"]);
                    enrolBars.Append(BarRow(r["title"].ToString(), n, enrolAxis, n.ToString("N0"), false));
                }
                enrolBars.Append(BarAxis(enrolAxis));
            }
            enrolBars.Append("<div class='note'>Counts include all enrolments recorded for each course.</div>");
            litEnrolBars.Text = enrolBars.ToString();

            // ----- Column chart: which subject (topic) has the most courses -----
            // The topic with the MOST courses comes first.
            DataTable topicTable = GetTable(
                @"SELECT category, COUNT(*) AS total
                  FROM public.courses
                  WHERE category IS NOT NULL AND category <> ''
                  GROUP BY category
                  ORDER BY total DESC, category
                  LIMIT 6");

            litTopicBars.Text = BuildColumnChart(topicTable);

            // ----- Bars: the 5 courses with the most lessons -----
            DataTable lessonTop = GetTable(
                @"SELECT c.title, COUNT(l.lesson_id) AS lessons
                  FROM public.courses c
                  LEFT JOIN public.lesson l ON l.course_id = c.course_id
                  GROUP BY c.course_id, c.title
                  ORDER BY lessons DESC, c.title LIMIT 5");

            int maxLessons = 0;
            foreach (DataRow r in lessonTop.Rows)
                maxLessons = Math.Max(maxLessons, Convert.ToInt32(r["lessons"]));

            StringBuilder lessonBars = new StringBuilder();
            foreach (DataRow r in lessonTop.Rows)
            {
                int n = Convert.ToInt32(r["lessons"]);
                lessonBars.Append(BarRow(r["title"].ToString(), n, maxLessons, n.ToString(), false));
            }
            litLessonBars.Text = lessonBars.ToString();

            // ----- The two tables -----
            litTopCourses.Text = RankRows(top5);
            litLowCourses.Text = RankRows(low5);

            lblRankNote.Text = "Ranking covers " + published.ToString("N0") +
                               " published courses. Draft, pending-review and archived courses are excluded to compare live learning opportunities.";
        }

        // How many courses have this subject word in the title or the topic?
        private int CountSubject(string subject)
        {
            return GetNumber("SELECT COUNT(*) FROM public.courses WHERE title ILIKE '%" + subject + "%' OR category ILIKE '%" + subject + "%'");
        }

        // How many lessons are inside the courses of this subject?
        private int CountSubjectLessons(string subject)
        {
            return GetNumber(
                @"SELECT COUNT(*) FROM public.lesson l
                  JOIN public.courses c ON c.course_id = l.course_id
                  WHERE c.title ILIKE '%" + subject + "%' OR c.category ILIKE '%" + subject + "%'");
        }

        // The SQL for "published courses, ranked by how many learners enrolled".
        // order = "DESC" (most first) or "ASC" (fewest first)
        private string RankSql(string order)
        {
            return @"SELECT c.course_id, c.title, c.category, c.course_level, c.status::text AS status,
                            u.full_name AS instructor,
                            COUNT(e.enrollment_id) AS enrolments
                     FROM public.courses c
                     LEFT JOIN public.users u ON u.user_id = c.user_id
                     LEFT JOIN public.enrollment e ON e.course_id = c.course_id
                     WHERE c.status::text ILIKE 'publish%'
                     GROUP BY c.course_id, c.title, c.category, c.course_level, c.status, u.full_name
                     ORDER BY enrolments " + order + ", c.title LIMIT 5";
        }

        // Turns the ranking table into HTML rows
        private string RankRows(DataTable table)
        {
            if (table.Rows.Count == 0)
                return "<tr><td colspan='7' class='grey-text'>No published courses yet.</td></tr>";

            StringBuilder sb = new StringBuilder();
            int rank = 1;
            foreach (DataRow r in table.Rows)
            {
                sb.Append("<tr>");
                sb.Append("<td>" + rank + "</td>");
                sb.Append("<td>" + Server.HtmlEncode(r["title"].ToString()) + "</td>");
                sb.Append("<td>" + OrDash(r["instructor"]) + "</td>");
                sb.Append("<td>" + OrDash(r["course_level"]) + "</td>");
                sb.Append("<td>" + OrDash(r["category"]) + "</td>");
                sb.Append("<td>" + OrDash(r["status"]) + "</td>");
                sb.Append("<td>" + Convert.ToInt32(r["enrolments"]).ToString("N0") + "</td>");
                sb.Append("</tr>");
                rank++;
            }
            return sb.ToString();
        }

        // Empty database value -> a dash. Otherwise the text, made safe for the page.
        private string OrDash(object value)
        {
            if (value == null || value == DBNull.Value || value.ToString().Trim() == "") return "—";
            return Server.HtmlEncode(value.ToString());
        }

        // =====================================================
        //  TAB 3 : LEARNING PERFORMANCE
        //  (enrollment + quiz_score + quiz + lesson + courses tables)
        //  "Reporting cohort" = the PUBLISHED courses that have enrolments.
        // =====================================================
        private void LoadLearningTab()
        {
            // An enrolment counts as "complete" when ONE of these is true:
            //   1) its status says complete,
            //   2) its progression reached 100 (only if progression is a number column),
            //   3) the student finished EVERY lesson of the course (lesson_progress table).
            // (3) is the most reliable one, because lesson_progress is saved when a lesson is finished.
            string doneRule = "e.enrollment_status::text ILIKE 'complet%'";
            if (IsNumberType(ColumnType("enrollment", "progression")))
                doneRule += " OR e.progression >= 100";
            doneRule += " OR (COALESCE(lt.n, 0) > 0 AND COALESCE(ld.n, 0) >= lt.n)";

            // The PUBLISHED courses that have enrolments + how many of them are complete.
            // (A course that is still draft / pending review is left out, because students
            //  cannot enrol in it yet. Want only the top 5? Add  LIMIT 5  at the end.)
            DataTable cohort = GetTable(
                @"SELECT c.course_id, c.title, c.category, u.full_name AS instructor,
                         COUNT(e.enrollment_id) AS enrol,
                         COUNT(*) FILTER (WHERE (" + doneRule + @")) AS done
                  FROM public.courses c
                  JOIN public.enrollment e ON e.course_id = c.course_id
                  LEFT JOIN public.users u ON u.user_id = c.user_id
                  -- lt.n = how many lessons the course has
                  LEFT JOIN (SELECT course_id, COUNT(*) AS n
                             FROM public.lesson GROUP BY course_id) lt
                         ON lt.course_id = c.course_id
                  -- ld.n = how many of those lessons THIS student has finished
                  LEFT JOIN (SELECT lp.user_id, l.course_id, COUNT(DISTINCT l.lesson_id) AS n
                             FROM public.lesson_progress lp
                             JOIN public.lesson l ON l.lesson_id = lp.lesson_id
                             WHERE lp.is_completed
                             GROUP BY lp.user_id, l.course_id) ld
                         ON ld.user_id = e.user_id AND ld.course_id = c.course_id
                  WHERE c.status::text ILIKE 'publish%'
                  GROUP BY c.course_id, c.title, c.category, u.full_name
                  ORDER BY enrol DESC, c.title");

            // Average quiz score of every course (course_id -> score)
            Dictionary<int, int> quizByCourse = new Dictionary<int, int>();
            DataTable quizCourses = GetTable(
                @"SELECT l.course_id, ROUND(AVG(qs.total_score::numeric)) AS avg_score
                  FROM public.quiz_score qs
                  JOIN public.quiz q ON q.quiz_id = qs.quiz_id
                  JOIN public.lesson l ON l.lesson_id = q.lesson_id
                  GROUP BY l.course_id");
            foreach (DataRow r in quizCourses.Rows)
                quizByCourse[Convert.ToInt32(r["course_id"])] = Convert.ToInt32(r["avg_score"]);

            // Average quiz score of every topic (the 5 topics with the most quiz attempts)
            DataTable quizTopics = GetTable(
                @"SELECT c.category, ROUND(AVG(qs.total_score::numeric)) AS avg_score, COUNT(*) AS attempts
                  FROM public.quiz_score qs
                  JOIN public.quiz q ON q.quiz_id = qs.quiz_id
                  JOIN public.lesson l ON l.lesson_id = q.lesson_id
                  JOIN public.courses c ON c.course_id = l.course_id
                  WHERE c.category IS NOT NULL AND c.category <> ''
                  GROUP BY c.category
                  ORDER BY attempts DESC, c.category
                  LIMIT 5");

            // ----- Quiz score by topic bars -----
            StringBuilder quizBars = new StringBuilder();
            int quizSum = 0;
            string lowestTopic = "";
            int lowestScore = 101;

            foreach (DataRow r in quizTopics.Rows)
            {
                string topic = r["category"].ToString();
                int score = Convert.ToInt32(r["avg_score"]);
                quizSum += score;

                // low scores get an orange bar (below 74%)
                quizBars.Append(BarRow(topic, score, 100, score + "%", score < 74));

                if (score < lowestScore) { lowestScore = score; lowestTopic = topic; }
            }

            bool hasQuiz = quizTopics.Rows.Count > 0;
            int avgQuiz = hasQuiz ? (int)Math.Round(quizSum / (double)quizTopics.Rows.Count) : 0;

            if (!hasQuiz)
                quizBars.Append("<div class='note'>No quiz scores recorded yet.</div>");
            else if (lowestScore < 74)
                quizBars.Append("<div class='note'>Scores on a 0–100% scale · " + Server.HtmlEncode(lowestTopic) + " needs attention.</div>");
            else
                quizBars.Append("<div class='note'>Scores on a 0–100% scale · every topic is doing well.</div>");

            litQuizBars.Text = quizBars.ToString();

            lblQuiz.Text = hasQuiz ? avgQuiz + "%" : "—";
            lblQuizNote.Text = "Mean across " + quizTopics.Rows.Count + " topic scores";

            // ----- The cohort: bars + table + totals -----
            int totalEnrol = 0;
            int totalDone = 0;
            StringBuilder completionBars = new StringBuilder();
            StringBuilder table = new StringBuilder();
            HashSet<string> cohortTopics = new HashSet<string>();

            foreach (DataRow r in cohort.Rows)
            {
                int courseId = Convert.ToInt32(r["course_id"]);
                string name = r["title"].ToString();
                string topic = r["category"] == DBNull.Value ? "" : r["category"].ToString();
                int enrol = Convert.ToInt32(r["enrol"]);
                int done = Convert.ToInt32(r["done"]);
                int rate = (enrol == 0) ? 0 : (int)Math.Round(done * 100.0 / enrol);

                int quiz = -1;   // -1 = no quiz scores for this course
                if (quizByCourse.ContainsKey(courseId)) quiz = quizByCourse[courseId];

                totalEnrol += enrol;
                totalDone += done;
                if (topic != "") cohortTopics.Add(topic);

                // low completion gets an orange bar (below 70%)
                // the bar is as long as the completion rate, the number at the end is  completed/enrolled  (like 2/4)
                completionBars.Append(BarRow(name, rate, 100, done + "/" + enrol, rate < 70));

                table.Append("<tr><td>" + Server.HtmlEncode(name) + "</td><td>" + OrDash(topic) + "</td><td>" + enrol.ToString("N0") +
                             "</td><td>" + done.ToString("N0") + "</td><td>" + rate + "%</td><td>" +
                             (quiz < 0 ? "—" : quiz + "%") + "</td></tr>");
            }

            double overallRate = (totalEnrol == 0) ? 0 : totalDone * 100.0 / totalEnrol;

            lblEnrol.Text = totalEnrol.ToString("N0");
            lblEnrolNote.Text = "Across " + cohort.Rows.Count + " published courses";
            // The box shows how many (published) courses there are right now
            lblDone.Text = GetNumber("SELECT COUNT(*) FROM public.courses WHERE status::text ILIKE 'publish%'").ToString("N0");

            if (cohort.Rows.Count == 0)
            {
                completionBars.Append("<div class='note'>No enrolments in published courses yet.</div>");
                table.Append("<tr><td colspan='6' class='grey-text'>No enrolments in published courses yet.</td></tr>");
            }
            else
            {
                completionBars.Append("<div class='note'>Numbers show completed / enrolled students · the bar shows the completion rate (0–100%).</div>");

                table.Append("<tr class='total'><td>Published courses</td><td>" + cohortTopics.Count + " topics</td><td>" + totalEnrol.ToString("N0") +
                             "</td><td>" + totalDone.ToString("N0") + "</td><td>" + overallRate.ToString("0.0", Inv) +
                             "%</td><td>" + (hasQuiz ? avgQuiz + "%" : "—") + "</td></tr>");
            }

            litCompletionBars.Text = completionBars.ToString();
            litPerfTable.Text = table.ToString();
        }

        // =====================================================
        //  "WHO WAS ACTIVE?"
        //  A user is ACTIVE when they did something in the last 30 days.
        //  Every table below is checked, and a table is skipped if it
        //  (or its date column) is not in the database.
        // =====================================================
        private string RecentActivitySql()
        {
            if (recentActivitySql != null) return recentActivitySql;

            //                      table                 who             when
            string[][] sources =
            {
                new string[] { "enrollment",         "user_id",   "enrolled_at"  },   // joined a course
                new string[] { "lesson_progress",    "user_id",   "completed_at" },   // finished a lesson
                new string[] { "quiz_score",         "user_id",   "done_time"    },   // took a quiz
                new string[] { "discussion_posts",   "user_id",   "created_at"   },   // wrote a post
                new string[] { "discussion_replies", "user_id",   "created_at"   },   // wrote a reply
                new string[] { "messages",           "sender_id", "created_at"   },   // sent a message
                new string[] { "users",              "user_id",   "last_login"   },   // logged in (only if you add this column, see the note)
                new string[] { "users",              "user_id",   "created_at"   }    // a brand-new account is not "inactive"
            };

            List<string> parts = new List<string>();
            foreach (string[] s in sources)
            {
                if (ColumnType(s[0], s[1]) != null && IsTimeType(ColumnType(s[0], s[2])))
                {
                    parts.Add("SELECT " + s[1] + " FROM public." + s[0] + " WHERE " + s[2] + " >= now() - interval '30 days'");
                }
            }

            // The admin who is looking at this page RIGHT NOW is obviously active.
            // (Logging in is not saved anywhere in the database, so without this line
            //  an admin who only looks at pages would always be shown as "inactive".)
            if (adminId > 0)
                parts.Add("SELECT " + adminId);

            // Nothing found? Then nobody is active.
            if (parts.Count == 0)
                recentActivitySql = "SELECT user_id FROM public.users WHERE false";
            else
                recentActivitySql = string.Join(" UNION ", parts.ToArray());

            return recentActivitySql;
        }

        // Asks the database: "what kind of column is table.column?"  (null = it does not exist)
        private string ColumnType(string table, string column)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(
                "SELECT data_type FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @t AND column_name = @c", conn))
            {
                cmd.Parameters.AddWithValue("@t", table);
                cmd.Parameters.AddWithValue("@c", column);
                object result = cmd.ExecuteScalar();
                return result == null ? null : result.ToString();
            }
        }

        private bool IsTimeType(string type)
        {
            return type == "timestamp without time zone" || type == "timestamp with time zone" || type == "date";
        }

        private bool IsNumberType(string type)
        {
            return type == "integer" || type == "smallint" || type == "bigint" || type == "numeric" ||
                   type == "real" || type == "double precision";
        }

        // =====================================================
        //  HELPERS  (small tools used again and again)
        // =====================================================

        // 7 out of 10  ->  "70.0%"   (0 when there is nothing to divide by)
        private string Percent(int part, int whole)
        {
            double value = (whole == 0) ? 0 : part * 100.0 / whole;
            return value.ToString("0.0", Inv) + "%";
        }

        // Rounds a number UP to a "nice" one:  1284 -> 1500,  10218 -> 12000
        // (so the bottom numbers of a chart look tidy)
        private int NiceCeiling(int max)
        {
            if (max <= 0) return 1;

            double[] steps = { 1, 1.2, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10 };
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(max)));

            foreach (double s in steps)
            {
                if (s * magnitude >= max) return (int)Math.Round(s * magnitude);
            }
            return (int)Math.Round(10 * magnitude);
        }

        // The gap between the grey lines of a chart (1, 2, 5, 10, 20, 50 ...). There are 4 gaps.
        private int NiceStep(int biggest)
        {
            int[] niceSteps = { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 50000 };
            foreach (int s in niceSteps)
            {
                if (s * 4 >= biggest) return s;
            }
            return niceSteps[niceSteps.Length - 1];
        }

        // Makes ONE horizontal bar:   label ======== value
        // fill width = value / max * 100 percent
        private string BarRow(string label, int value, int max, string valueText, bool orange)
        {
            double percent = (max == 0) ? 0 : value * 100.0 / max;
            string color = orange ? " orange" : "";

            return "<div class='bar-row'>" +
                   "<div class='bar-label'>" + Server.HtmlEncode(label) + "</div>" +
                   "<div class='bar-track'><div class='bar-fill" + color + "' style='width:" + percent.ToString("0.0", Inv) + "%'></div></div>" +
                   "<div class='bar-value'>" + valueText + "</div>" +
                   "</div>";
        }

        // The little numbers under a group of bars:  0   (half)   (max)
        private string BarAxis(int max)
        {
            return "<div style='display:flex; justify-content:space-between; margin:2px 60px 0 170px; font-size:11px; color:#64748B;'>" +
                   "<span>0</span><span>" + (max / 2).ToString("N0") + "</span><span>" + max.ToString("N0") + "</span></div>";
        }

        // Draws a column chart for the "courses per topic" table.
        // The first column (the topic with the MOST courses) is the darkest blue.
        private string BuildColumnChart(DataTable topics)
        {
            if (topics.Rows.Count == 0)
                return "<div class='note'>No courses yet.</div>";

            List<string> names = new List<string>();
            List<int> values = new List<int>();
            List<string> colors = new List<string>();

            for (int i = 0; i < topics.Rows.Count; i++)
            {
                names.Add(topics.Rows[i]["category"].ToString());
                values.Add(Convert.ToInt32(topics.Rows[i]["total"]));
                colors.Add(i == 0 ? "#4F67CC" : "#8E9EE0");   // the winner is darker
            }

            // A sentence under the chart: which subject is the biggest?
            string winner = Server.HtmlEncode(names[0]);
            return DrawColumns(names, values, colors) +
                   "<div class='note'><b>" + winner + "</b> has the most courses (" + values[0] + ").</div>";
        }

        // Draws a column chart (tall bars standing up) using SVG.
        // grey lines + numbers on the left, a number on top of each bar, a name under each bar.
        private string DrawColumns(List<string> names, List<int> values, List<string> colors)
        {
            // 1) Find the biggest number
            int biggest = 1;
            foreach (int v in values) biggest = Math.Max(biggest, v);

            // 2) Choose the gap between the grey lines (about 4 lines)
            int step = NiceStep(biggest);
            int top = step * 4;   // the highest line of the chart

            // 3) The size of the chart area inside the picture
            double left = 40, right = 490, topY = 20, bottomY = 210;
            double height = bottomY - topY;

            StringBuilder svg = new StringBuilder();
            svg.Append("<svg viewBox='0 0 500 250' width='100%'>");

            // grey lines + numbers on the left (0, step, step*2 ...)
            for (int i = 0; i <= 4; i++)
            {
                double y = bottomY - (height * i / 4.0);
                string ys = y.ToString("0.0", Inv);
                svg.Append("<line x1='" + left.ToString("0", Inv) + "' y1='" + ys + "' x2='" + right.ToString("0", Inv) + "' y2='" + ys + "' stroke='#E2E8F0' />");
                svg.Append("<text x='32' y='" + (y + 4).ToString("0.0", Inv) + "' font-size='11' text-anchor='end' fill='#64748B'>" + (step * i) + "</text>");
            }

            // 4) One column for every item
            int count = names.Count;
            double slot = (right - left) / count;           // space for each column
            double barWidth = Math.Min(56, slot * 0.6);     // the bar is a bit thinner than its space

            for (int i = 0; i < count; i++)
            {
                string name = names[i];
                int value = values[i];

                double barHeight = value * height / top;
                double x = left + slot * i + (slot - barWidth) / 2;
                double y = bottomY - barHeight;
                double middle = x + barWidth / 2;

                // long names are cut short, so they do not overlap
                string shortName = (name.Length > 12) ? name.Substring(0, 11) + "…" : name;

                svg.Append("<rect x='" + x.ToString("0.0", Inv) + "' y='" + y.ToString("0.0", Inv) +
                           "' width='" + barWidth.ToString("0.0", Inv) + "' height='" + barHeight.ToString("0.0", Inv) +
                           "' rx='3' fill='" + colors[i] + "'><title>" + Server.HtmlEncode(name) + ": " + value + "</title></rect>");

                // the number on top of the bar
                svg.Append("<text x='" + middle.ToString("0.0", Inv) + "' y='" + (y - 6).ToString("0.0", Inv) +
                           "' font-size='12' font-weight='700' text-anchor='middle' fill='#0F172A'>" + value + "</text>");

                // the name under the bar
                svg.Append("<text x='" + middle.ToString("0.0", Inv) + "' y='230' font-size='11' text-anchor='middle' fill='#64748B'>" +
                           Server.HtmlEncode(shortName) + "</text>");
            }

            svg.Append("</svg>");
            return svg.ToString();
        }

        // Draws a simple line chart using SVG (a picture made from text)
        // with grey lines and numbers on the left, like the Figma design
        private string BuildLineChart(DataTable months)
        {
            if (months.Rows.Count == 0)
                return "<div class='note'>No registrations yet.</div>";

            int maxValue = 1;
            foreach (DataRow r in months.Rows)
                maxValue = Math.Max(maxValue, Convert.ToInt32(r["total"]));

            int step = NiceStep(maxValue);
            int top = step * 4;   // the highest grey line

            int count = months.Rows.Count;
            StringBuilder grid = new StringBuilder();     // grey lines + numbers on the left
            StringBuilder points = new StringBuilder();   // the line
            StringBuilder dots = new StringBuilder();     // dots + words

            for (int i = 0; i <= 4; i++)
            {
                double gy = 180 - i * 35;
                string gys = gy.ToString("0.0", Inv);
                grid.Append("<line x1='50' y1='" + gys + "' x2='490' y2='" + gys + "' stroke='#E2E8F0' />");
                grid.Append("<text x='42' y='" + (gy + 4).ToString("0.0", Inv) + "' font-size='11' text-anchor='end' fill='#64748B'>" + (step * i) + "</text>");
            }

            for (int i = 0; i < count; i++)
            {
                int value = Convert.ToInt32(months.Rows[i]["total"]);
                string name = months.Rows[i]["month_name"].ToString();

                // x goes left to right, y goes up when the value is bigger
                double x = (count == 1) ? 270 : 70 + i * (400.0 / (count - 1));
                double y = 180 - (value * 140.0 / top);

                string xs = x.ToString("0.0", Inv);
                string ys = y.ToString("0.0", Inv);

                points.Append(xs + "," + ys + " ");
                dots.Append("<circle cx='" + xs + "' cy='" + ys + "' r='4' fill='#4F67CC' />");
                dots.Append("<text x='" + xs + "' y='" + (y - 10).ToString("0.0", Inv) + "' font-size='11' text-anchor='middle' fill='#4F67CC'>" + value + "</text>");
                dots.Append("<text x='" + xs + "' y='205' font-size='11' text-anchor='middle' fill='#64748B'>" + name + "</text>");
            }

            return "<svg viewBox='0 0 500 220' width='100%'>" +
                   grid +
                   "<polyline points='" + points + "' fill='none' stroke='#4F67CC' stroke-width='2' />" +
                   dots +
                   "</svg>";
        }

        // Asks the database for ONE number (like a count)
        private int GetNumber(string sql)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }

        // Asks the database for a TABLE of rows
        private DataTable GetTable(string sql)
        {
            DataTable table = new DataTable();
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(sql, conn))
            {
                adapter.Fill(table);
            }
            return table;
        }

        // =====================================================
        //  WHO IS LOGGED IN?  (used by every admin page)
        //  The login page saves the user number in Session["UserID"].
        //  (Session["user_id"] is also checked, in case an older page saved it.)
        //  Gives back 0 when nobody is logged in.
        // =====================================================
        private int CurrentUserId()
        {
            int id;
            if (int.TryParse(Convert.ToString(Session["UserID"]), out id) && id > 0) return id;
            if (int.TryParse(Convert.ToString(Session["user_id"]), out id) && id > 0) return id;

            // The session was forgotten (idle for a long time)? Ask the "remember me" cookie who the admin is.
            return WAPP_EduCore.Data.LoginKeeper.Restore(Context);
        }
    }
}