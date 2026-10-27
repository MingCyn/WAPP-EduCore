using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web.UI;
using Npgsql;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    // NOTE: the class name is WebForm1 because your .aspx first line says Inherits="...WebForm1"
    public partial class WebForm1 : System.Web.UI.Page
    {
        // These are shown in the sidebar and the top-right circle
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        // Colors used by the charts (same blues as the Figma design)
        private string[] chartColors = { "#4F67CC", "#8E9EE0", "#E0A85A", "#C7D0F0", "#4F67CC", "#8E9EE0" };


        // =====================================================
        //  STEP 1: Page opens  ->  fill every box with data
        // =====================================================
        protected void Page_Load(object sender, EventArgs e)
        {
            LoadAdminInfo();        // the admin's name
            LoadTopNumbers();       // the 6 number boxes
            LoadRoleDonut();        // chart 1 : Users by Role
            LoadTopicChart();       // chart 2 : Courses by Topic
            LoadRecentActivity();   // Recent Activity list
            LoadNeedsAttention();   // the yellow box
        }


        // =====================================================
        //  ADMIN NAME (sidebar + top-right circle)
        // =====================================================
        private void LoadAdminInfo()
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;

                if (CurrentUserId() > 0)
                {
                    cmd.CommandText = "SELECT full_name FROM public.users WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("@id", CurrentUserId());
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
        //  ROW 1 : THE 6 NUMBER BOXES
        // =====================================================
        private void LoadTopNumbers()
        {
            int totalUsers = GetNumber("SELECT COUNT(*) FROM public.users");
            int newUsers = GetNumber("SELECT COUNT(*) FROM public.users WHERE created_at >= date_trunc('month', now())");
            int admins = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Admin'");

            int totalCourses = GetNumber("SELECT COUNT(*) FROM public.courses");
            int topics = GetNumber("SELECT COUNT(DISTINCT category) FROM public.courses");
            int lessons = GetNumber("SELECT COUNT(*) FROM public.lesson");

            lblTotalUsers.Text = totalUsers.ToString("N0");
            lblNewUsers.Text = newUsers.ToString("N0");
            lblAdmins.Text = admins.ToString("N0");

            lblTotalCourses.Text = totalCourses.ToString("N0");
            lblCoursesNote.Text = "Across " + topics + " topics";
            lblLessons.Text = lessons.ToString("N0");
            lblTopics.Text = topics.ToString("N0");
        }


        // =====================================================
        //  ROW 2 - CHART 1 : DONUT  (Users by Role)
        // =====================================================
        private void LoadRoleDonut()
        {
            int total = GetNumber("SELECT COUNT(*) FROM public.users");
            int students = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Student'");
            int tutors = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Tutor'");
            int admins = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Admin'");

            string[] names = { "Student", "Tutor", "Admin" };
            int[] counts = { students, tutors, admins };
            string[] colors = { "#4F67CC", "#8E9EE0", "#C7D0F0" };

            // ----- The ring -----
            // A circle line has a length (circumference).
            // Each role gets a piece of that line: its share of the users.
            double radius = 80;
            double circle = 2 * Math.PI * radius;
            double start = 0;                       // where the next piece starts

            StringBuilder svg = new StringBuilder();
            svg.Append("<svg viewBox='0 0 210 210' width='210' height='210'>");
            svg.Append("<circle cx='105' cy='105' r='80' fill='none' stroke='#EEF2FF' stroke-width='30' />");   // grey ring behind

            for (int i = 0; i < counts.Length; i++)
            {
                double piece = (total == 0) ? 0 : counts[i] * circle / total;

                svg.Append("<circle cx='105' cy='105' r='80' fill='none' stroke='" + colors[i] + "' stroke-width='30'" +
                           " stroke-dasharray='" + Num(piece) + " " + Num(circle - piece) + "'" +
                           " stroke-dashoffset='" + Num(-start) + "'" +
                           " transform='rotate(-90 105 105)' />");

                start = start + piece;
            }
            svg.Append("</svg>");

            // The number in the middle of the ring
            litDonut.Text = "<div class='donut'>" + svg +
                            "<div class='donut-center'>" +
                            "<div class='donut-number'>" + total.ToString("N0") + "</div>" +
                            "<div class='donut-label'>total users</div>" +
                            "</div></div>";

            // ----- The list on the right (Student / Tutor / Admin) -----
            StringBuilder legend = new StringBuilder();
            for (int i = 0; i < counts.Length; i++)
            {
                double percent = (total == 0) ? 0 : counts[i] * 100.0 / total;

                legend.Append("<div class='legend-item'>");
                legend.Append("<div class='legend-name'><span class='dot' style='background:" + colors[i] + "'></span>" + names[i] + "</div>");
                legend.Append("<div class='legend-number'>" + counts[i].ToString("N0") +
                              "<span class='legend-percent'>· " + percent.ToString("0.0", CultureInfo.InvariantCulture) + "%</span></div>");
                legend.Append("</div>");
            }
            litLegend.Text = legend.ToString();
        }


        // =====================================================
        //  ROW 2 - CHART 2 : COLUMNS  (Courses by Topic)
        //  The 5 topics that have the most courses.
        // =====================================================
        private void LoadTopicChart()
        {
            int totalCourses = GetNumber("SELECT COUNT(*) FROM public.courses");

            DataTable topics = GetTable(
                @"SELECT COALESCE(category, 'Other') AS topic, COUNT(*) AS total
                  FROM public.courses
                  GROUP BY COALESCE(category, 'Other')
                  ORDER BY total DESC, topic
                  LIMIT 5");

            lblTopicSubtitle.Text = totalCourses.ToString("N0") + " courses across the catalogue";

            if (topics.Rows.Count == 0)
            {
                litTopicChart.Text = "<div class='note'>No courses yet.</div>";
                return;
            }

            // The biggest number decides how tall the tallest column is
            int biggest = 1;
            foreach (DataRow r in topics.Rows)
                biggest = Math.Max(biggest, Convert.ToInt32(r["total"]));

            StringBuilder columns = new StringBuilder("<div class='col-chart'>");
            StringBuilder labels = new StringBuilder("<div class='col-labels'>");

            int i = 0;
            foreach (DataRow r in topics.Rows)
            {
                int n = Convert.ToInt32(r["total"]);

                // height in pixels: the biggest column is 150px tall
                int height = (int)Math.Round(n * 150.0 / biggest);
                if (height < 6) height = 6;

                columns.Append("<div class='col'>" +
                               "<div class='col-value'>" + n + "</div>" +
                               "<div class='col-bar' style='height:" + height + "px; background:" + chartColors[i % chartColors.Length] + "'></div>" +
                               "</div>");

                labels.Append("<div class='col'>" + Server.HtmlEncode(r["topic"].ToString()) + "</div>");
                i++;
            }

            columns.Append("</div>");
            labels.Append("</div>");

            litTopicChart.Text = columns.ToString() + labels.ToString();
        }


        // =====================================================
        //  ROW 3 - RECENT ACTIVITY
        //  The 5 newest things: new accounts and new courses.
        // =====================================================
        private void LoadRecentActivity()
        {
            DataTable events = GetTable(
                @"SELECT who, kind, detail, happened
                  FROM (
                        SELECT full_name AS who,
                               'Account created' AS kind,
                               'Joined as ' || COALESCE(role, 'user') AS detail,
                               created_at AS happened
                        FROM public.users

                        UNION ALL

                        SELECT title AS who,
                               'Course added' AS kind,
                               'New course in ' || COALESCE(category, 'the catalogue') AS detail,
                               created_at AS happened
                        FROM public.courses
                       ) AS events
                  ORDER BY happened DESC NULLS LAST
                  LIMIT 5");

            if (events.Rows.Count == 0)
            {
                litActivity.Text = "<div class='note'>Nothing has happened yet.</div>";
                return;
            }

            // Two little pictures: a person and a book
            string personIcon = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M16 21v-2a4 4 0 00-4-4H6a4 4 0 00-4 4v2'/><circle cx='9' cy='7' r='4'/><line x1='20' y1='8' x2='20' y2='14'/><line x1='17' y1='11' x2='23' y2='11'/></svg>";
            string bookIcon = "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z'/><path d='M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z'/></svg>";

            StringBuilder sb = new StringBuilder();
            foreach (DataRow r in events.Rows)
            {
                string kind = r["kind"].ToString();
                string icon = (kind == "Course added") ? bookIcon : personIcon;

                string date = "";
                if (r["happened"] != DBNull.Value)
                    date = Convert.ToDateTime(r["happened"]).ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);

                sb.Append("<div class='activity-item'>");
                sb.Append("<div class='activity-icon'>" + icon + "</div>");
                sb.Append("<div class='activity-main'>");
                sb.Append("<div class='activity-name'>" + Server.HtmlEncode(r["who"].ToString()) + "</div>");
                sb.Append("<div class='activity-text'>" + Server.HtmlEncode(r["detail"].ToString()) + "</div>");
                sb.Append("<div class='activity-date'>" + date + "</div>");
                sb.Append("</div>");
                sb.Append("<div class='activity-kind'>" + kind + "</div>");
                sb.Append("</div>");
            }
            litActivity.Text = sb.ToString();
        }


        // =====================================================
        //  ROW 3 - NEEDS ATTENTION (yellow box, only courses without lessons)
        // =====================================================
        private void LoadNeedsAttention()
        {
            // Courses that have no lessons at all
            int noLessons = GetNumber(
                @"SELECT COUNT(*) FROM public.courses c
                  WHERE NOT EXISTS (SELECT 1 FROM public.lesson l WHERE l.course_id = c.course_id)");

            lblNoLessons.Text = noLessons.ToString("N0");
        }


        // =====================================================
        //  LOGOUT  (the "Logout" option in the drop-down menu)
        // =====================================================
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();        // forget who was logged in
            Session.Abandon();      // end the session
            Response.Redirect("~/auth/login.aspx");   // go to the login page
        }


        // =====================================================
        //  HELPERS  (small tools used again and again)
        // =====================================================

        // Turns a decimal number into text the browser understands (always with a dot)
        private string Num(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        // Asks the database for ONE number (like a count)
        private int GetNumber(string sql)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
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
            return 0;
        }
    }
}