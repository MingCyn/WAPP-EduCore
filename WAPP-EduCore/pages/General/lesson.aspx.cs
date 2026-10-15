using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.UI;

namespace WAPP_EduCore.pages.General
{
    public partial class lesson : System.Web.UI.Page
    {
        protected int currentLessonId = 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!int.TryParse(
                    Request.QueryString["id"],
                    out currentLessonId))
                {
                    Response.Redirect("coursesDirectory.aspx");
                    return;
                }

                LoadLessonData(currentLessonId);
            }
        }

        private void LoadLessonData(int lessonId)
        {
            // 1. Load lesson and course information
            DataTable dtLesson = Db.Query(@"
                SELECT l.lesson_id,
                       l.lesson_title,
                       l.lesson_content,
                       l.video_url,
                       l.course_id,
                       c.title AS course_title,
                       u.full_name AS instructor_name
                FROM lesson l
                JOIN courses c ON l.course_id = c.course_id
                LEFT JOIN users u ON c.user_id = u.user_id
                WHERE l.lesson_id = @id",
                new Dictionary<string, object>
                {
                    { "@id", lessonId }
                });

            if (dtLesson.Rows.Count == 0)
            {
                Response.Redirect("coursesDirectory.aspx");
                return;
            }

            DataRow row = dtLesson.Rows[0];

            int courseId = Convert.ToInt32(row["course_id"]);

            // 2. Display lesson information
            string lessonTitle = Str(row["lesson_title"]);

            litLessonTitle.Text = Enc(lessonTitle);
            litCourseTitle.Text = Enc(Str(row["course_title"]));
            litPlayerTitle.Text = Enc(lessonTitle);

            litLessonContent.Text = Str(row["lesson_content"]);

            // 3. Display instructor information
            string tutorName = Str(row["instructor_name"]);

            if (tutorName != "")
            {
                litTutorName.Text = Enc(tutorName);
                litTutorInitial.Text =
                    Enc(tutorName.Substring(0, 1).ToUpper());
            }

            // 4. Handle lesson video
            string videoUrl = Str(row["video_url"]);

            if (videoUrl != "")
            {
                pnlVideoEmbed.Visible = true;
                pnlVideoPlaceholder.Visible = false;
                iframeVideo.Attributes["src"] = videoUrl;
            }
            else
            {
                pnlVideoEmbed.Visible = false;
                pnlVideoPlaceholder.Visible = true;
            }

            // 5. Load course syllabus
            DataTable dtSyllabus = Db.Query(@"
                SELECT lesson_id, lesson_title
                FROM lesson
                WHERE course_id = @courseId
                ORDER BY lesson_id",
                new Dictionary<string, object>
                {
                    { "@courseId", courseId }
                });

            rptSyllabus.DataSource = dtSyllabus;
            rptSyllabus.DataBind();

            // 6. Check whether this lesson has a quiz
            LoadLessonQuiz(lessonId);
        }

        private void LoadLessonQuiz(int lessonId)
        {
            DataTable quiz = Db.Query(@"
                SELECT quiz_id
                FROM quiz
                WHERE lesson_id = @lesson_id
                LIMIT 1",
                new Dictionary<string, object>
                {
                    { "@lesson_id", lessonId }
                });

            if (quiz.Rows.Count > 0)
            {
                // Show the Attempt Quiz button
                pnlLessonQuiz.Visible = true;

                string quizId =
                    Convert.ToString(quiz.Rows[0]["quiz_id"]);

                lnkAttemptQuiz.NavigateUrl =
                    "quiz.aspx?quiz_id=" +
                    Server.UrlEncode(quizId);
            }
            else
            {
                // Hide the button if no quiz exists
                pnlLessonQuiz.Visible = false;
            }
        }

        protected bool IsCurrentLesson(object idObj)
        {
            if (idObj == null || idObj == DBNull.Value)
                return false;

            return Convert.ToInt32(idObj) == currentLessonId;
        }

        protected static string Str(object o)
        {
            return o == null || o == DBNull.Value
                ? ""
                : Convert.ToString(o).Trim();
        }

        protected static string Enc(object o)
        {
            return HttpUtility.HtmlEncode(
                Convert.ToString(o) ?? "");
        }
    }
}