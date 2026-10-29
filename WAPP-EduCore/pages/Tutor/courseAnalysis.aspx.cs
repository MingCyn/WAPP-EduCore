using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Web.UI.WebControls;
using Npgsql;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class courseAnalysis : System.Web.UI.Page
    {
        private const string ConnectionName = "SupabasePostgres";
        private const string SessionUserKey = "UserID";
        private const string LoginPage = "~/auth/login.aspx";
        private const string CourseListPage = "courses.aspx";

        private int userId;
        private int courseId;

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

        private bool CourseHasAssessment(NpgsqlConnection conn, int cid)
        {
            const string sql =
                "SELECT EXISTS (" +
                "  SELECT 1 FROM public.lesson l " +
                "  WHERE l.course_id = @cid " +
                "  AND (l.quiz_id IS NOT NULL OR EXISTS (SELECT 1 FROM public.quiz q WHERE q.lesson_id = l.lesson_id))" +
                ")";
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("cid", cid);
                return Convert.ToBoolean(cmd.ExecuteScalar());
            }
        }
        public class StudentRowView
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string EnrolledAt { get; set; }
            public int ProgressPercent { get; set; }
            public string StatusText { get; set; }
            public string StatusClass { get; set; }
        }

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
            LnkCourseDetails.NavigateUrl = "courseDetails.aspx?courseId=" + courseId;

            try
            {
                LoadAnalyticsData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                ShowMessage("We couldn't load analytics for this course right now.", true);
            }
        }

        private void LoadAnalyticsData()
        {
            using (NpgsqlConnection conn = OpenConnection())
            {
                // 1. Verify course ownership & title
                string courseTitle = "";
                using (var cmd = new NpgsqlCommand("SELECT title FROM courses WHERE course_id = @cid AND user_id = @uid", conn))
                {
                    cmd.Parameters.AddWithValue("cid", courseId);
                    cmd.Parameters.AddWithValue("uid", userId);
                    object res = cmd.ExecuteScalar();
                    if (res == null || res == DBNull.Value)
                    {
                        Response.Redirect(CourseListPage);
                        return;
                    }
                    courseTitle = Convert.ToString(res);
                }

                Page.Title = "EduCore Analytics - " + courseTitle;
                LitCrumbTitle.Text = Server.HtmlEncode(courseTitle);
                LitTitle.Text = Server.HtmlEncode(courseTitle);

                bool hasAssessment = CourseHasAssessment(conn, courseId);
                PnlNoAssessment.Visible = !hasAssessment;
                PnlAnalytics.Visible = hasAssessment;
                if (!hasAssessment) return;

                // 2. Load Enrolled Students with progression
                var studentList = new List<StudentRowView>();
                int totalProgressSum = 0;
                int studentCount = 0;

                string studentQuery =
                    "SELECT u.full_name, u.email, e.enrolled_at, e.progression " +
                    "FROM enrollment e " +
                    "JOIN public.users u ON e.user_id = u.user_id " +
                    "WHERE e.course_id = @cid ORDER BY e.enrolled_at DESC";

                using (var cmd = new NpgsqlCommand(studentQuery, conn))
                {
                    cmd.Parameters.AddWithValue("cid", courseId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            studentCount++;
                            string name = r.IsDBNull(0) ? "Unknown Student" : r.GetString(0);
                            string email = r.IsDBNull(1) ? "" : r.GetString(1);
                            DateTime enrolledAt = r.IsDBNull(2) ? DateTime.Now : r.GetDateTime(2);
                            int progress = r.IsDBNull(3) ? 0 : Convert.ToInt32(r[3]);
                            progress = Math.Max(0, Math.Min(100, progress));   // keep the bar and the status in range

                            string statusText, statusClass;
                            GetProgressStatus(progress, out statusText, out statusClass);

                            totalProgressSum += progress;

                            studentList.Add(new StudentRowView
                            {
                                FullName = name,
                                Email = email,
                                EnrolledAt = enrolledAt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
                                ProgressPercent = progress,
                                StatusText = statusText,
                                StatusClass = statusClass
                            });
                        }
                    }
                }

                LitTotalEnrolled.Text = studentCount.ToString(CultureInfo.InvariantCulture);

                int avgProg = studentCount > 0 ? totalProgressSum / studentCount : 0;
                LitAvgProgress.Text = avgProg.ToString(CultureInfo.InvariantCulture) + "%";

                RptStudents.DataSource = studentList;
                RptStudents.DataBind();
                PnlStudentList.Visible = studentCount > 0;
                PnlNoStudents.Visible = studentCount == 0;

                // 3. Calculate Average Assessment Score (if an assessment/quiz attempt score table exists)
                // Falls back gracefully if scores aren't populated yet.
                double avgScore = GetAverageAssessmentScore(conn, courseId);
                LitAvgScore.Text = avgScore.ToString("0.#", CultureInfo.InvariantCulture) + "%";
            }
        }

        private double GetAverageAssessmentScore(NpgsqlConnection conn, int cid)
        {
            try
            {
                // Adjust this query name if your quiz attempt/score table differs (e.g. quiz_attempts or assessment_results)
                string query =
                    "SELECT AVG(qs.total_score::numeric) FROM public.quiz_score qs " +
                    "JOIN public.quiz q ON q.quiz_id = qs.quiz_id " +
                    "JOIN public.lesson l ON l.lesson_id = q.lesson_id " +
                    "WHERE l.course_id = @cid";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("cid", cid);
                    object res = cmd.ExecuteScalar();
                    if (res != null && res != DBNull.Value)
                    {
                        return Convert.ToDouble(res);
                    }
                }
            }
            catch
            {
                // Table might not be queried or structured this way yet, return 0 as safe fallback
            }
            return 0.0;
        }

        // <30% = At Risk, 30-50% = Needs Attention, >50% = On Track
        private static void GetProgressStatus(int progress, out string text, out string cssClass)
        {
            if (progress < 30) { text = "At Risk"; cssClass = "status-risk"; }
            else if (progress <= 50) { text = "Needs Attention"; cssClass = "status-attention"; }
            else { text = "On Track"; cssClass = "status-on-track"; }
        }

        private void ShowMessage(string message, bool isError)
        {
            LblStatus.Text = Server.HtmlEncode(message);
            LblStatus.CssClass = isError ? "page-status error" : "page-status";
            LblStatus.Visible = true;
        }
    }
}