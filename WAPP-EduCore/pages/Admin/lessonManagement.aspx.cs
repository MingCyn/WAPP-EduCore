using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using Npgsql;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    // NOTE: the class name is lessonManagement because the .aspx first line says Inherits="...lessonManagement"
    public partial class lessonManagement : System.Web.UI.Page
    {
        // These are shown in the sidebar and the top-right circle
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        private const int PageSize = 10;    // how many lessons per page (change me!)

        // The page number we are on (0 = first page). It is remembered between clicks.
        private int CurrentPage
        {
            get { return ViewState["page"] == null ? 0 : (int)ViewState["page"]; }
            set { ViewState["page"] = value; }
        }

        // The SQL for "which lessons?"  (used for counting AND for the table)
        //   l = the lesson,   l.qid = the id of its quiz (or empty when it has no quiz)
        //   c = its course,   q = its quiz
        private const string FromAndWhere =
            @" FROM (SELECT x.*,
                            COALESCE(x.quiz_id, (SELECT MIN(z.quiz_id) FROM public.quiz z WHERE z.lesson_id = x.lesson_id)) AS qid
                     FROM public.lesson x) l
               LEFT JOIN public.courses c ON c.course_id = l.course_id
               LEFT JOIN public.quiz q ON q.quiz_id = l.qid
               WHERE (@search = '' OR l.lesson_title ILIKE @like OR c.title ILIKE @like)
                 AND (@course = 0 OR l.course_id = @course)
                 AND (@quiz = ''
                      OR (@quiz = 'with'    AND l.qid IS NOT NULL)
                      OR (@quiz = 'without' AND l.qid IS NULL)) ";


        // =====================================================
        //  STEP 1: Page opens  ->  find out who the admin is
        // =====================================================
        protected void Page_Load(object sender, EventArgs e)
        {
            LoadAdminInfo();
        }

        // =====================================================
        //  STEP 2: PreRender runs AFTER every button click and
        //  just BEFORE the page is drawn. So everything on the
        //  page is always fresh.
        // =====================================================
        protected void Page_PreRender(object sender, EventArgs e)
        {
            // Each part is tried on its own. If one part has a problem, a red box at the
            // top shows the REAL error message, and the rest of the page still works.
            try { FillCourseLists(); } catch (Exception ex) { ShowProblem("the course lists", ex); }   // the course dropdowns
            try { LoadStats(); } catch (Exception ex) { ShowProblem("the number boxes", ex); }         // the 5 number boxes
            try { LoadLessons(); } catch (Exception ex) { ShowProblem("the lesson table", ex); }       // the table
        }


        // =====================================================
        //  ADMIN NAME (sidebar + top-right circle)
        // =====================================================
        private void LoadAdminInfo()
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;

                if (CurrentUserId() > 0)
                {
                    cmd.CommandText = "SELECT user_id, full_name FROM public.users WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("@id", CurrentUserId());
                }
                else
                {
                    cmd.CommandText = "SELECT user_id, full_name FROM public.users WHERE role = 'Admin' ORDER BY user_id LIMIT 1";
                }

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string fullName = reader[1].ToString().Trim();
                        AdminName = Server.HtmlEncode(fullName);

                        string[] words = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length == 1)
                            AdminInitials = words[0].Substring(0, 1).ToUpper();
                        else if (words.Length > 1)
                            AdminInitials = words[0].Substring(0, 1).ToUpper();
                    }
                }
            }
        }


        // =====================================================
        //  THE COURSE DROPDOWNS (filter + Create popup + Edit popup)
        // =====================================================
        private void FillCourseLists()
        {
            DataTable courses = GetTable("SELECT course_id, title FROM public.courses ORDER BY title");

            FillOneList(ddlCourse, courses, "All courses", "0");
            FillOneList(ddlAddCourse, courses, "Select a course", "");
            FillOneList(ddlEditCourse, courses, null, "");
        }

        // Puts the courses in ONE dropdown (and keeps the choice the admin made)
        private void FillOneList(DropDownList list, DataTable courses, string firstText, string firstValue)
        {
            string keep = list.SelectedValue;
            list.Items.Clear();

            if (firstText != null) list.Items.Add(new ListItem(firstText, firstValue));

            foreach (DataRow r in courses.Rows)
            {
                list.Items.Add(new ListItem(r["title"].ToString(), r["course_id"].ToString()));
            }

            if (list.Items.FindByValue(keep) != null) list.SelectedValue = keep;
        }


        // =====================================================
        //  THE 5 NUMBER BOXES
        // =====================================================
        private void LoadStats()
        {
            // "has a video" = the lesson content has a video block (the link is saved inside lesson_content)
            string hasVideo = "(COALESCE(x.lesson_content::text, '') LIKE '%\"video\"%')";

            int total = GetNumber("SELECT COUNT(*) FROM public.lesson");
            int courses = GetNumber("SELECT COUNT(DISTINCT course_id) FROM public.lesson");
            int video = GetNumber("SELECT COUNT(*) FROM public.lesson x WHERE " + hasVideo);
            int quiz = GetNumber(
                @"SELECT COUNT(*) FROM public.lesson x
                  WHERE x.quiz_id IS NOT NULL OR EXISTS (SELECT 1 FROM public.quiz z WHERE z.lesson_id = x.lesson_id)");

            lblTotal.Text = total.ToString("N0");

            lblCourses.Text = courses.ToString("N0");
            lblCoursesNote.Text = "Courses that have lessons";

            lblVideo.Text = video.ToString("N0");
            lblVideoNote.Text = Percent(video, total) + " of lessons";

            lblQuiz.Text = quiz.ToString("N0");
            lblQuizNote.Text = Percent(quiz, total) + " of lessons";

            double average = (courses == 0) ? 0 : (double)total / courses;
            lblAverage.Text = average.ToString("0.0", CultureInfo.InvariantCulture);
        }


        // =====================================================
        //  THE LESSON TABLE  (search + filters + pages)
        // =====================================================
        private void LoadLessons()
        {
            // 1) How many lessons match the search?
            int total;
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*)" + FromAndWhere, conn))
            {
                AddFilters(cmd);
                total = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 2) Make sure the page number is not too big
            int lastPage = Math.Max(0, (total - 1) / PageSize);
            if (CurrentPage > lastPage) CurrentPage = lastPage;
            if (CurrentPage < 0) CurrentPage = 0;

            // 3) Get only the lessons for this page
            DataTable table = new DataTable();
            string sql =
                @"SELECT l.lesson_id, l.lesson_title, COALESCE(l.lesson_description, '') AS description,
                         l.course_id, c.title AS course_title, l.created_at,
                         COALESCE(l.lesson_content::text, '') AS content,
                         l.qid, q.quiz_title,
                         (SELECT COUNT(*) FROM public.quiz_questions k WHERE k.quiz_id = l.qid) AS qcount"
                + FromAndWhere +
                @" ORDER BY l.lesson_id ASC
                   LIMIT @size OFFSET @skip";

            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                AddFilters(cmd);
                cmd.Parameters.AddWithValue("@size", PageSize);
                cmd.Parameters.AddWithValue("@skip", CurrentPage * PageSize);

                using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd))
                {
                    adapter.Fill(table);
                }
            }

            // 4) Turn every row into HTML
            StringBuilder rows = new StringBuilder();
            foreach (DataRow r in table.Rows)
            {
                rows.Append(BuildRow(r));
            }
            litRows.Text = rows.ToString();

            // 5) Show the table OR the "No results found" box
            pnlTable.Visible = (total > 0);
            pnlEmpty.Visible = (total == 0);

            // 6) "Showing 1-10 of 40" + Previous / Next buttons
            int from = CurrentPage * PageSize + 1;
            int to = CurrentPage * PageSize + table.Rows.Count;
            lblShowing.Text = "Showing " + from + "–" + to + " of " + total.ToString("N0");

            btnPrev.Enabled = (CurrentPage > 0);
            btnNext.Enabled = (CurrentPage < lastPage);
        }

        // Gives the search words to the database command
        private void AddFilters(NpgsqlCommand cmd)
        {
            string search = txtSearch.Text.Trim();
            int course;
            int.TryParse(ddlCourse.SelectedValue, out course);

            cmd.Parameters.AddWithValue("@search", search);
            cmd.Parameters.AddWithValue("@like", "%" + search + "%");
            cmd.Parameters.AddWithValue("@course", course);
            cmd.Parameters.AddWithValue("@quiz", ddlQuizFilter.SelectedValue);
        }

        // Makes ONE table row.
        // The data-... parts hide the lesson's info inside the row,
        // so the popups can read it with JavaScript when you click.
        private string BuildRow(DataRow r)
        {
            int id = Convert.ToInt32(r["lesson_id"]);
            string title = r["lesson_title"].ToString();
            string course = r["course_title"] == DBNull.Value ? "" : r["course_title"].ToString();
            string courseId = r["course_id"] == DBNull.Value ? "" : r["course_id"].ToString();
            string description = r["description"].ToString();
            string content = r["content"].ToString();

            // The video link is saved inside the lesson content
            string video = ReadBlock(content, "video", "url");

            // The paragraph text inside the lesson content
            string text = ReadBlock(content, "paragraph", "content");

            string quizId = r["qid"] == DBNull.Value ? "" : r["qid"].ToString();
            string quizTitle = r["quiz_title"] == DBNull.Value ? "Quiz #" + quizId : r["quiz_title"].ToString();
            string qcount = r["qcount"].ToString();

            StringBuilder sb = new StringBuilder();

            // The row itself (click = open the detail popup)
            sb.Append("<tr class='clickable' id='row" + id + "' onclick='openDetail(" + id + ")'");
            sb.Append(Attr("title", title));
            sb.Append(Attr("course", course));
            sb.Append(Attr("courseid", courseId));
            sb.Append(Attr("description", description));
            sb.Append(Attr("video", video));
            sb.Append(Attr("text", text));
            sb.Append(Attr("quizid", quizId));
            sb.Append(Attr("quiz", quizTitle));
            sb.Append(Attr("qcount", qcount));
            sb.Append(Attr("created", FormatDate(r["created_at"])));
            sb.Append(">");

            // The cells
            sb.Append("<td class='grey-text'>" + id + "</td>");
            sb.Append("<td>" + Server.HtmlEncode(title) + "</td>");
            sb.Append("<td class='grey-text'>" + (course == "" ? "—" : Server.HtmlEncode(course)) + "</td>");

            if (video == "")
                sb.Append("<td><span class='badge inactive'>No video</span></td>");
            else
                sb.Append("<td><span class='badge active'>Video</span></td>");

            if (quizId == "")
                sb.Append("<td><span class='badge pending'>No quiz</span></td>");
            else
                sb.Append("<td><span class='badge blue'>" + qcount + " questions</span></td>");

            sb.Append("<td class='grey-text nowrap'>" + FormatDate(r["created_at"]) + "</td>");

            // The 3 little icons: view, edit and delete
            sb.Append("<td class='actions'>");
            sb.Append(IconButton("blue", "View", "openDetail(" + id + ")",
                "<path d='M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z'/><circle cx='12' cy='12' r='3'/>"));
            sb.Append(IconButton("", "Edit", "openEdit(" + id + ")",
                "<path d='M12 20h9'/><path d='M16.5 3.5a2.1 2.1 0 013 3L7 19l-4 1 1-4z'/>"));
            sb.Append(IconButton("red", "Delete", "openDelete(" + id + ")",
                "<polyline points='3 6 5 6 21 6'/><path d='M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6'/><path d='M10 11v6'/><path d='M14 11v6'/><path d='M9 6V4a1 1 0 011-1h4a1 1 0 011 1v2'/>"));
            sb.Append("</td>");

            sb.Append("</tr>");
            return sb.ToString();
        }


        // =====================================================
        //  BUTTONS
        // =====================================================

        // Search button / dropdown changed  ->  go back to page 1
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

        // ----- Edit popup: Save changes -----
        protected void btnSaveEdit_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(hfEditId.Value, out id)) return;

            int courseId;
            int.TryParse(Request.Form[ddlEditCourse.UniqueID], out courseId);

            string title = txtEditTitle.Text.Trim();
            string description = txtEditDescription.Text.Trim();
            string video = txtEditVideo.Text.Trim();
            string text = txtEditText.Text.Trim();

            string error = CheckLesson(courseId, title, video);
            if (error != "")
            {
                ShowMessage(error, false);
                ReopenPopup("editModal");
                return;
            }

            try
            {
                // Keep everything else inside the lesson content, only change the video and the paragraph
                object old = GetValueOrNull("SELECT lesson_content::text FROM public.lesson WHERE lesson_id = @id", new NpgsqlParameter("@id", id));
                string content = BuildContent(old == null ? "" : old.ToString(), video, text);

                RunSql(@"UPDATE public.lesson
                         SET course_id = @cid, lesson_title = @title, lesson_description = CAST(@desc AS text),
                             lesson_content = CAST(@content AS jsonb)
                         WHERE lesson_id = @id",
                    new NpgsqlParameter("@cid", courseId),
                    new NpgsqlParameter("@title", title),
                    new NpgsqlParameter("@desc", description == "" ? (object)DBNull.Value : description),
                    new NpgsqlParameter("@content", content),
                    new NpgsqlParameter("@id", id));

                ShowMessage("Saved! \"" + title + "\" was updated.", true);
            }
            catch (Exception ex)
            {
                ShowMessage("Could not save: " + ex.Message, false);
                ReopenPopup("editModal");
            }
        }

        // ----- Create lesson popup -----
        protected void btnAddLesson_Click(object sender, EventArgs e)
        {
            lblAddError.Text = "";

            int courseId;
            int.TryParse(Request.Form[ddlAddCourse.UniqueID], out courseId);

            string title = txtAddTitle.Text.Trim();
            string description = txtAddDescription.Text.Trim();
            string video = txtAddVideo.Text.Trim();
            string text = txtAddText.Text.Trim();

            string error = CheckLesson(courseId, title, video);
            if (error != "")
            {
                lblAddError.Text = error;
                ReopenPopup("addModal");
                return;
            }

            try
            {
                string content = BuildContent("", video, text);

                InsertLesson(courseId, title, description, content);

                ShowMessage("Created! \"" + title + "\" was added to the course.", true);

                // clear the boxes
                txtAddTitle.Text = "";
                txtAddDescription.Text = "";
                txtAddVideo.Text = "";
                txtAddText.Text = "";
            }
            catch (Exception ex)
            {
                lblAddError.Text = "Could not create the lesson: " + ex.Message;
                ReopenPopup("addModal");
            }
        }

        // Puts the new lesson in the database.
        // Problem we fixed: "duplicate key value violates unique constraint lesson_pkey".
        // It happens when the lessons were added with their own numbers (imported), so the
        // database counter that gives the NEXT lesson_id is behind and gives a number that is already used.
        //   Try 1: the normal way (the database picks lesson_id).
        //   Try 2: if the number is already used, move the counter to the biggest lesson_id, then try again.
        //   Try 3: if the table has no counter at all, use  biggest lesson_id + 1  ourselves.
        private void InsertLesson(int courseId, string title, string description, string content)
        {
            string normalSql = @"INSERT INTO public.lesson (course_id, lesson_title, lesson_description, lesson_content)
                                 VALUES (@cid, @title, CAST(@desc AS text), CAST(@content AS jsonb))";

            // (the same 4 values are used every time, so we make them in a small list)
            Func<NpgsqlParameter[]> values = delegate ()
            {
                return new NpgsqlParameter[] {
                    new NpgsqlParameter("@cid", courseId),
                    new NpgsqlParameter("@title", title),
                    new NpgsqlParameter("@desc", description == "" ? (object)DBNull.Value : description),
                    new NpgsqlParameter("@content", content)
                };
            };

            try
            {
                RunSql(normalSql, values());                         // Try 1
            }
            catch (PostgresException ex)
            {
                // 23505 = number already used,   23502 = no number was given (no counter)
                if (ex.SqlState != "23505" && ex.SqlState != "23502") throw;

                try
                {
                    // Try 2: move the counter forward (does nothing when there is no counter)
                    RunSql(@"SELECT setval(pg_get_serial_sequence('public.lesson', 'lesson_id'),
                                           (SELECT COALESCE(MAX(lesson_id), 1) FROM public.lesson))
                             WHERE pg_get_serial_sequence('public.lesson', 'lesson_id') IS NOT NULL");
                    RunSql(normalSql, values());
                }
                catch (PostgresException)
                {
                    // Try 3: choose the number ourselves
                    RunSql(@"INSERT INTO public.lesson (lesson_id, course_id, lesson_title, lesson_description, lesson_content)
                             SELECT COALESCE(MAX(lesson_id), 0) + 1, @cid, @title, CAST(@desc AS text), CAST(@content AS jsonb)
                             FROM public.lesson", values());
                }
            }
        }

        // Checks what the admin typed. Gives back "" when everything is fine, or the error text.
        private string CheckLesson(int courseId, string title, string video)
        {
            if (courseId <= 0) return "Please choose a course.";
            if (title == "") return "Please type a lesson title.";

            if (video != "")
            {
                Uri uri;
                if (!Uri.TryCreate(video, UriKind.Absolute, out uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                    return "The video link must start with https://";
            }
            return "";
        }


        // ----- Delete popup: the admin clicked "Confirm" -----
        protected void btnConfirmDelete_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(hfDeleteId.Value, out id)) return;

            // Everything that belongs to a lesson, from the INSIDE to the OUTSIDE.
            // Each line:  { the SQL,  the table.column it needs }   (a line is skipped if the table does not exist)
            // @id = the lesson,   {ids} = the numbers of the quizzes of this lesson
            string postsOfLesson = "(SELECT post_id FROM public.discussion_posts WHERE lesson_id = @id)";

            string[][] belongsToLesson =
            {
                // quizzes and lessons point to each other, so we break those links first
                new string[] { "UPDATE public.quiz SET question_id = NULL WHERE quiz_id IN ({ids})",
                               "quiz.question_id", "quiz.quiz_id" },
                new string[] { "UPDATE public.lesson SET quiz_id = NULL WHERE lesson_id = @id",
                               "lesson.quiz_id" },

                // discussions
                new string[] { "DELETE FROM public.discussion_replies WHERE post_id IN " + postsOfLesson,
                               "discussion_replies.post_id", "discussion_posts.post_id", "discussion_posts.lesson_id" },
                new string[] { "DELETE FROM public.discussion_posts WHERE lesson_id = @id",
                               "discussion_posts.lesson_id" },

                // the quizzes of the lesson: scores, questions, then the quiz itself
                new string[] { "DELETE FROM public.quiz_score WHERE quiz_id IN ({ids})",
                               "quiz_score.quiz_id" },
                new string[] { "DELETE FROM public.quiz_questions WHERE quiz_id IN ({ids})",
                               "quiz_questions.quiz_id" },
                new string[] { "DELETE FROM public.quiz WHERE quiz_id IN ({ids})",
                               "quiz.quiz_id" },

                // what students did in the lesson
                new string[] { "DELETE FROM public.lesson_progress WHERE lesson_id = @id",
                               "lesson_progress.lesson_id" }
            };

            try
            {
                string title;
                List<long> quizIds = new List<long>();

                using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
                {
                    // 1) Is the lesson still there? (and which quizzes belong to it)
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT lesson_title, quiz_id FROM public.lesson WHERE lesson_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ShowMessage("That lesson was not found. Maybe it was already deleted.", false);
                                return;
                            }
                            title = reader[0].ToString();
                            if (!reader.IsDBNull(1)) quizIds.Add(Convert.ToInt64(reader[1]));
                        }
                    }

                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT quiz_id FROM public.quiz WHERE lesson_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                long q = Convert.ToInt64(reader[0]);
                                if (!quizIds.Contains(q)) quizIds.Add(q);
                            }
                        }
                    }

                    // 2) Which of the lines above can we use? (only the tables that exist)
                    List<string> usable = new List<string>();
                    foreach (string[] line in belongsToLesson)
                    {
                        bool allThere = true;
                        for (int i = 1; i < line.Length; i++)
                        {
                            string[] parts = line[i].Split('.');
                            if (!ColumnExists(conn, parts[0], parts[1])) allThere = false;
                        }
                        if (allThere) usable.Add(line[0]);
                    }

                    // 3) Delete everything together. All or nothing (a "transaction"):
                    //    if one step fails, NOTHING is deleted.
                    using (NpgsqlTransaction transaction = conn.BeginTransaction())
                    {
                        string idList = IdList(quizIds);   // the quiz numbers written like:  3, 7, 12

                        foreach (string sql in usable)
                        {
                            // {ids} in the SQL is replaced by the quiz numbers
                            using (NpgsqlCommand cmd = new NpgsqlCommand(sql.Replace("{ids}", idList), conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM public.lesson WHERE lesson_id = @id", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();   // everything worked: save it for real
                    }
                }

                ShowMessage("Deleted! \"" + title + "\" was permanently removed, together with everything linked to it.", true);
            }
            catch (PostgresException ex)
            {
                ShowMessage("Could not delete: " + ex.Message +
                            (string.IsNullOrEmpty(ex.TableName) ? "" : " (table: " + ex.TableName + ")"), false);
            }
            catch (Exception ex)
            {
                ShowMessage("Could not delete: " + ex.Message, false);
            }
        }

        // ----- Logout (the "Logout" option in the drop-down menu) -----
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/auth/login.aspx");
        }


        // =====================================================
        //  THE LESSON CONTENT  (the jsonb column)
        //  It is a list of blocks, for example:
        //     [ {"type":"video","url":"https://..."},  {"type":"paragraph","content":"Some text"} ]
        //  We only touch the "video" and "paragraph" blocks. Other blocks (tables, labs...) stay as they are.
        // =====================================================

        // Reads ONE value from a block. Example: ReadBlock(json, "video", "url")
        private string ReadBlock(string json, string type, string key)
        {
            List<Dictionary<string, object>> blocks;
            if (!TryParseBlocks(json, out blocks)) return "";

            foreach (Dictionary<string, object> b in blocks)
            {
                object t;
                if (b != null && b.TryGetValue("type", out t) &&
                    string.Equals(Convert.ToString(t), type, StringComparison.OrdinalIgnoreCase))
                {
                    object v;
                    if (b.TryGetValue(key, out v) && v != null) return Convert.ToString(v).Trim();
                }
            }
            return "";
        }

        // Gives back the new content text: old blocks + the new video + the new paragraph
        private string BuildContent(string oldJson, string video, string text)
        {
            List<Dictionary<string, object>> blocks;
            if (!TryParseBlocks(oldJson, out blocks))
                throw new Exception("The lesson content is in an unexpected format, so it was not changed.");

            SetBlock(blocks, "video", "url", video, true);
            SetBlock(blocks, "paragraph", "content", text, false);

            return new JavaScriptSerializer().Serialize(blocks);
        }

        // Changes ONE block. Empty value = remove the block.
        private void SetBlock(List<Dictionary<string, object>> blocks, string type, string key, string value, bool putFirst)
        {
            Dictionary<string, object> found = null;
            foreach (Dictionary<string, object> b in blocks)
            {
                object t;
                if (b != null && b.TryGetValue("type", out t) &&
                    string.Equals(Convert.ToString(t), type, StringComparison.OrdinalIgnoreCase))
                {
                    found = b;
                    break;
                }
            }

            if (value == "")
            {
                if (found != null) blocks.Remove(found);
                return;
            }

            if (found == null)
            {
                found = new Dictionary<string, object>();
                found["type"] = type;
                if (putFirst) blocks.Insert(0, found); else blocks.Add(found);
            }
            found[key] = value;
        }

        // Turns the text into a list of blocks. Empty text = an empty list.
        private bool TryParseBlocks(string json, out List<Dictionary<string, object>> blocks)
        {
            blocks = new List<Dictionary<string, object>>();
            if (string.IsNullOrWhiteSpace(json)) return true;

            try
            {
                List<Dictionary<string, object>> list = new JavaScriptSerializer().Deserialize<List<Dictionary<string, object>>>(json);
                if (list == null) return false;
                blocks = list;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }


        // =====================================================
        //  HELPERS  (small tools used again and again)
        // =====================================================

        // Shows a green (ok) or red (error) message at the top
        private void ShowMessage(string text, bool ok)
        {
            litMessage.Text = "<div class='msg " + (ok ? "ok" : "error") + "'>" + Server.HtmlEncode(text) + "</div>";
        }

        // Adds a red box with the REAL error text, when one part of the page cannot load.
        // (the rest of the page still works, and you can read what the database said)
        private void ShowProblem(string part, Exception ex)
        {
            litMessage.Text += "<div class='msg error'>Could not load " + Server.HtmlEncode(part) + ": " + Server.HtmlEncode(ex.Message) + "</div>";
        }

        // Turns a list of numbers into text for the SQL, like:  3, 7, 12
        // (the numbers come from our own database, so it is safe. No quizzes = -1, which matches nothing)
        private string IdList(List<long> ids)
        {
            if (ids.Count == 0) return "-1";

            List<string> parts = new List<string>();
            foreach (long n in ids) parts.Add(n.ToString(CultureInfo.InvariantCulture));
            return string.Join(", ", parts.ToArray());
        }

        // Opens a popup again after a page reload (used when something is wrong)
        private void ReopenPopup(string popupId)
        {
            ClientScript.RegisterStartupScript(GetType(), "reopen", "openModal('" + popupId + "');", true);
        }

        // Writes  data-key='value'  safely
        private string Attr(string key, string value)
        {
            return " data-" + key + "='" + Server.HtmlEncode(value) + "'";
        }

        // Makes one little icon button inside a table row
        private string IconButton(string color, string title, string js, string svgInside)
        {
            return "<button type='button' class='icon-btn " + color + "' title='" + title + "' " +
                   "onclick='event.stopPropagation(); " + js + "'>" +
                   "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
                   svgInside + "</svg></button>";
        }

        // 12 Sep 2026
        private string FormatDate(object value)
        {
            if (value == null || value == DBNull.Value) return "—";
            return Convert.ToDateTime(value).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        }

        // Does this table have this column?
        private bool ColumnExists(NpgsqlConnection conn, string table, string column)
        {
            using (NpgsqlCommand cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @t AND column_name = @c", conn))
            {
                cmd.Parameters.AddWithValue("@t", table);
                cmd.Parameters.AddWithValue("@c", column);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        // 12.5%
        private string Percent(int part, int whole)
        {
            double p = (whole == 0) ? 0 : part * 100.0 / whole;
            return p.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        // Asks the database for ONE value (gives back null when there is nothing)
        private object GetValueOrNull(string sql, params NpgsqlParameter[] parameters)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                object result = cmd.ExecuteScalar();
                return (result == DBNull.Value) ? null : result;
            }
        }

        // Asks the database for ONE number (like a count)
        private int GetNumber(string sql)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Asks the database for a TABLE of rows
        private DataTable GetTable(string sql)
        {
            DataTable table = new DataTable();
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(sql, conn))
            {
                adapter.Fill(table);
            }
            return table;
        }

        // Runs a command that CHANGES the database (insert / update)
        private void RunSql(string sql, params NpgsqlParameter[] parameters)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        // =====================================================
        //  WHO IS LOGGED IN?  (used by every admin page)
        //  The login page saves the user number in Session["UserID"].
        //  (Session["user_id"] is also checked, in case an older page saved it.)
        //  Gives back 0 when nobody is logged in.
        // =====================================================
        private int CurrentUserId()
        {
            int id;
            if (int.TryParse(Convert.ToString(Session["UserID"]), out id) && id > 0) return id;
            if (int.TryParse(Convert.ToString(Session["user_id"]), out id) && id > 0) return id;
            return 0;
        }
    }
}