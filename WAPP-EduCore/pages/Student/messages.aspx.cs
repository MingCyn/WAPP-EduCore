using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WAPP_EduCore.pages.Student
{
    public partial class messages : System.Web.UI.Page
    {
        private const int UtcOffsetHours = 8;   // Malaysia time, used for older dates
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Session key used to show a popup after a redirect (e.g. "Post updated")
        private const string FlashKey = "FmFlash";

        private class Post
        {
            public long Id;
            public string Title;
            public string Content;
            public string Type;
            public DateTime Created;   // UTC
            public long AuthorId;
            public string Author;
            public string Role;
            public long Replies;
        }

        private DataTable _enrolled;
        private string _tab;
        private long _lessonId, _postId;
        private long _lessonCourseId, _lessonNo;
        private string _lessonTitle = "";

        private bool IsGuest
        {
            get
            {
                object id = Session["UserID"];
                if (id == null) return true;

                long parsed;
                if (!long.TryParse(Convert.ToString(id), out parsed) || parsed <= 0) return true;

                string role = Convert.ToString(Session["Role"]) ?? Convert.ToString(Session["UserRole"]);
                if (string.Equals(role, "guest", StringComparison.OrdinalIgnoreCase)) return true;

                return false;
            }
        }

        private long UserId { get { return IsGuest ? 0 : Convert.ToInt64(Session["UserID"]); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            long.TryParse(Request.QueryString["lesson"], out _lessonId);
            long.TryParse(Request.QueryString["post"], out _postId);

            if (IsGuest)
            {
                _enrolled = new DataTable();

                // Configure guest UI constraints safely from code-behind
                pnlGuestNotice.Visible = true;
                txtTitle.Attributes["onclick"] = "openLoginPrompt(); return false;";
                txtContent.Attributes["onclick"] = "openLoginPrompt(); return false;";
                btnPost.OnClientClick = "openLoginPrompt(); return false;";

                txtReply.Attributes["onclick"] = "openLoginPrompt(); return false;";
                txtReply.Attributes["placeholder"] = "Log in to write a reply...";
                btnReply.OnClientClick = "openLoginPrompt(); return false;";
            }
            else
            {
                _enrolled = Db.Query(
                    "SELECT c.course_id, c.title FROM courses c " +
                    "JOIN enrollment e ON e.course_id = c.course_id WHERE e.user_id = @uid ORDER BY c.title",
                    new Dictionary<string, object> { { "@uid", UserId } });

                if (_enrolled.Rows.Count == 0)
                {
                    pnlForum.Visible = false;
                    pnlNoCourses.Visible = true;
                    return;
                }
            }

            if (_lessonId > 0) LoadLessonContext();

            string tab = (Request.QueryString["tab"] ?? "").ToLowerInvariant();
            if (tab != "all" && tab != "lesson" && tab != "questions" && tab != "announcements")
                tab = _lessonId > 0 ? "lesson" : "all";
            if (tab == "lesson" && _lessonId == 0) tab = "all";
            _tab = tab;

            if (!IsPostBack)
            {
                DataTable courseSource = IsGuest
                    ? Db.Query("SELECT course_id, title FROM courses ORDER BY title")
                    : _enrolled;

                foreach (DataRow r in courseSource.Rows)
                    ddlCourse.Items.Add(new ListItem(Convert.ToString(r["title"]), Convert.ToString(r["course_id"])));
            }
            pnlCoursePick.Visible = _lessonId == 0;

            BuildTopbar();
            BuildTabs();
            RenderPosts();
            BuildPeople();

            ShowFlashIfAny();
        }

        // ---------- lesson context (opened from the lesson page) ----------
        private void LoadLessonContext()
        {
            string sql =
                "SELECT l.course_id, l.lesson_title, " +
                "  (SELECT COUNT(*) FROM lesson l2 WHERE l2.course_id = l.course_id AND l2.lesson_id <= l.lesson_id) AS lesson_no " +
                "FROM lesson l WHERE l.lesson_id = @lid";

            var prm = new Dictionary<string, object> { { "@lid", _lessonId } };

            if (!IsGuest)
            {
                sql += " AND l.course_id IN (SELECT course_id FROM enrollment WHERE user_id = @uid)";
                prm["@uid"] = UserId;
            }

            DataTable t = Db.Query(sql, prm);

            if (t.Rows.Count == 0) { _lessonId = 0; return; }

            _lessonCourseId = Convert.ToInt64(t.Rows[0]["course_id"]);
            _lessonTitle = Convert.ToString(t.Rows[0]["lesson_title"]);
            _lessonNo = Convert.ToInt64(t.Rows[0]["lesson_no"]);
        }

        private void BuildTopbar()
        {
            if (_lessonId == 0) return;

            litChip.Text = "<span class=\"fm-chip\">Lesson " + _lessonNo + "</span>";
            litTopic.Text = "<span>Topic: " + HttpUtility.HtmlEncode(_lessonTitle) + "</span>" +
                "<a class=\"fm-back\" href=\"" + HttpUtility.HtmlAttributeEncode(ResolveUrl(
                    "~/pages/Student/learningProgression.aspx?course=" + _lessonCourseId + "&lesson=" + _lessonId)) +
                "\">Back to lesson</a>";
        }

        private void BuildTabs()
        {
            var sb = new StringBuilder();
            AppendTab(sb, "all", "All Posts");
            if (_lessonId > 0) AppendTab(sb, "lesson", "Discussed in Lesson");
            AppendTab(sb, "questions", "Questions");
            AppendTab(sb, "announcements", "Announcements");
            litTabs.Text = sb.ToString();
        }

        private void AppendTab(StringBuilder sb, string key, string label)
        {
            sb.Append("<a class=\"fm-tab" + (key == _tab ? " active" : "") + "\" href=\"" + Href(key, 0) + "\">" +
                      label + "</a>");
        }

        // ---------- the threads ----------
        private void RenderPosts()
        {
            List<Post> posts;
            try
            {
                posts = LoadPosts();
            }
            catch (Exception ex)
            {
                litBefore.Text = "<div class=\"fm-empty-box\">Could not load discussions: " +
                                 HttpUtility.HtmlEncode(ex.Message) +
                                 "<br />Make sure the discussion tables exist (see the setup SQL).</div>";
                return;
            }

            pnlNoPosts.Visible = posts.Count == 0;

            int sel = posts.FindIndex(p => p.Id == _postId);
            var before = new StringBuilder();
            var after = new StringBuilder();

            for (int i = 0; i < posts.Count; i++)
            {
                if (sel < 0 || i < sel) before.Append(CardHtml(posts[i]));
                else if (i > sel) after.Append(CardHtml(posts[i]));
            }
            litBefore.Text = before.ToString();
            litAfter.Text = after.ToString();

            if (sel >= 0)
            {
                Post p = posts[sel];
                pnlSelected.Visible = true;
                litSelTop.Text = AuthorHtml(p) +
                    "<div class=\"fm-title\">" + HttpUtility.HtmlEncode(p.Title) + "</div>" +
                    "<div class=\"fm-body\">" + Multiline(p.Content) + "</div>" +
                    OwnerActionsHtml(p);
                litReplies.Text = RepliesHtml(p.Id);
            }
        }

        private List<Post> LoadPosts()
        {
            var where = new StringBuilder("1=1");
            var prm = new Dictionary<string, object>();

            if (!IsGuest)
            {
                where.Clear();
                where.Append("p.course_id IN (SELECT course_id FROM enrollment WHERE user_id = @uid)");
                prm["@uid"] = UserId;
            }

            if (_tab == "lesson")
            {
                where.Append(" AND p.lesson_id = @lid");
                prm["@lid"] = _lessonId;
            }
            else if (_tab == "questions")
            {
                if (_lessonId > 0)
                {
                    where.Append(" AND (p.lesson_id = @lid OR p.course_id = @cid)");
                    prm["@lid"] = _lessonId;
                    prm["@cid"] = _lessonCourseId;
                }
                where.Append(" AND p.post_type = 'Question'");
            }
            else
            {
                if (_lessonId > 0 && _tab != "all" && _tab != "announcements")
                {
                    where.Append(" AND p.course_id = @cid");
                    prm["@cid"] = _lessonCourseId;
                }

                if (_tab == "questions") where.Append(" AND p.post_type = 'Question'");
                else if (_tab == "announcements") where.Append(" AND p.post_type = 'Announcement'");
            }

            string limitClause = IsGuest ? " LIMIT 3" : " LIMIT 30";

            DataTable t = Db.Query(
                "SELECT p.post_id, p.title, p.content, p.post_type, p.created_at, " +
                "  u.user_id AS author_id, u.full_name, u.role, " +
                "  (SELECT COUNT(*) FROM discussion_replies r WHERE r.post_id = p.post_id) AS reply_count " +
                "FROM discussion_posts p JOIN users u ON u.user_id = p.user_id " +
                "WHERE " + where + " ORDER BY p.created_at DESC" + limitClause, prm);

            var list = new List<Post>();
            foreach (DataRow r in t.Rows)
            {
                list.Add(new Post
                {
                    Id = Convert.ToInt64(r["post_id"]),
                    Title = Convert.ToString(r["title"]),
                    Content = Convert.ToString(r["content"]),
                    Type = Convert.ToString(r["post_type"]),
                    Created = DateTime.SpecifyKind(Convert.ToDateTime(r["created_at"]), DateTimeKind.Utc),
                    AuthorId = Convert.ToInt64(r["author_id"]),
                    Author = Convert.ToString(r["full_name"]),
                    Role = Convert.ToString(r["role"]),
                    Replies = Convert.ToInt64(r["reply_count"])
                });
            }
            return list;
        }

        private string CardHtml(Post p)
        {
            return "<a class=\"fm-card\" href=\"" + Href(_tab, p.Id) + "\">" +
                   AuthorHtml(p) +
                   "<div class=\"fm-title\">" + HttpUtility.HtmlEncode(p.Title) + "</div>" +
                   "<div class=\"fm-meta\">Replies: " + p.Replies + "</div></a>";
        }

        private string AuthorHtml(Post p)
        {
            string tag = "";
            if (p.Type == "Question") tag = "<span class=\"fm-tag q\">Question</span>";
            else if (p.Type == "Announcement") tag = "<span class=\"fm-tag a\">Announcement</span>";

            string you = IsMine(p) ? "<span class=\"fmx-you\">Your post</span>" : "";

            return "<div class=\"fm-author\">" + AvatarHtml(p.AuthorId, p.Author) +
                   "<div class=\"fm-who\"><strong>" + HttpUtility.HtmlEncode(p.Author) + "</strong>" + you + "<small>" +
                   HttpUtility.HtmlEncode(p.Role) + " &middot; " + TimeAgo(p.Created) + "</small></div>" +
                   tag + "</div>";
        }

        private string RepliesHtml(long postId)
        {
            DataTable t = Db.Query(
                "SELECT r.content, u.full_name FROM discussion_replies r " +
                "JOIN users u ON u.user_id = r.user_id WHERE r.post_id = @pid ORDER BY r.created_at",
                new Dictionary<string, object> { { "@pid", postId } });

            if (t.Rows.Count == 0)
                return "<div class=\"fm-noreplies\">No replies yet. Be the first to answer.</div>";

            var sb = new StringBuilder();
            foreach (DataRow r in t.Rows)
            {
                sb.Append("<div class=\"fm-reply\"><strong>" + HttpUtility.HtmlEncode(Convert.ToString(r["full_name"])) +
                          "</strong>" + Multiline(Convert.ToString(r["content"])) + "</div>");
            }
            return sb.ToString();
        }

        private void BuildPeople()
        {
            string scope;
            var prm = new Dictionary<string, object>();

            if (IsGuest)
            {
                scope = "1=1";
            }
            else
            {
                scope = "p.course_id IN (SELECT course_id FROM enrollment WHERE user_id = @uid)";
                prm["@uid"] = UserId;
            }

            if (_lessonId > 0 && _tab == "lesson")
            {
                scope += " AND p.course_id = @cid";
                prm["@cid"] = _lessonCourseId;
            }

            try
            {
                DataTable t = Db.Query(
                    "SELECT u.user_id, u.full_name FROM (" +
                    "  SELECT p.user_id, MAX(p.created_at) AS t FROM discussion_posts p WHERE " + scope + " GROUP BY p.user_id " +
                    "  UNION ALL " +
                    "  SELECT r.user_id, MAX(r.created_at) FROM discussion_replies r " +
                    "    JOIN discussion_posts p ON p.post_id = r.post_id WHERE " + scope + " GROUP BY r.user_id" +
                    ") a JOIN users u ON u.user_id = a.user_id " +
                    "GROUP BY u.user_id, u.full_name ORDER BY MAX(a.t) DESC LIMIT 6", prm);

                if (t.Rows.Count == 0)
                {
                    litPeople.Text = "<div class=\"fm-meta\" style=\"margin-top:10px\">No activity yet.</div>";
                    return;
                }

                var sb = new StringBuilder("<div class=\"fm-people\">");
                foreach (DataRow r in t.Rows)
                    sb.Append(AvatarHtml(Convert.ToInt64(r["user_id"]), Convert.ToString(r["full_name"])));
                sb.Append("</div>");
                litPeople.Text = sb.ToString();
            }
            catch (Exception)
            {
                litPeople.Text = "";
            }
        }

        // ---------- posting ----------
        protected void btnPost_Click(object sender, EventArgs e)
        {
            if (IsGuest)
            {
                PostError("Please log in now to unlock all functions, post topics, and engage in discussions!");
                return;
            }

            string title = txtTitle.Text.Trim();
            string content = txtContent.Text.Trim();

            if (title.Length == 0) { PostError("Please enter a topic title."); return; }
            if (title.Length > 150) { PostError("The title is too long (150 characters at most)."); return; }
            if (content.Length == 0) { PostError("Please write something in the content box."); return; }
            if (content.Length > 2000) { PostError("The content is too long (2000 characters at most)."); return; }

            long courseId;
            if (_lessonId > 0) courseId = _lessonCourseId;
            else if (!long.TryParse(ddlCourse.SelectedValue, out courseId)) { PostError("Please choose a course."); return; }

            bool enrolled = _enrolled.AsEnumerable().Any(r => Convert.ToInt64(r["course_id"]) == courseId);
            if (!enrolled) { PostError("You are not enrolled in that course."); return; }

            long newId;
            try
            {
                DataTable saved = Db.Query(
                    "INSERT INTO discussion_posts (user_id, course_id, lesson_id, title, content, post_type) " +
                    "VALUES (@uid, @cid, @lid, @t, @c, @type) RETURNING post_id",
                    new Dictionary<string, object>
                    {
                        { "@uid", UserId },
                        { "@cid", courseId },
                        { "@lid", (_lessonId > 0 && _tab == "lesson") ? (object)_lessonId : DBNull.Value },
                        { "@t", title },
                        { "@c", content },
                        { "@type", ddlType.SelectedValue }
                    });
                newId = Convert.ToInt64(saved.Rows[0]["post_id"]);
            }
            catch (Exception ex)
            {
                PostError("Could not post: " + ex.Message);
                return;
            }

            Response.Redirect(ResolveUrl(RawUrl(_lessonId > 0 ? "lesson" : "all", newId)));
        }

        protected void btnReply_Click(object sender, EventArgs e)
        {
            if (IsGuest)
            {
                ReplyError("Please log in now to unlock all functions and reply to discussions!");
                return;
            }

            if (_postId <= 0) return;

            string text = txtReply.Text.Trim();
            if (text.Length == 0) { ReplyError("Please write a reply first."); return; }
            if (text.Length > 1000) { ReplyError("The reply is too long (1000 characters at most)."); return; }

            try
            {
                DataTable ok = Db.Query(
                    "SELECT 1 FROM discussion_posts p WHERE p.post_id = @pid " +
                    "AND p.course_id IN (SELECT course_id FROM enrollment WHERE user_id = @uid)",
                    new Dictionary<string, object> { { "@pid", _postId }, { "@uid", UserId } });
                if (ok.Rows.Count == 0) { ReplyError("You cannot reply to this thread."); return; }

                Db.Execute(
                    "INSERT INTO discussion_replies (post_id, user_id, content) VALUES (@pid, @uid, @c)",
                    new Dictionary<string, object> { { "@pid", _postId }, { "@uid", UserId }, { "@c", text } });
            }
            catch (Exception ex)
            {
                ReplyError("Could not send the reply: " + ex.Message);
                return;
            }

            Response.Redirect(Request.RawUrl);
        }

        // ---------- NEW: edit / delete your own post ----------

        // Students may only change their own Discussion and Question posts (never Announcements).
        private static bool IsEditableType(string type)
        {
            return string.Equals(type, "Discussion", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "Question", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsMine(Post p)
        {
            return !IsGuest && p.AuthorId == UserId;
        }

        // The Edit and Delete buttons shown under the selected post (only for the author)
        private string OwnerActionsHtml(Post p)
        {
            if (!IsMine(p) || !IsEditableType(p.Type)) return "";

            string type = string.Equals(p.Type, "Question", StringComparison.OrdinalIgnoreCase) ? "Question" : "Discussion";

            return "<div class=\"fmx-actions\">" +
                   "<button type=\"button\" class=\"fmx-act\" onclick=\"fmxOpenEdit(this)\"" +
                   " data-title=\"" + HttpUtility.HtmlAttributeEncode(p.Title) + "\"" +
                   " data-type=\"" + type + "\"" +
                   " data-content=\"" + HttpUtility.HtmlAttributeEncode(p.Content) + "\">" +
                   "<svg width=\"15\" height=\"15\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" +
                   "<path d=\"M12 20h9\"/><path d=\"M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z\"/></svg>Edit</button>" +
                   "<button type=\"button\" class=\"fmx-act danger\" onclick=\"fmxAskDelete(this)\"" +
                   " data-title=\"" + HttpUtility.HtmlAttributeEncode(p.Title) + "\"" +
                   " data-replies=\"" + p.Replies + "\">" +
                   "<svg width=\"15\" height=\"15\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" +
                   "<polyline points=\"3 6 5 6 21 6\"/><path d=\"M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6\"/>" +
                   "<path d=\"M10 11v6\"/><path d=\"M14 11v6\"/><path d=\"M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2\"/></svg>Delete</button>" +
                   "</div>";
        }

        // Checks that the post exists, belongs to this student, and is a Discussion/Question.
        // Returns null when everything is fine, otherwise a message to show the student.
        private string CheckOwnPost()
        {
            if (_postId <= 0)
                return "We couldn\u2019t tell which post you meant. Please refresh the page and try again.";

            DataTable own = Db.Query(
                "SELECT post_type FROM discussion_posts WHERE post_id = @pid AND user_id = @uid",
                new Dictionary<string, object> { { "@pid", _postId }, { "@uid", UserId } });

            if (own.Rows.Count == 0)
                return "This post no longer exists, or it isn\u2019t yours. You can only change posts that you wrote.";

            if (!IsEditableType(Convert.ToString(own.Rows[0]["post_type"])))
                return "Announcements can\u2019t be changed here.";

            return null;
        }

        protected void btnSaveEdit_Click(object sender, EventArgs e)
        {
            if (IsGuest)
            {
                ClientScript.RegisterStartupScript(GetType(), "openLoginPrompt", "openLoginPrompt();", true);
                return;
            }

            string title = txtEditTitle.Text.Trim();
            string content = txtEditContent.Text.Trim();
            string type = ddlEditType.SelectedValue == "Question" ? "Question" : "Discussion";

            if (title.Length == 0) { EditError("Please enter a topic title."); return; }
            if (title.Length > 150) { EditError("The title is too long (150 characters at most)."); return; }
            if (content.Length == 0) { EditError("Please write something in the content box."); return; }
            if (content.Length > 2000) { EditError("The content is too long (2000 characters at most)."); return; }

            try
            {
                string problem = CheckOwnPost();
                if (problem != null) { EditError(problem); return; }

                Db.Execute(
                    "UPDATE discussion_posts SET title = @t, content = @c, post_type = @type " +
                    "WHERE post_id = @pid AND user_id = @uid",
                    new Dictionary<string, object>
                    {
                        { "@t", title },
                        { "@c", content },
                        { "@type", type },
                        { "@pid", _postId },
                        { "@uid", UserId }
                    });
            }
            catch (Exception)
            {
                EditError("Something went wrong while saving your changes. Please try again.");
                return;
            }

            SetFlash("success", "Post updated", "Your changes have been saved.");
            Response.Redirect(ResolveUrl(RawUrl(_tab, _postId)));
        }

        protected void btnConfirmDelete_Click(object sender, EventArgs e)
        {
            if (IsGuest)
            {
                ClientScript.RegisterStartupScript(GetType(), "openLoginPrompt", "openLoginPrompt();", true);
                return;
            }

            try
            {
                string problem = CheckOwnPost();
                if (problem != null)
                {
                    NoticeNow("error", "Can\u2019t delete this post", problem);
                    return;
                }

                // Remove the replies first, then the post itself (works with or without ON DELETE CASCADE)
                Db.Execute(
                    "DELETE FROM discussion_replies WHERE post_id = @pid",
                    new Dictionary<string, object> { { "@pid", _postId } });

                Db.Execute(
                    "DELETE FROM discussion_posts WHERE post_id = @pid AND user_id = @uid",
                    new Dictionary<string, object> { { "@pid", _postId }, { "@uid", UserId } });
            }
            catch (Exception)
            {
                NoticeNow("error", "Could not delete the post",
                    "Something went wrong while deleting your post. Please try again.");
                return;
            }

            SetFlash("success", "Post deleted", "Your post and its replies have been removed.");
            Response.Redirect(ResolveUrl(RawUrl(_tab, 0)));
        }

        // Shows an error inside the Edit popup and re-opens it (the student keeps what they typed)
        private void EditError(string text)
        {
            fmxEditErr.InnerText = text;
            ClientScript.RegisterStartupScript(GetType(), "fmxShowEdit", "fmxShowEdit();", true);
        }

        // Shows the styled notice popup right away (no redirect)
        private void NoticeNow(string kind, string title, string message)
        {
            ClientScript.RegisterStartupScript(GetType(), "fmxNotice", NoticeScript(kind, title, message), true);
        }

        // Remembers a notice for the next page load (used right before a redirect)
        private void SetFlash(string kind, string title, string message)
        {
            Session[FlashKey] = new[] { kind, title, message };
        }

        private void ShowFlashIfAny()
        {
            string[] f = Session[FlashKey] as string[];
            if (f == null || f.Length < 3) return;

            Session.Remove(FlashKey);
            ClientScript.RegisterStartupScript(GetType(), "fmxFlash", NoticeScript(f[0], f[1], f[2]), true);
        }

        private static string NoticeScript(string kind, string title, string message)
        {
            return "fmxNotice('" + HttpUtility.JavaScriptStringEncode(kind) + "','" +
                   HttpUtility.JavaScriptStringEncode(title) + "','" +
                   HttpUtility.JavaScriptStringEncode(message) + "');";
        }

        private void PostError(string text)
        {
            if (IsGuest)
            {
                ClientScript.RegisterStartupScript(GetType(), "openLoginPrompt", "openLoginPrompt();", true);
            }
            else
            {
                lblPostError.Text = HttpUtility.HtmlEncode(text);
                lblPostError.Visible = true;
            }
        }

        private void ReplyError(string text)
        {
            if (IsGuest)
            {
                ClientScript.RegisterStartupScript(GetType(), "openLoginPrompt", "openLoginPrompt();", true);
            }
            else
            {
                lblReplyError.Text = HttpUtility.HtmlEncode(text);
                lblReplyError.Visible = true;
            }
        }

        private string RawUrl(string tab, long postId)
        {
            return "~/pages/Student/messages.aspx?tab=" + tab +
                   (_lessonId > 0 ? "&lesson=" + _lessonId : "") +
                   (postId > 0 ? "&post=" + postId : "");
        }

        private string Href(string tab, long postId)
        {
            return HttpUtility.HtmlAttributeEncode(ResolveUrl(RawUrl(tab, postId)));
        }

        private static string Multiline(string text)
        {
            return HttpUtility.HtmlEncode(text ?? "").Replace("\r\n", "<br />").Replace("\n", "<br />");
        }

        private static string AvatarHtml(long userId, string name)
        {
            return "<span class=\"fm-av fm-c" + (userId % 4) + "\" title=\"" +
                   HttpUtility.HtmlAttributeEncode(name) + "\">" + HttpUtility.HtmlEncode(InitialsOf(name)) + "</span>";
        }

        private static string InitialsOf(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            string[] parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string initials = parts[0].Substring(0, 1);
            if (parts.Length > 1) initials += parts[parts.Length - 1].Substring(0, 1);
            return initials.ToUpperInvariant();
        }

        private static string TimeAgo(DateTime utc)
        {
            TimeSpan d = DateTime.UtcNow - utc;
            if (d.TotalMinutes < 1) return "just now";
            if (d.TotalMinutes < 60) return Plural((int)d.TotalMinutes, "minute");
            if (d.TotalHours < 24) return Plural((int)d.TotalHours, "hour");
            if (d.TotalDays < 7) return Plural((int)d.TotalDays, "day");
            return utc.AddHours(UtcOffsetHours).ToString("d MMM yyyy", Inv);
        }

        private static string Plural(int n, string unit)
        {
            return n + " " + unit + (n == 1 ? "" : "s") + " ago";
        }
    }
}