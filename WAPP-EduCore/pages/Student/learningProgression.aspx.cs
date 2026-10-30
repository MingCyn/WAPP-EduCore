using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.Script.Serialization;

namespace WAPP_EduCore.pages.Student
{
    public partial class WebForm6 : System.Web.UI.Page
    {
        public long _courseId;
        public long _lessonId;
        private int _index;
        private DataTable _lessons;
        private DataRow _course;

        private bool IsGuest { get { return Session["UserID"] == null; } }
        private long UserId { get { return IsGuest ? 0 : Convert.ToInt64(Session["UserID"]); } }

        // Session-based temporary completion storage for guests
        private HashSet<long> GuestCompletedLessons
        {
            get
            {
                if (Session["GuestCompleted_" + _courseId] == null)
                {
                    Session["GuestCompleted_" + _courseId] = new HashSet<long>();
                }
                return (HashSet<long>)Session["GuestCompleted_" + _courseId];
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!long.TryParse(Request.QueryString["course"], out _courseId))
            {
                Response.Redirect("~/pages/Student/myCourses.aspx");
                return;
            }

            // Course and tutor info (enrollment check only applies to logged-in students)
            string courseQuery = IsGuest
                ? "SELECT c.course_id, c.title, c.category, COALESCE(u.full_name, 'TBA') AS tutor FROM courses c LEFT JOIN users u ON u.user_id = c.user_id WHERE c.course_id = @cid"
                : "SELECT c.course_id, c.title, c.category, COALESCE(u.full_name, 'TBA') AS tutor, EXISTS (SELECT 1 FROM enrollment e WHERE e.course_id = c.course_id AND e.user_id = @uid) AS is_enrolled FROM courses c LEFT JOIN users u ON u.user_id = c.user_id WHERE c.course_id = @cid";

            var parameters = new Dictionary<string, object> { { "@cid", _courseId } };
            if (!IsGuest) parameters.Add("@uid", UserId);

            DataTable ct = Db.Query(courseQuery, parameters);

            if (ct.Rows.Count == 0)
            {
                Response.Redirect("~/pages/Student/myCourses.aspx");
                return;
            }
            _course = ct.Rows[0];

            // If logged in and not enrolled, redirect to course overview to prompt enrollment
            if (!IsGuest && !Convert.ToBoolean(_course["is_enrolled"]))
            {
                Response.Redirect("~/pages/Student/courseOverview.aspx?id=" + _courseId);
                return;
            }

            // Fetch lessons and merge with guest session progress if guest
            string lessonQuery = IsGuest
                ? "SELECT l.lesson_id, l.lesson_title, l.lesson_description, l.lesson_content, l.quiz_id, false AS done FROM lesson l WHERE l.course_id = @cid ORDER BY l.lesson_id"
                : "SELECT l.lesson_id, l.lesson_title, l.lesson_description, l.lesson_content, l.quiz_id, COALESCE(lp.is_completed, false) AS done FROM lesson l LEFT JOIN lesson_progress lp ON lp.lesson_id = l.lesson_id AND lp.user_id = @uid WHERE l.course_id = @cid ORDER BY l.lesson_id";

            var lessonParams = new Dictionary<string, object> { { "@cid", _courseId } };
            if (!IsGuest) lessonParams.Add("@uid", UserId);

            _lessons = Db.Query(lessonQuery, lessonParams);

            // Apply guest temporary state from Session if guest
            if (IsGuest)
            {
                var completedSet = GuestCompletedLessons;
                foreach (DataRow row in _lessons.Rows)
                {
                    long lid = Convert.ToInt64(row["lesson_id"]);
                    if (completedSet.Contains(lid))
                    {
                        row["done"] = true;
                    }
                }
            }

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

        private void Render()
        {
            DataRow cur = _lessons.Rows[_index];
            int total = _lessons.Rows.Count;

            int done = 0;
            foreach (DataRow r in _lessons.Rows)
                if (Convert.ToBoolean(r["done"])) done++;
            int pct = total > 0 ? (int)(done * 100L / total) : 0;

            litLessonNo.Text = (_index + 1).ToString();
            litLessonTotal.Text = total.ToString();
            litPct.Text = pct.ToString();
            litDoneCount.Text = done.ToString();
            litTotalCount.Text = total.ToString();
            barFill.Attributes["style"] = "width:" + pct + "%";

            litLessonTitle.Text = Convert.ToString(cur["lesson_title"]);
            litLessonDesc.Text = Convert.ToString(cur["lesson_description"]);

            string jsonContent = Convert.ToString(cur["lesson_content"]);
            string videoHtml = "";
            string bodyContentHtml = "";

            if (!string.IsNullOrWhiteSpace(jsonContent))
            {
                try
                {
                    var blocks = new JavaScriptSerializer().Deserialize<List<Dictionary<string, object>>>(jsonContent);
                    if (blocks != null)
                    {
                        System.Text.StringBuilder contentBuilder = new System.Text.StringBuilder();

                        foreach (var block in blocks)
                        {
                            if (block == null || !block.TryGetValue("type", out object typeObj)) continue;
                            string type = Convert.ToString(typeObj).ToLowerInvariant();

                            if (type == "video" && block.TryGetValue("url", out object urlObj))
                            {
                                string url = Convert.ToString(urlObj);
                                string builtVideo = BuildVideoHtml(url);

                                if (string.IsNullOrEmpty(videoHtml))
                                {
                                    videoHtml = builtVideo;
                                }
                                else
                                {
                                    contentBuilder.Append(builtVideo);
                                }
                            }
                            else if (type == "paragraph" && block.TryGetValue("content", out object textObj))
                            {
                                string text = Convert.ToString(textObj);
                                contentBuilder.AppendFormat("<p class=\"lesson-p\">{0}</p>", HttpUtility.HtmlEncode(text));
                            }
                            else if (type == "subheading" && block.TryGetValue("text", out object subObj))
                            {
                                string sub = Convert.ToString(subObj);
                                contentBuilder.AppendFormat("<h3 class=\"lesson-subheading\">{0}</h3>", HttpUtility.HtmlEncode(sub));
                            }
                            else if (type == "list" && block.TryGetValue("items", out object itemsObj))
                            {
                                contentBuilder.Append("<ul class=\"lesson-list\">");
                                if (itemsObj is System.Collections.IEnumerable listItems)
                                {
                                    foreach (var item in listItems)
                                    {
                                        contentBuilder.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(Convert.ToString(item)));
                                    }
                                }
                                contentBuilder.Append("</ul>");
                            }
                            else if (type == "image" && block.TryGetValue("url", out object imgUrlObj))
                            {
                                string imgUrl = Convert.ToString(imgUrlObj);
                                block.TryGetValue("caption", out object captionObj);
                                string caption = captionObj != null ? Convert.ToString(captionObj) : "";

                                // Properly resolve relative tilde paths (e.g., ~/uploads/...) to absolute web paths
                                if (!string.IsNullOrWhiteSpace(imgUrl))
                                {
                                    imgUrl = imgUrl.Trim();
                                    if (!imgUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                    {
                                        imgUrl = ResolveUrl("~/" + imgUrl.TrimStart('/', '~'));
                                    }
                                }

                                contentBuilder.Append("<div class=\"lesson-image-wrapper\">");
                                contentBuilder.AppendFormat("<img src=\"{0}\" alt=\"{1}\" class=\"lesson-img\" />",
                                    HttpUtility.HtmlAttributeEncode(imgUrl), HttpUtility.HtmlAttributeEncode(caption));
                                if (!string.IsNullOrEmpty(caption))
                                {
                                    contentBuilder.AppendFormat("<span class=\"lesson-caption\">{0}</span>", HttpUtility.HtmlEncode(caption));
                                }
                                contentBuilder.Append("</div>");
                            }
                        }

                        bodyContentHtml = contentBuilder.ToString();
                    }
                }
                catch (Exception)
                {
                    bodyContentHtml = HttpUtility.HtmlEncode(jsonContent).Replace("\r\n", "<br />").Replace("\n", "<br />");
                }
            }

            litVideo.Text = videoHtml;

            if (string.IsNullOrWhiteSpace(bodyContentHtml))
            {
                pnlContent.Visible = false;
                litContent.Text = "";
            }
            else
            {
                pnlContent.Visible = true;
                litContent.Text = bodyContentHtml;
            }

            // Buttons & Navigation State for Guests vs Students
            bool isDone = Convert.ToBoolean(cur["done"]);
            btnComplete.Visible = !isDone;
            doneBox.Visible = isDone;
            lnkDiscuss.Visible = true; // Visible for both guests and students now

            lnkPrev.Visible = _index > 0;
            if (lnkPrev.Visible)
                lnkPrev.NavigateUrl = LessonUrl(Convert.ToInt64(_lessons.Rows[_index - 1]["lesson_id"]));

            lnkNext.Visible = _index < total - 1;
            if (lnkNext.Visible)
                lnkNext.NavigateUrl = LessonUrl(Convert.ToInt64(_lessons.Rows[_index + 1]["lesson_id"]));

            // Quiz Link (works for guests too, routing to assessment.aspx)
            long lessonQuizId = cur["quiz_id"] == DBNull.Value ? -1 : Convert.ToInt64(cur["quiz_id"]);
            DataTable qz = Db.Query(
                "SELECT quiz_id FROM quiz WHERE lesson_id = @lid OR quiz_id = @qid ORDER BY quiz_id LIMIT 1",
                new Dictionary<string, object> { { "@lid", _lessonId }, { "@qid", lessonQuizId } });
            lnkQuiz.Visible = qz.Rows.Count > 0;
            if (lnkQuiz.Visible)
                lnkQuiz.NavigateUrl = "~/pages/Student/assessment.aspx?quiz=" + Convert.ToInt64(qz.Rows[0]["quiz_id"]);

            rptSyllabus.DataSource = _lessons;
            rptSyllabus.DataBind();

            string tutor = Convert.ToString(_course["tutor"]);
            string category = Convert.ToString(_course["category"]);
            litTutorName.Text = tutor;
            litTutorInitial.Text = tutor.Length > 0 ? tutor.Substring(0, 1).ToUpperInvariant() : "?";
            litTutorRole.Text = string.IsNullOrWhiteSpace(category) ? "Tutor" : "Tutor \u00B7 " + category;
            lnkDiscuss.NavigateUrl = "~/pages/Student/messages.aspx?lesson=" + _lessonId + "&tab=lesson";
        }

        protected void btnComplete_Click(object sender, EventArgs e)
        {
            if (IsGuest)
            {
                // Temporarily store in session for the guest without touching database
                GuestCompletedLessons.Add(_lessonId);
            }
            else
            {
                Db.Execute(
                    "INSERT INTO lesson_progress (user_id, lesson_id, is_completed, completed_at) " +
                    "VALUES (@uid, @lid, true, now()) " +
                    "ON CONFLICT (user_id, lesson_id) DO UPDATE SET is_completed = true, completed_at = now()",
                    new Dictionary<string, object> { { "@uid", UserId }, { "@lid", _lessonId } });

                int done = 0;
                foreach (DataRow r in _lessons.Rows)
                    if (Convert.ToBoolean(r["done"])) done++;
                if (!Convert.ToBoolean(_lessons.Rows[_index]["done"])) done++;
                long pct = done * 100L / _lessons.Rows.Count;

                Db.Execute(
                    "UPDATE enrollment SET progression = @p WHERE user_id = @uid AND course_id = @cid",
                    new Dictionary<string, object> { { "@p", pct }, { "@uid", UserId }, { "@cid", _courseId } });
            }

            Response.Redirect(LessonUrl(_lessonId));
        }

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

        private string BuildVideoHtml(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";

            Match match = Regex.Match(url,
                @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return "<div class=\"lv-video\"><iframe src=\"https://www.youtube.com/embed/" + match.Groups[1].Value + "\" title=\"Lesson video\" allowfullscreen></iframe></div>";
            }

            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri) && uri.Scheme == Uri.UriSchemeHttps &&
                url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                return "<div class=\"lv-video\"><video controls src=\"" + HttpUtility.HtmlAttributeEncode(url) + "\"></video></div>";
            }

            return "";
        }
    }
}