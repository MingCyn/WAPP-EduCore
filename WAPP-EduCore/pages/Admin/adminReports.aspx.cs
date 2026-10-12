using System;
using System.Data;
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

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadAdminInfo();   // runs every time the page opens

            if (!IsPostBack)
            {
                lblSnapshot.Text = "Snapshot · " + DateTime.Now.ToString("dd MMM yyyy");

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

                if (Session["user_id"] != null)
                {
                    cmd.CommandText = "SELECT full_name FROM public.users WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(Session["user_id"]));
                }
                else
                {
                    cmd.CommandText = "SELECT full_name FROM public.users WHERE role = 'Admin' ORDER BY user_id LIMIT 1";
                }

                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    string fullName = result.ToString().Trim();
                    AdminName = Server.HtmlEncode(fullName);

                    // Initials = first letter of the first word + first letter of the last word
                    string[] words = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 1)
                        AdminInitials = words[0].Substring(0, 1).ToUpper();
                    else if (words.Length > 1)
                        AdminInitials = (words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, 1)).ToUpper();
                }
            }
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
        //  TAB 1 : USERS  (real data from the users table)
        // =====================================================
        private void LoadUsersTab()
        {
            int total = GetNumber("SELECT COUNT(*) FROM public.users");
            int students = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Student'");
            int tutors = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Tutor'");
            int admins = total - students - tutors;   // everyone else
            int newThisMonth = GetNumber(
                "SELECT COUNT(*) FROM public.users WHERE created_at >= date_trunc('month', now())");

            lblTotalUsers.Text = total.ToString("N0");
            lblStudents.Text = students.ToString("N0");
            lblTutors.Text = tutors.ToString("N0");
            lblNewUsers.Text = newThisMonth.ToString("N0");

            // ----- Line chart: new accounts per month -----
            DataTable months = GetTable(
                @"SELECT to_char(date_trunc('month', created_at), 'Mon') AS month_name,
                         COUNT(*) AS total
                  FROM public.users
                  WHERE created_at >= date_trunc('month', now()) - interval '5 months'
                  GROUP BY date_trunc('month', created_at)
                  ORDER BY date_trunc('month', created_at)");
            litLineChart.Text = BuildLineChart(months);

            // ----- Bars: users by role -----
            StringBuilder bars = new StringBuilder();
            bars.Append(BarRow("Student", students, total, students.ToString("N0"), false));
            bars.Append(BarRow("Tutor", tutors, total, tutors.ToString("N0"), false));
            bars.Append(BarRow("Admin", admins, total, admins.ToString("N0"), false));
            litRoleBars.Text = bars.ToString();

            // ----- Role table -----
            StringBuilder rows = new StringBuilder();
            rows.Append(RoleTableRow("Student", students, total));
            rows.Append(RoleTableRow("Tutor", tutors, total));
            rows.Append(RoleTableRow("Admin", admins, total));
            rows.Append("<tr class='total'><td>All roles</td><td>" + total.ToString("N0") + "</td><td>100%</td></tr>");
            litRoleTable.Text = rows.ToString();
        }

        private string RoleTableRow(string role, int count, int total)
        {
            double share = (total == 0) ? 0 : count * 100.0 / total;
            return "<tr><td>" + role + "</td><td>" + count.ToString("N0") + "</td><td>" + share.ToString("0.0") + "%</td></tr>";
        }

        // =====================================================
        //  TAB 2 : COURSES  (real data from courses + lesson tables)
        // =====================================================
        private void LoadCoursesTab()
        {
            lblTotalCourses.Text = GetNumber("SELECT COUNT(*) FROM public.courses").ToString("N0");
            lblTopics.Text = GetNumber("SELECT COUNT(DISTINCT category) FROM public.courses").ToString("N0");
            lblLessons.Text = GetNumber("SELECT COUNT(*) FROM public.lesson").ToString("N0");
            lblNewCourses.Text = GetNumber(
                "SELECT COUNT(*) FROM public.courses WHERE created_at >= date_trunc('month', now())").ToString("N0");

            // ----- Bars: courses per topic -----
            DataTable topics = GetTable(
                @"SELECT category, COUNT(*) AS total
                  FROM public.courses
                  GROUP BY category
                  ORDER BY total DESC
                  LIMIT 6");

            int maxTopic = 0;
            foreach (DataRow r in topics.Rows)
                maxTopic = Math.Max(maxTopic, Convert.ToInt32(r["total"]));

            StringBuilder topicBars = new StringBuilder();
            foreach (DataRow r in topics.Rows)
            {
                int n = Convert.ToInt32(r["total"]);
                topicBars.Append(BarRow(r["category"].ToString(), n, maxTopic, n.ToString(), false));
            }
            litTopicBars.Text = topicBars.ToString();

            // ----- Top 5 courses by lesson count -----
            string lessonSql =
                @"SELECT c.title, c.category, COUNT(l.lesson_id) AS lessons
                  FROM public.courses c
                  LEFT JOIN public.lesson l ON l.course_id = c.course_id
                  GROUP BY c.course_id, c.title, c.category
                  ORDER BY lessons ";

            DataTable top5 = GetTable(lessonSql + "DESC, c.title LIMIT 5");
            DataTable low5 = GetTable(lessonSql + "ASC, c.title LIMIT 5");

            // bars for top 5
            int maxLessons = 0;
            foreach (DataRow r in top5.Rows)
                maxLessons = Math.Max(maxLessons, Convert.ToInt32(r["lessons"]));

            StringBuilder lessonBars = new StringBuilder();
            foreach (DataRow r in top5.Rows)
            {
                int n = Convert.ToInt32(r["lessons"]);
                lessonBars.Append(BarRow(r["title"].ToString(), n, maxLessons, n.ToString(), false));
            }
            litLessonBars.Text = lessonBars.ToString();

            litTopCourses.Text = CourseRows(top5);
            litLowCourses.Text = CourseRows(low5);
        }

        private string CourseRows(DataTable table)
        {
            StringBuilder sb = new StringBuilder();
            int rank = 1;
            foreach (DataRow r in table.Rows)
            {
                sb.Append("<tr>");
                sb.Append("<td>" + rank + "</td>");
                sb.Append("<td>" + Server.HtmlEncode(r["title"].ToString()) + "</td>");
                sb.Append("<td>" + Server.HtmlEncode(r["category"].ToString()) + "</td>");
                sb.Append("<td>" + r["lessons"] + "</td>");
                sb.Append("</tr>");
                rank++;
            }
            return sb.ToString();
        }

        // =====================================================
        //  TAB 3 : LEARNING PERFORMANCE  (SAMPLE numbers!)
        //  There is no enrolment or quiz table yet, so we type
        //  the numbers ourselves. Later, replace them with a
        //  database query. Easy to change during the demo!
        // =====================================================
        private void LoadLearningTab()
        {
            //                     course name                   topic           enrolments  completions  quiz score
            object[][] sample =
            {
                new object[] { "Applied Data Literacy",     "Data Science", 1284, 1053, 82 },
                new object[] { "Introduction to Python",    "Programming",  1106,  863, 79 },
                new object[] { "Climate Systems Lab",       "Science",       942,  716, 84 },
                new object[] { "Modern History Survey",     "History",       876,  631, 77 },
                new object[] { "Foundations of Algebra",    "Mathematics",   638,  408, 73 }
            };

            int totalEnrol = 0;
            int totalDone = 0;
            int quizSum = 0;
            StringBuilder completionBars = new StringBuilder();
            StringBuilder quizBars = new StringBuilder();
            StringBuilder table = new StringBuilder();

            string weakest = "";
            int weakestRate = 101;
            int weakestQuiz = 0;

            foreach (object[] row in sample)
            {
                string name = (string)row[0];
                string topic = (string)row[1];
                int enrol = (int)row[2];
                int done = (int)row[3];
                int quiz = (int)row[4];
                int rate = (int)Math.Round(done * 100.0 / enrol);

                totalEnrol += enrol;
                totalDone += done;
                quizSum += quiz;

                // low scores get an orange bar (below 70% rate or below 74% quiz)
                completionBars.Append(BarRow(name, rate, 100, rate + "%", rate < 70));
                quizBars.Append(BarRow(topic, quiz, 100, quiz + "%", quiz < 74));

                table.Append("<tr><td>" + name + "</td><td>" + topic + "</td><td>" + enrol.ToString("N0") +
                             "</td><td>" + done.ToString("N0") + "</td><td>" + rate + "%</td><td>" + quiz + "%</td></tr>");

                if (rate < weakestRate)
                {
                    weakestRate = rate;
                    weakest = name;
                    weakestQuiz = quiz;
                }
            }

            double overallRate = totalDone * 100.0 / totalEnrol;
            int avgQuiz = (int)Math.Round(quizSum / (double)sample.Length);

            lblEnrol.Text = totalEnrol.ToString("N0");
            lblDone.Text = totalDone.ToString("N0");
            lblRate.Text = overallRate.ToString("0.0") + "%";
            lblQuiz.Text = avgQuiz + "%";

            table.Append("<tr class='total'><td>Reporting cohort</td><td>5 topics</td><td>" + totalEnrol.ToString("N0") +
                         "</td><td>" + totalDone.ToString("N0") + "</td><td>" + overallRate.ToString("0.0") +
                         "%</td><td>" + avgQuiz + "%</td></tr>");

            litCompletionBars.Text = completionBars.ToString();
            litQuizBars.Text = quizBars.ToString();
            litPerfTable.Text = table.ToString();

            litFocus.Text = "<b>Focus area · " + weakest + "</b><br />" +
                            weakestRate + "% completion and a " + weakestQuiz + "% quiz score. Review lesson pacing and quiz support.";
        }

        // =====================================================
        //  HELPERS  (small tools used again and again)
        // =====================================================

        // Makes ONE horizontal bar:   label ======== value
        // fill width = value / max * 100 percent
        private string BarRow(string label, int value, int max, string valueText, bool orange)
        {
            double percent = (max == 0) ? 0 : value * 100.0 / max;
            string color = orange ? " orange" : "";

            return "<div class='bar-row'>" +
                   "<div class='bar-label'>" + Server.HtmlEncode(label) + "</div>" +
                   "<div class='bar-track'><div class='bar-fill" + color + "' style='width:" + percent.ToString("0.0") + "%'></div></div>" +
                   "<div class='bar-value'>" + valueText + "</div>" +
                   "</div>";
        }

        // Draws a simple line chart using SVG (a picture made from text)
        private string BuildLineChart(DataTable months)
        {
            if (months.Rows.Count == 0)
                return "<div class='note'>No registrations yet.</div>";

            int maxValue = 1;
            foreach (DataRow r in months.Rows)
                maxValue = Math.Max(maxValue, Convert.ToInt32(r["total"]));

            int count = months.Rows.Count;
            StringBuilder points = new StringBuilder();   // the line
            StringBuilder dots = new StringBuilder();     // dots + words

            for (int i = 0; i < count; i++)
            {
                int value = Convert.ToInt32(months.Rows[i]["total"]);
                string name = months.Rows[i]["month_name"].ToString();

                // x goes left to right, y goes up when the value is bigger
                double x = (count == 1) ? 250 : 40 + i * (420.0 / (count - 1));
                double y = 180 - (value * 140.0 / maxValue);

                string xs = x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                string ys = y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

                points.Append(xs + "," + ys + " ");
                dots.Append("<circle cx='" + xs + "' cy='" + ys + "' r='4' fill='#4F67CC' />");
                dots.Append("<text x='" + xs + "' y='" + (y - 10).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "' font-size='11' text-anchor='middle' fill='#4F67CC'>" + value + "</text>");
                dots.Append("<text x='" + xs + "' y='205' font-size='11' text-anchor='middle' fill='#64748B'>" + name + "</text>");
            }

            return "<svg viewBox='0 0 500 220' width='100%'>" +
                   "<line x1='30' y1='180' x2='480' y2='180' stroke='#E2E8F0' />" +
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
    }
}