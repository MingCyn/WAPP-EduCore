using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Script.Serialization;
using System.Web;
using System.Web.UI;
using WAPP_EduCore;

namespace WAPP_EduCore.pages.General
{
    public partial class courseOverview : Page
    {
        protected string LoginUrl
        {
            get
            {
                return ResolveUrl("~/login.aspx") + "?returnUrl=" + HttpUtility.UrlEncode(Request.Url.PathAndQuery);
            }
        }

        protected string StartLessonUrl { get; set; } = "coursesDirectory.aspx";

        protected void Page_Load(object sender, EventArgs e)
        {
            int courseId;
            if (!int.TryParse(Request.QueryString["id"], out courseId))
            {
                Response.Redirect("coursesDirectory.aspx");
                return;
            }

            DataTable course = Db.Query(@"
            SELECT c.course_id, c.title, c.category, c.course_description, c.duration,
                   c.course_img, c.course_level, u.full_name AS instructor
            FROM courses c
            LEFT JOIN users u ON u.user_id = c.user_id
            WHERE c.course_id = @id",
                new Dictionary<string, object> { { "@id", courseId } });

            if (course.Rows.Count == 0)
            {
                Response.StatusCode = 404;
                pnlCourse.Visible = false;
                pnlMissing.Visible = true;
                litTitle.Text = "Course not found";
                litHeading.Text = "Course not found";
                return;
            }

            DataRow c = course.Rows[0];
            string title = Str(c["title"]);

            // Header + hero
            litTitle.Text = Enc(title);
            litHeading.Text = Enc(title);
            litCourseTitle.Text = Enc(title);
            litLevelBadge.Text = Enc(Str(c["course_level"]).ToUpperInvariant());
            litCategoryBadge.Text = Enc(Str(c["category"]).ToUpperInvariant());
            litInstructor.Text = Enc(Str(c["instructor"]) == "" ? "EduCore Instructor" : Str(c["instructor"]));

            string img = Str(c["course_img"]);
            if (img != "")
            {
                // If the database just stores "bio1.jpeg" or "images/bio1.jpeg", normalize it to the root images folder
                if (!img.StartsWith("~/") && !img.StartsWith("/"))
                {
                    if (img.StartsWith("images/"))
                    {
                        img = "~/" + img;
                    }
                    else
                    {
                        img = "~/images/" + img;
                    }
                }

                // Resolve the virtual path to a clean relative/absolute URL for the browser
                img = ResolveUrl(img);
                img = img.Replace("'", "%27").Replace("\"", "%22");

                hero.Attributes["style"] = "background-image:url('" + img + "')";
            }

            // MOVED OUTSIDE: Always find the first lesson for this course so the button works reliably
            DataTable firstLesson = Db.Query(@"
            SELECT lesson_id FROM lesson 
            WHERE course_id = @id 
            ORDER BY lesson_id 
            LIMIT 1",
            new Dictionary<string, object> { { "@id", courseId } });

            if (firstLesson.Rows.Count > 0)
            {
                int firstLessonId = Convert.ToInt32(firstLesson.Rows[0]["lesson_id"]);
                StartLessonUrl = ResolveUrl($"~/pages/General/lesson.aspx?id={firstLessonId}");
            }

            // About
            string desc = Str(c["course_description"]);
            pnlAbout.Visible = desc != "";
            litDescription.Text = Enc(desc);

            // Lessons
            DataTable lessons = Db.Query(@"
            SELECT l.lesson_id, l.lesson_title, l.lesson_description, l.lesson_content::text, l.quiz_id,
                   (SELECT COUNT(*) FROM quiz q WHERE q.lesson_id = l.lesson_id) AS quiz_count
            FROM lesson l
            WHERE l.course_id = @id
            ORDER BY l.lesson_id",
                new Dictionary<string, object> { { "@id", courseId } });

            int quizzes = 0;
            var learn = new List<string>();
            foreach (DataRow r in lessons.Rows)
            {
                if (HasQuiz(r)) quizzes++;

                if (learn.Count < 4)
                {
                    string text = Str(r["lesson_description"]);
                    if (text == "") text = Str(r["lesson_title"]);
                    if (text.Length > 140) text = text.Substring(0, 137).TrimEnd() + "...";
                    if (text != "") learn.Add(text);
                }
            }

            rptLearn.DataSource = learn;
            rptLearn.DataBind();
            pnlLearn.Visible = learn.Count > 0;

            rptLessons.DataSource = lessons;
            rptLessons.DataBind();
            pnlNoLessons.Visible = lessons.Rows.Count == 0;

            // Side panel
            string lessonText = lessons.Rows.Count + (lessons.Rows.Count == 1 ? " lesson" : " lessons");
            litLessonCount.Text = lessonText;
            litLessonsSide.Text = lessons.Rows.Count.ToString();
            litQuizzes.Text = quizzes.ToString();
            litDuration.Text = Enc(Str(c["duration"]) == "" ? "Self-paced" : Str(c["duration"]));
        }

        // ---------- Helpers ----------

        private static string Str(object o)
        {
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }

        protected static string Enc(object o)
        {
            return HttpUtility.HtmlEncode(Convert.ToString(o) ?? "");
        }

        protected static bool HasValue(object o)
        {
            return Str(o) != "";
        }

        protected static bool HasVideo(object dataItem)
        {
            DataRow row = dataItem as DataRow;
            if (row == null)
            {
                DataRowView view = dataItem as DataRowView;
                if (view == null) return false;
                row = view.Row;
            }

            try
            {
                var blocks = new JavaScriptSerializer().Deserialize<List<Dictionary<string, object>>>(Str(row["lesson_content"]));
                if (blocks == null) return false;
                foreach (var block in blocks)
                {
                    object type;
                    object url;
                    if (block != null && block.TryGetValue("type", out type) &&
                        string.Equals(Convert.ToString(type), "video", StringComparison.OrdinalIgnoreCase) &&
                        block.TryGetValue("url", out url) && Str(url) != "")
                        return true;
                }
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            return false;
        }

        protected static bool HasQuiz(object dataItem)
        {
            DataRow row = dataItem as DataRow;
            if (row == null)
            {
                DataRowView view = dataItem as DataRowView;
                if (view == null) return false;
                row = view.Row;
            }
            if (row["quiz_id"] != DBNull.Value) return true;
            return row["quiz_count"] != DBNull.Value && Convert.ToInt32(row["quiz_count"]) > 0;
        }
    }
}