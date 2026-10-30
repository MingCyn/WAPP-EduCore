using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;

namespace WAPP_EduCore.pages.Student
{
    public partial class SavedNotes : System.Web.UI.Page
    {
        private long _userId;
        public long _selectedCourseId;

        protected void Page_Load(object sender, EventArgs e)
        {
            long uid;
            if (Session["UserID"] == null || !long.TryParse(Convert.ToString(Session["UserID"]), out uid))
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }
            _userId = uid;

            if (!IsPostBack)
            {
                string qs = Request.QueryString["course"];
                if (long.TryParse(qs, out _selectedCourseId) && _selectedCourseId > 0)
                {
                    BindNotesForCourse(_selectedCourseId);
                }
                else
                {
                    BindCourseCards();
                }
            }
        }

        // Binds course cards with course_img fetched from the courses table
        private void BindCourseCards()
        {
            DataTable dt = Db.Query(
                "SELECT nb.course_id, nb.notebook_title, c.course_img, " +
                "(SELECT COUNT(*) FROM student_notes n WHERE n.notebook_id = nb.notebook_id) as note_count " +
                "FROM course_notebooks nb " +
                "LEFT JOIN courses c ON c.course_id = nb.course_id " +
                "WHERE nb.user_id = @uid " +
                "AND EXISTS (SELECT 1 FROM student_notes n WHERE n.notebook_id = nb.notebook_id) " +
                "ORDER BY nb.notebook_title",
                new Dictionary<string, object> { { "@uid", _userId } });

            if (dt.Rows.Count > 0)
            {
                rptCourses.DataSource = dt;
                rptCourses.DataBind();
                pnlCoursesList.Visible = true;
                pnlNotesList.Visible = false;
                pnlEmpty.Visible = false;
                litTopTitle.Text = "📚 Course Study Notebooks";
            }
            else
            {
                pnlCoursesList.Visible = false;
                pnlNotesList.Visible = false;
                pnlEmpty.Visible = true;
                litTopTitle.Text = "📚 Course Study Notebooks";
            }
        }

        // Resolves course image paths (handles ~/, absolute paths, or https links)
        protected string ResolveCourseImage(object imgPathObj)
        {
            string img = Convert.ToString(imgPathObj);
            if (string.IsNullOrEmpty(img))
            {
                return ResolveUrl("~/Content/images/default-course.png");
            }

            // External URL (e.g. picsum.photos)
            if (img.StartsWith("http://") || img.StartsWith("https://"))
            {
                return img;
            }

            // Path starting with ~ (e.g. ~/uploads/courses/...)
            if (img.StartsWith("~"))
            {
                return ResolveUrl(img);
            }

            // Path starting with /
            if (img.StartsWith("/"))
            {
                return ResolveUrl("~" + img);
            }

            return ResolveUrl("~/" + img);
        }

        private void BindNotesForCourse(long courseId)
        {
            var p = new Dictionary<string, object> { { "@uid", _userId }, { "@cid", courseId } };
            string sql =
                "SELECT n.note_id, n.highlighted_text, n.ai_summary, n.created_at, " +
                "       nb.course_id, nb.notebook_title, l.lesson_id, l.lesson_title " +
                "FROM student_notes n " +
                "JOIN course_notebooks nb ON nb.notebook_id = n.notebook_id " +
                "JOIN lesson l ON l.lesson_id = n.lesson_id " +
                "WHERE n.user_id = @uid AND nb.course_id = @cid " +
                "ORDER BY n.created_at DESC";

            DataTable dt = Db.Query(sql, p);

            if (dt.Rows.Count > 0)
            {
                rptNotes.DataSource = dt;
                rptNotes.DataBind();
                litCount.Text = dt.Rows.Count + (dt.Rows.Count == 1 ? " highlighted note" : " highlighted notes");
                lblNotebookTitle.InnerText = Convert.ToString(dt.Rows[0]["notebook_title"]) + " Notebook";
                litTopTitle.Text = "📖 " + Convert.ToString(dt.Rows[0]["notebook_title"]);

                pnlCoursesList.Visible = false;
                pnlNotesList.Visible = true;
                pnlEmpty.Visible = false;
            }
            else
            {
                pnlCoursesList.Visible = false;
                pnlNotesList.Visible = false;
                pnlEmpty.Visible = true;
            }
        }

        protected void rptNotes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "delete") return;

            long noteId;
            if (long.TryParse(Convert.ToString(e.CommandArgument), out noteId))
            {
                Db.Execute(
                    "DELETE FROM student_notes WHERE note_id = @nid AND user_id = @uid",
                    new Dictionary<string, object> { { "@nid", noteId }, { "@uid", _userId } });
            }

            string qs = Request.QueryString["course"];
            if (long.TryParse(qs, out _selectedCourseId))
            {
                BindNotesForCourse(_selectedCourseId);
            }
        }

        protected string LessonLink(object courseId, object lessonId)
        {
            string url = ResolveUrl("~/pages/Student/learningProgression.aspx?course=" +
                                    Convert.ToInt64(courseId) + "&lesson=" + Convert.ToInt64(lessonId));
            return HttpUtility.HtmlAttributeEncode(url);
        }

        protected string FormatNote(object raw)
        {
            string text = HttpUtility.HtmlEncode(Convert.ToString(raw) ?? "").Replace("\r", "");
            var sb = new StringBuilder();
            bool inList = false;

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.StartsWith("* ") || line.StartsWith("- "))
                {
                    if (!inList) { sb.Append("<ul>"); inList = true; }
                    sb.Append("<li>").Append(Inline(line.Substring(2))).Append("</li>");
                    continue;
                }
                if (inList) { sb.Append("</ul>"); inList = false; }
                if (line.Length == 0) continue;

                if (line.StartsWith("#"))
                    sb.Append("<h4>").Append(Inline(line.TrimStart('#', ' '))).Append("</h4>");
                else
                    sb.Append("<p>").Append(Inline(line)).Append("</p>");
            }
            if (inList) sb.Append("</ul>");
            return sb.ToString();
        }

        private static string Inline(string s)
        {
            return Regex.Replace(s, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        }
    }
}