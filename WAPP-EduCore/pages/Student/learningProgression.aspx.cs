using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace WAPP_EduCore.pages.Student
{
    public partial class WebForm6 : System.Web.UI.Page
    {
        private long _courseId;
        private long _lessonId;
        private int _index;
        private DataTable _lessons;
        private DataRow _course;

        private long UserId { get { return Convert.ToInt64(Session["UserID"]); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }

            if (!long.TryParse(Request.QueryString["course"], out _courseId))
            {
                Response.Redirect("~/pages/Student/myCourses.aspx");
                return;
            }

            // course, tutor and enrollment check
            DataTable ct = Db.Query(
                "SELECT c.course_id, c.title, c.category, COALESCE(u.full_name, 'TBA') AS tutor, " +
                "  EXISTS (SELECT 1 FROM enrollment e WHERE e.course_id = c.course_id AND e.user_id = @uid) AS is_enrolled " +
                "FROM courses c LEFT JOIN users u ON u.user_id = c.user_id " +
                "WHERE c.course_id = @cid",
                new Dictionary<string, object> { { "@uid", UserId }, { "@cid", _courseId } });

            if (ct.Rows.Count == 0)
            {
                Response.Redirect("~/pages/Student/myCourses.aspx");
                return;
            }
            _course = ct.Rows[0];

            if (!Convert.ToBoolean(_course["is_enrolled"]))
            {
                Response.Redirect("~/pages/Student/courseOverview.aspx?id=" + _courseId);
                return;
            }

            // all lessons of the course, with this student's completion
            _lessons = Db.Query(
                "SELECT l.lesson_id, l.lesson_title, l.lesson_description, l.lesson_content, l.video_url, l.quiz_id, " +
                "  COALESCE(lp.is_completed, false) AS done " +
                "FROM lesson l " +
                "LEFT JOIN lesson_progress lp ON lp.lesson_id = l.lesson_id AND lp.user_id = @uid " +
                "WHERE l.course_id = @cid ORDER BY l.lesson_id",
                new Dictionary<string, object> { { "@uid", UserId }, { "@cid", _courseId } });

            string courseTitle = Convert.ToString(_course["title"]);
            litCourseTitle.Text = courseTitle;
            Page.Title = courseTitle + " - EduCore";

            if (_lessons.Rows.Count == 0)
            {
                pnlViewer.Visible = false;
                chipBox.Visible = false;
                pnlEmpty.Visible = true;
                litPct.Text = "0";
                return;
            }

            // which lesson to show
            bool found = false;
            long requested;
            if (long.TryParse(Request.QueryString["lesson"], out requested))
            {
                for (int i = 0; i < _lessons.Rows.Count; i++)
                {
                    if (Convert.ToInt64(_lessons.Rows[i]["lesson_id"]) == requested)
                    {
                        _index = i;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                // default: first unfinished lesson, else the first one
                _index = 0;
                for (int i = 0; i < _lessons.Rows.Count; i++)
                {
                    if (!Convert.ToBoolean(_lessons.Rows[i]["done"])) { _index = i; break; }
                }

                if (!IsPostBack)
                {
                    Response.Redirect(LessonUrl(Convert.ToInt64(_lessons.Rows[_index]["lesson_id"])));
                    return;
                }
            }

            _lessonId = Convert.ToInt64(_lessons.Rows[_index]["lesson_id"]);

            if (!IsPostBack) Render();
        }

        // ---------- showing the lesson ----------
        private void Render()
        {
            DataRow cur = _lessons.Rows[_index];
            int total = _lessons.Rows.Count;

            int done = 0;
            foreach (DataRow r in _lessons.Rows)
                if (Convert.ToBoolean(r["done"])) done++;
            int pct = (int)(done * 100L / total);

            litLessonNo.Text = (_index + 1).ToString();
            litLessonTotal.Text = total.ToString();
            litPct.Text = pct.ToString();
            litDoneCount.Text = done.ToString();
            litTotalCount.Text = total.ToString();
            barFill.Attributes["style"] = "width:" + pct + "%";

            litLessonTitle.Text = Convert.ToString(cur["lesson_title"]);
            litLessonDesc.Text = Convert.ToString(cur["lesson_description"]);

            string content = Convert.ToString(cur["lesson_content"]);
            if (string.IsNullOrWhiteSpace(content))
            {
                pnlContent.Visible = false;
            }
            else
            {
                litContent.Text = HttpUtility.HtmlEncode(content)
                    .Replace("\r\n", "<br />").Replace("\n", "<br />");
            }

            string video = BuildVideoHtml(Convert.ToString(cur["video_url"]));
            litVideo.Text = video.Length > 0
                ? video
                : "<div class=\"lv-novideo\">No video for this lesson yet.</div>";

            // buttons
            bool isDone = Convert.ToBoolean(cur["done"]);
            btnComplete.Visible = !isDone;
            doneBox.Visible = isDone;

            lnkPrev.Visible = _index > 0;
            if (lnkPrev.Visible)
                lnkPrev.NavigateUrl = LessonUrl(Convert.ToInt64(_lessons.Rows[_index - 1]["lesson_id"]));

            lnkNext.Visible = _index < total - 1;
            if (lnkNext.Visible)
                lnkNext.NavigateUrl = LessonUrl(Convert.ToInt64(_lessons.Rows[_index + 1]["lesson_id"]));

            // quiz of this lesson (works whichever table holds the link)
            long lessonQuizId = cur["quiz_id"] == DBNull.Value ? -1 : Convert.ToInt64(cur["quiz_id"]);
            DataTable qz = Db.Query(
                "SELECT quiz_id FROM quiz WHERE lesson_id = @lid OR quiz_id = @qid ORDER BY quiz_id LIMIT 1",
                new Dictionary<string, object> { { "@lid", _lessonId }, { "@qid", lessonQuizId } });
            lnkQuiz.Visible = qz.Rows.Count > 0;
            if (lnkQuiz.Visible)
                lnkQuiz.NavigateUrl = "~/pages/Student/assessment.aspx?quiz=" + Convert.ToInt64(qz.Rows[0]["quiz_id"]);

            // syllabus list
            rptSyllabus.DataSource = _lessons;
            rptSyllabus.DataBind();

            // tutor card
            string tutor = Convert.ToString(_course["tutor"]);
            string category = Convert.ToString(_course["category"]);
            litTutorName.Text = tutor;
            litTutorInitial.Text = tutor.Length > 0 ? tutor.Substring(0, 1).ToUpperInvariant() : "?";
            litTutorRole.Text = string.IsNullOrWhiteSpace(category) ? "Tutor" : "Tutor \u00B7 " + category;

            lnkDiscuss.NavigateUrl = "~/pages/Student/messages.aspx?lesson=" + _lessonId + "&tab=lesson";
        }

        // ---------- mark as complete ----------
        protected void btnComplete_Click(object sender, EventArgs e)
        {
            Db.Execute(
                "INSERT INTO lesson_progress (user_id, lesson_id, is_completed, completed_at) " +
                "VALUES (@uid, @lid, true, now()) " +
                "ON CONFLICT (user_id, lesson_id) DO UPDATE SET is_completed = true, completed_at = now()",
                new Dictionary<string, object> { { "@uid", UserId }, { "@lid", _lessonId } });

            // keep enrollment.progression in step with the lessons completed
            int done = 0;
            foreach (DataRow r in _lessons.Rows)
                if (Convert.ToBoolean(r["done"])) done++;
            if (!Convert.ToBoolean(_lessons.Rows[_index]["done"])) done++;
            long pct = done * 100L / _lessons.Rows.Count;

            Db.Execute(
                "UPDATE enrollment SET progression = @p WHERE user_id = @uid AND course_id = @cid",
                new Dictionary<string, object> { { "@p", pct }, { "@uid", UserId }, { "@cid", _courseId } });

            // reload the same lesson so that refreshing cannot repeat the save
            Response.Redirect(LessonUrl(_lessonId));
        }

        // ---------- helpers ----------
        private string LessonUrl(long lessonId)
        {
            return "~/pages/Student/learningProgression.aspx?course=" + _courseId + "&lesson=" + lessonId;
        }

        protected string LessonHref(object lessonId)
        {
            return ResolveUrl(LessonUrl(Convert.ToInt64(lessonId)));
        }

        protected string ItemCss(object lessonId, object done)
        {
            string css = "lv-item";
            if (Convert.ToInt64(lessonId) == _lessonId) css += " current";
            if (Convert.ToBoolean(done)) css += " done";
            return css;
        }

        // YouTube link -> embedded player. Only the 11-character video ID is used in the page.
        private string BuildVideoHtml(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            url = url.Trim();

            Match m = Regex.Match(url,
                @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})");
            if (m.Success)
            {
                return "<div class=\"lv-video\"><iframe src=\"https://www.youtube.com/embed/" + m.Groups[1].Value +
                       "\" title=\"Lesson video\" allow=\"accelerometer; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe></div>";
            }

            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                url.ToLowerInvariant().Contains(".mp4"))
            {
                return "<div class=\"lv-video\"><video controls src=\"" + HttpUtility.HtmlAttributeEncode(url) + "\"></video></div>";
            }

            return "";
        }
    }
}