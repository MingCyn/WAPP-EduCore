using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI;
using Npgsql;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Tutor
{
    // NOTE: the class name is WebForm4 because your .aspx first line says Inherits="...WebForm4"
    public partial class WebForm4 : System.Web.UI.Page
    {
        private const int PageSize = 8;      // how many students per page (change me!)
        private int tutorId = 0;             // the tutor who is logged in

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ONE student in ONE course (one line of the table)
        private class StudentRow
        {
            public int UserId;
            public string Name;
            public int CourseId;
            public string Course;
            public int Progress;     // 0 - 100
            public int Score;        // 0 - 100, or -1 when the student has no quiz score yet
            public bool Done;        // finished the course?
            public string Status;    // "On Track", "Needs Attention" or "At Risk"
        }

        // The page number we are on (0 = first page). It is remembered between clicks.
        private int CurrentPage
        {
            get { return ViewState["page"] == null ? 0 : (int)ViewState["page"]; }
            set { ViewState["page"] = value; }
        }


        // =====================================================
        //  STEP 1: Page opens  ->  only a logged-in Tutor may see this page
        //  (the login page saves Session["UserRole"] and Session["UserID"],
        //   the same as the other tutor pages)
        // =====================================================
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserRole"] == null ||
                !Session["UserRole"].ToString().Equals("Tutor", StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(Convert.ToString(Session["UserID"]), out tutorId))
            {
                Response.Redirect("../../auth/login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
        }

        // =====================================================
        //  STEP 2: PreRender runs after every click and just BEFORE the page
        //  is drawn, so everything on the page is always fresh.
        // =====================================================
        protected void Page_PreRender(object sender, EventArgs e)
        {
            if (tutorId == 0) return;                // not logged in: Page_Load is already sending the user to the login page

            List<StudentRow> all = LoadStudents();   // every student of this tutor

            ShowTopPart(all);                        // title + 3 boxes
            ShowDonut(all);                          // the donut chart
            ShowTable(all);                          // search + filter + sort + pages
        }


        // =====================================================
        //  GET THE STUDENTS from the database
        //  = people who enrolled in a course that THIS tutor owns (courses.user_id)
        // =====================================================
        private List<StudentRow> LoadStudents()
        {
            // The progress is a number from 0 to 100.
            // (regexp_replace removes things like "%", so "85%" also works)
            string progressSql = "COALESCE(NULLIF(regexp_replace(e.progression::text, '[^0-9.]', '', 'g'), '')::numeric, 0)";

            DataTable table = new DataTable();
            string sql =
                @"SELECT e.user_id, u.full_name, c.course_id, c.title,
                         " + progressSql + @" AS progress,
                         s.score,
                         (e.enrollment_status::text ILIKE 'complet%') AS done
                  FROM public.enrollment e
                  JOIN public.users u   ON u.user_id = e.user_id
                  JOIN public.courses c ON c.course_id = e.course_id
                  LEFT JOIN (SELECT qs.user_id, l.course_id, AVG(qs.total_score::numeric) AS score
                             FROM public.quiz_score qs
                             JOIN public.quiz q   ON q.quiz_id = qs.quiz_id
                             JOIN public.lesson l ON l.lesson_id = q.lesson_id
                             GROUP BY qs.user_id, l.course_id) s
                         ON s.user_id = e.user_id AND s.course_id = e.course_id
                  WHERE c.user_id = @tutor
                  ORDER BY u.full_name";

            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@tutor", tutorId);
                using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd))
                {
                    adapter.Fill(table);
                }
            }

            List<StudentRow> list = new List<StudentRow>();
            foreach (DataRow r in table.Rows)
            {
                StudentRow s = new StudentRow();
                s.UserId = Convert.ToInt32(r["user_id"]);
                s.Name = r["full_name"].ToString();
                s.CourseId = Convert.ToInt32(r["course_id"]);
                s.Course = r["title"].ToString();
                s.Progress = (int)Math.Round(Math.Min(100, Convert.ToDouble(r["progress"], Inv)));
                s.Score = (r["score"] == DBNull.Value) ? -1 : (int)Math.Round(Convert.ToDouble(r["score"], Inv));
                s.Done = (r["done"] != DBNull.Value && Convert.ToBoolean(r["done"])) || s.Progress >= 100;
                s.Status = WorkOutStatus(s.Progress, s.Score);
                list.Add(s);
            }
            return list;
        }

        // The rules for the status (change the numbers if you want):
        //   At Risk          = progress below 40%  OR  score below 50%
        //   Needs Attention  = progress below 60%  OR  score below 70%
        //   On Track         = everything else
        private string WorkOutStatus(int progress, int score)
        {
            if (progress < 40 || (score >= 0 && score < 50)) return "At Risk";
            if (progress < 60 || (score >= 0 && score < 70)) return "Needs Attention";
            return "On Track";
        }


        // =====================================================
        //  TITLE + THE 3 NUMBER BOXES
        // =====================================================
        private void ShowTopPart(List<StudentRow> all)
        {
            // "Advanced Data Structures • 56 Enrolled Students"
            int students = all.Select(s => s.UserId).Distinct().Count();
            List<string> courses = all.Select(s => s.Course).Distinct().ToList();

            string courseText = (courses.Count == 1) ? courses[0] : (courses.Count == 0 ? "No courses yet" : "All my courses (" + courses.Count + ")");
            lblCourseLine.Text = Server.HtmlEncode(courseText) + " &bull; " + students + " Enrolled Students";

            // Class average score = the mean of the students' quiz scores
            List<int> scores = all.Where(s => s.Score >= 0).Select(s => s.Score).ToList();
            if (scores.Count > 0)
            {
                lblAvg.Text = scores.Average().ToString("0.0", Inv) + "%";
                lblAvgNote.Text = "Mean quiz score of " + scores.Count + " students";
            }
            else
            {
                lblAvg.Text = "—";
                lblAvgNote.Text = "No quiz scores yet";
            }

            // Completion rate = finished / all
            int done = all.Count(s => s.Done);
            double rate = (all.Count == 0) ? 0 : done * 100.0 / all.Count;
            lblRate.Text = Math.Round(rate).ToString("0", Inv) + "%";
            lblRateNote.Text = done + " students finished all modules";

            // At risk
            lblRisk.Text = all.Count(s => s.Status == "At Risk") + " Students";
        }


        // =====================================================
        //  THE DONUT CHART  (On Track / Needs Attention / At Risk)
        //  Drawn with SVG: 3 circles, each one shows a part of the ring.
        // =====================================================
        private void ShowDonut(List<StudentRow> all)
        {
            int ok = all.Count(s => s.Status == "On Track");
            int warn = all.Count(s => s.Status == "Needs Attention");
            int risk = all.Count(s => s.Status == "At Risk");
            int total = ok + warn + risk;

            string[] names = { "On Track", "Needs Attention", "At Risk" };
            int[] values = { ok, warn, risk };
            string[] colors = { "#4F67CC", "#E5B05A", "#C62828" };

            double radius = 40;
            double ring = 2 * Math.PI * radius;      // the length of the whole ring
            double used = 0;                         // how much of the ring is already painted

            StringBuilder svg = new StringBuilder();
            svg.Append("<svg width='110' height='110' viewBox='0 0 100 100' style='flex-shrink:0'>");
            svg.Append("<circle cx='50' cy='50' r='40' fill='none' stroke='#EEF0F6' stroke-width='16' />");

            StringBuilder legend = new StringBuilder();

            for (int i = 0; i < 3; i++)
            {
                double part = (total == 0) ? 0 : values[i] * 100.0 / total;

                if (values[i] > 0)
                {
                    double length = ring * values[i] / total;
                    // rotate -90 so the ring starts at the top
                    svg.Append("<circle cx='50' cy='50' r='40' fill='none' stroke='" + colors[i] + "' stroke-width='16' " +
                               "stroke-dasharray='" + length.ToString("0.00", Inv) + " " + (ring - length).ToString("0.00", Inv) + "' " +
                               "stroke-dashoffset='" + (-used).ToString("0.00", Inv) + "' transform='rotate(-90 50 50)' />");
                    used += length;
                }

                legend.Append("<div><span class='dot' style='background:" + colors[i] + "'></span>" + names[i] +
                              " (" + Math.Round(part).ToString("0", Inv) + "%)</div>");
            }

            svg.Append("</svg>");
            litDonut.Text = svg.ToString();
            litLegend.Text = legend.ToString();
        }


        // =====================================================
        //  THE TABLE  (search + status filter + sort + pages)
        // =====================================================
        private void ShowTable(List<StudentRow> all)
        {
            IEnumerable<StudentRow> list = all;

            // 1) Search by name
            string search = txtSearch.Text.Trim();
            if (search != "")
                list = list.Where(s => s.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);

            // 2) Filter by status
            if (ddlStatus.SelectedValue != "")
                list = list.Where(s => s.Status == ddlStatus.SelectedValue);

            // 3) Sort
            if (ddlSort.SelectedValue == "score")
                list = list.OrderByDescending(s => s.Score).ThenBy(s => s.Name);
            else if (ddlSort.SelectedValue == "name")
                list = list.OrderBy(s => s.Name);
            else
                list = list.OrderByDescending(s => s.Progress).ThenBy(s => s.Name);

            List<StudentRow> found = list.ToList();

            // 4) Make sure the page number is not too big
            int lastPage = Math.Max(0, (found.Count - 1) / PageSize);
            if (CurrentPage > lastPage) CurrentPage = lastPage;
            if (CurrentPage < 0) CurrentPage = 0;

            // 5) Only the students of this page
            List<StudentRow> pageRows = found.Skip(CurrentPage * PageSize).Take(PageSize).ToList();

            StringBuilder rows = new StringBuilder();
            foreach (StudentRow s in pageRows)
                rows.Append(BuildRow(s));
            litRows.Text = rows.ToString();

            // 6) Table or "No students found"
            pnlTable.Visible = (found.Count > 0);
            pnlEmpty.Visible = (found.Count == 0);

            // 7) "Showing 8 of 56 students"  +  "Page 1 of 7"
            lblShowing.Text = "Showing " + pageRows.Count + " of " + found.Count + " students";
            lblPage.Text = "Page " + (CurrentPage + 1) + " of " + (lastPage + 1);
            btnPrev.Enabled = (CurrentPage > 0);
            btnNext.Enabled = (CurrentPage < lastPage);
        }

        // Makes ONE table row (the same classes as the tutor dashboard: badge, progress bar...)
        private string BuildRow(StudentRow s)
        {
            string badge = (s.Status == "On Track") ? "status-on-track" : (s.Status == "At Risk" ? "status-risk" : "status-attention");

            // The name is a link to the student's performance page
            string link = "studentPerformance.aspx?studentId=" + s.UserId + "&courseId=" + s.CourseId;

            StringBuilder sb = new StringBuilder();
            sb.Append("<tr>");
            sb.Append("<td><div class='sm-student'><span class='sm-avatar'>" + MakeInitials(s.Name) + "</span><a href='" + link + "'>" + Server.HtmlEncode(s.Name) + "</a></div></td>");
            sb.Append("<td>" + Server.HtmlEncode(s.Course) + "</td>");
            sb.Append("<td><div class='table-progress-wrapper'><div class='progress-bar-container small'><div class='progress-bar-fill' style='width:" + s.Progress + "%;'></div></div><span>" + s.Progress + "%</span></div></td>");
            sb.Append("<td><strong>" + (s.Score < 0 ? "—" : s.Score + "%") + "</strong></td>");
            sb.Append("<td><span class='badge " + badge + "'>" + s.Status.ToUpper() + "</span></td>");
            sb.Append("</tr>");
            return sb.ToString();
        }

        // "Maya Chen" -> "MC"
        private string MakeInitials(string fullName)
        {
            string[] words = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            if (words.Length == 1) return Server.HtmlEncode(words[0].Substring(0, 1).ToUpper());
            return Server.HtmlEncode((words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, 1)).ToUpper());
        }


        // =====================================================
        //  BUTTONS
        // =====================================================

        // Search typed / dropdown changed  ->  go back to page 1
        protected void Filter_Changed(object sender, EventArgs e)
        {
            CurrentPage = 0;
        }

        protected void btnPrev_Click(object sender, EventArgs e)
        {
            CurrentPage = CurrentPage - 1;
        }

        protected void btnNext_Click(object sender, EventArgs e)
        {
            CurrentPage = CurrentPage + 1;
        }

        // Quick Actions: "Export Class Report"
        // Makes a .csv file of the WHOLE class (Excel opens it) and sends it to the browser.
        protected void btnExport_Click(object sender, EventArgs e)
        {
            List<StudentRow> all = LoadStudents().OrderBy(s => s.Name).ToList();

            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Student Name,Course,Progress (%),Score (%),Status");
            foreach (StudentRow s in all)
            {
                csv.AppendLine(Csv(s.Name) + "," + Csv(s.Course) + "," + s.Progress + "," +
                               (s.Score < 0 ? "" : s.Score.ToString()) + "," + s.Status);
            }

            Response.Clear();
            Response.ContentType = "text/csv";
            Response.ContentEncoding = new UTF8Encoding(true);   // true = Excel reads special letters correctly
            Response.AddHeader("Content-Disposition", "attachment; filename=class-report-" + DateTime.Now.ToString("yyyy-MM-dd", Inv) + ".csv");
            Response.Write(csv.ToString());
            Response.End();
        }

        // Puts a text in quotes, so a comma inside the text does not break the .csv
        private string Csv(string text)
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }


        // =====================================================
        //  HELPER
        // =====================================================

        // Asks the database for ONE number
        private int GetNumber(string sql)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}