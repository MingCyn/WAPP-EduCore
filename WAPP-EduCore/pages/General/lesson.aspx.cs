using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using Newtonsoft.Json.Linq; // Requires Newtonsoft.Json NuGet package

namespace WAPP_EduCore.pages.General
{
    public partial class lesson : System.Web.UI.Page
    {
        protected int currentLessonId = 0;
        
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!int.TryParse(Request.QueryString["id"], out currentLessonId))
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

            string lessonTitle = Str(row["lesson_title"]);

            litLessonTitle.Text = Enc(lessonTitle);
            litCourseTitle.Text = Enc(Str(row["course_title"]));

            // Render JSONB content block array dynamically
            string rawJsonContent = Str(row["lesson_content"]);
            litLessonContent.Text = RenderJsonbContent(rawJsonContent);

            // 3. Display instructor information
            string tutorName = Str(row["instructor_name"]);

            if (tutorName != "")
            {
                litTutorName.Text = Enc(tutorName);
                litTutorInitial.Text = Enc(tutorName.Substring(0, 1).ToUpper());
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

        private string RenderJsonbContent(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return "<p class=\"lesson-block-paragraph\">No content available for this lesson.</p>";
            }

            try
            {
                JArray blocks = JArray.Parse(jsonContent);
                StringBuilder sb = new StringBuilder();

                foreach (JToken block in blocks)
                {
                    string type = Str(block["type"]);

                    switch (type.ToLower())
                    {
                        case "paragraph":
                            string paragraphText = Str(block["content"]);
                            sb.AppendFormat("<p class=\"lesson-block-paragraph\">{0}</p>", HttpUtility.HtmlEncode(paragraphText));
                            break;

                        case "subheading":
                            string subText = Str(block["text"]);
                            sb.AppendFormat("<h3 class=\"lesson-block-subheading\">{0}</h3>", HttpUtility.HtmlEncode(subText));
                            break;

                        case "list":
                            sb.Append("<ul class=\"lesson-block-list\">");
                            JArray items = block["items"] as JArray;
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    sb.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(item.ToString()));
                                }
                            }
                            sb.Append("</ul>");
                            break;

                        case "video":
                            string vUrl = Str(block["url"]);
                            string vCaption = Str(block["caption"]);

                            // Simple normalization for YouTube watch links if needed
                            if (vUrl.Contains("watch?v="))
                            {
                                vUrl = vUrl.Replace("watch?v=", "embed/");
                            }

                            // Applies the lesson-block-video style container as requested
                            sb.Append("<div class=\"lesson-block-video\">");
                            sb.AppendFormat("<iframe src=\"{0}\" style=\"width:100%; height:100%; border:0;\" allowfullscreen></iframe>", HttpUtility.HtmlAttributeEncode(vUrl));
                            sb.Append("</div>");

                            if (!string.IsNullOrEmpty(vCaption))
                            {
                                sb.AppendFormat("<div class=\"lesson-block-caption\" style=\"text-align:center;\">{0}</div>", HttpUtility.HtmlEncode(vCaption));
                            }
                            break;

                        case "image":
                            string imgUrl = Str(block["url"]);
                            string imgCaption = Str(block["caption"]);

                            // Extract file name from JSON url to map to physical folder:
                            // C:\Users\SinYi\source\repos\WAPP-EduCore\WAPP-EduCore\images\
                            string fileName = Path.GetFileName(imgUrl);
                            string resolvedWebPath = "/images/" + fileName;

                            sb.Append("<div class=\"lesson-block-image-container\">");
                            sb.AppendFormat("<img src=\"{0}\" alt=\"{1}\" class=\"lesson-block-image\" />",
                                HttpUtility.HtmlAttributeEncode(resolvedWebPath),
                                HttpUtility.HtmlAttributeEncode(imgCaption));

                            if (!string.IsNullOrEmpty(imgCaption))
                            {
                                sb.AppendFormat("<div class=\"lesson-block-caption\">{0}</div>", HttpUtility.HtmlEncode(imgCaption));
                            }
                            sb.Append("</div>");
                            break;
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"<p style=\"color: red;\">Error parsing lesson content structure: {HttpUtility.HtmlEncode(ex.Message)}</p>";
            }
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
                pnlLessonQuiz.Visible = true;
                string quizId = Convert.ToString(quiz.Rows[0]["quiz_id"]);
                lnkAttemptQuiz.NavigateUrl = "quiz.aspx?quiz_id=" + Server.UrlEncode(quizId);
            }
            else
            {
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
            return HttpUtility.HtmlEncode(Convert.ToString(o) ?? "");
        }
    }
}