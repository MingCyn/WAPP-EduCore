using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace WAPP_EduCore.pages.Student
{
    public partial class courseOverview : System.Web.UI.Page
    {
        private long UserId
        {
            get { return Convert.ToInt64(Session["UserID"]); }
        }

        private bool TryGetCourseId(out long courseId)
        {
            return long.TryParse(Request.QueryString["id"], out courseId);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }

            long courseId;
            if (!TryGetCourseId(out courseId))
            {
                Response.Redirect("~/pages/Student/myCourses.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadPage(courseId);
            }
        }

        private void LoadPage(long courseId)
        {
            // ---------- course ----------
            string courseSql =
                "SELECT c.course_id, c.title, c.course_description, c.duration, c.course_level, c.course_img, " +
                "  COALESCE(u.full_name, 'TBA') AS instructor, " +
                "  (SELECT COUNT(*) FROM enrollment e WHERE e.course_id = c.course_id) AS student_count, " +
                "  EXISTS (SELECT 1 FROM enrollment e2 WHERE e2.course_id = c.course_id AND e2.user_id = @uid) AS is_enrolled " +
                "FROM courses c " +
                "LEFT JOIN users u ON u.user_id = c.user_id " +
                "WHERE c.course_id = @cid";

            DataTable course = Db.Query(courseSql, new Dictionary<string, object>
            {
                { "@uid", UserId },
                { "@cid", courseId }
            });

            if (course.Rows.Count == 0)
            {
                Response.Redirect("~/pages/Student/myCourses.aspx");
                return;
            }

            DataRow row = course.Rows[0];
            string title = Convert.ToString(row["title"]);
            string instructor = Convert.ToString(row["instructor"]);
            bool enrolled = Convert.ToBoolean(row["is_enrolled"]);
            string duration = Convert.ToString(row["duration"]);
            string level = Convert.ToString(row["course_level"]);

            Page.Title = title + " - EduCore";
            litTopTitle.Text = title;
            litTitle.Text = title;
            litLevel.Text = string.IsNullOrWhiteSpace(level) ? "Course" : level;
            litInstructor.Text = instructor;
            litInitial.Text = instructor.Length > 0 ? instructor.Substring(0, 1).ToUpperInvariant() : "?";
            litStudents.Text = Convert.ToString(row["student_count"]);
            litDuration.Text = string.IsNullOrWhiteSpace(duration) ? "To be announced" : duration;

            string heroStyle = HeroStyle(row["course_img"]);
            if (heroStyle.Length > 0) heroBox.Attributes["style"] = heroStyle;

            // What You'll Learn: one tick per line of course_description
            string desc = Convert.ToString(row["course_description"]);
            List<string> points = desc
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
            rptLearn.DataSource = points;
            rptLearn.DataBind();
            pnlLearn.Visible = points.Count > 0;

            // enrollment status card
            litStatus.Text = enrolled ? "You are Enrolled" : "Not Enrolled";
            phEnrolled.Visible = enrolled;
            phNotEnrolled.Visible = !enrolled;

            // ---------- lessons ----------
            DataTable lessons = Db.Query(
                "SELECT l.lesson_id, l.lesson_title, l.lesson_description, " +
                "  COALESCE(lp.is_completed, false) AS done " +
                "FROM lesson l " +
                "LEFT JOIN lesson_progress lp ON lp.lesson_id = l.lesson_id AND lp.user_id = @uid " +
                "WHERE l.course_id = @cid " +
                "ORDER BY l.lesson_id",
                new Dictionary<string, object> { { "@uid", UserId }, { "@cid", courseId } });

            rptLessons.DataSource = lessons;
            rptLessons.DataBind();
            pnlNoLessons.Visible = lessons.Rows.Count == 0;
            litLessonCount.Text = lessons.Rows.Count.ToString();

            lnkStart.NavigateUrl = "~/pages/Student/learningProgression.aspx?course=" + courseId;
        }

        protected void btnEnroll_Click(object sender, EventArgs e)
        {
            long courseId;
            if (!TryGetCourseId(out courseId)) return;

            Db.Execute(
                "INSERT INTO enrollment (user_id, course_id) VALUES (@uid, @cid) " +
                "ON CONFLICT (user_id, course_id) DO NOTHING",
                new Dictionary<string, object> { { "@uid", UserId }, { "@cid", courseId } });

            lblMessage.Text = "You are now enrolled.";
            lblMessage.Visible = true;
            LoadPage(courseId);
        }

        // Dark overlay on top of the course image, like the Figma hero
        private string HeroStyle(object img)
        {
            string url = Convert.ToString(img);
            if (string.IsNullOrWhiteSpace(url)) return "";

            url = url.Trim();
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = ResolveUrl("~/" + url.TrimStart('/', '~'));

            url = url.Replace("'", "%27").Replace("(", "%28").Replace(")", "%29");
            return "background:linear-gradient(rgba(15,26,70,.78),rgba(15,26,70,.78)),url('" + url + "') center/cover";
        }
    }
}