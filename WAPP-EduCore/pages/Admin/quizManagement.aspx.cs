using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    // NOTE: the class name is quizManagement because the .aspx first line says Inherits="...quizManagement"
    public partial class quizManagement : System.Web.UI.Page
    {
        // These are shown in the sidebar and the top-right circle
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        private const int PageSize = 10;    // how many quizzes per page (change me!)

        // The page number we are on (0 = first page). It is remembered between clicks.
        private int CurrentPage
        {
            get { return ViewState["page"] == null ? 0 : (int)ViewState["page"]; }
            set { ViewState["page"] = value; }
        }

        // The SQL for "which quizzes?"  (used for counting AND for the table)
        //   q = the quiz,  l = its lesson,  c = the course of the lesson
        //   k.n = how many questions,  s.n = how many times students did the quiz
        private string FromAndWhere()
        {
            // If the quiz_score table has no quiz_id column, we just say "0 attempts"
            string scoreJoin = ColumnExists("quiz_score", "quiz_id")
                ? "LEFT JOIN (SELECT quiz_id, COUNT(*) AS n FROM public.quiz_score GROUP BY quiz_id) s ON s.quiz_id = q.quiz_id "
                : "LEFT JOIN (SELECT 0 AS quiz_id, 0 AS n WHERE false) s ON s.quiz_id = q.quiz_id ";

            return
                @" FROM public.quiz q
                   LEFT JOIN public.lesson l
                          ON l.lesson_id = COALESCE(q.lesson_id, (SELECT MIN(x.lesson_id) FROM public.lesson x WHERE x.quiz_id = q.quiz_id))
                   LEFT JOIN public.courses c ON c.course_id = l.course_id
                   LEFT JOIN (SELECT quiz_id, COUNT(*) AS n FROM public.quiz_questions GROUP BY quiz_id) k ON k.quiz_id = q.quiz_id "
                + scoreJoin +
                @" WHERE (@search = '' OR q.quiz_title ILIKE @like OR l.lesson_title ILIKE @like OR c.title ILIKE @like)
                     AND (@course = 0 OR l.course_id = @course)
                     AND (@qs = ''
                          OR (@qs = 'with'    AND COALESCE(k.n, 0) > 0)
                          OR (@qs = 'without' AND COALESCE(k.n, 0) = 0)) ";
        }


        // =====================================================
        //  STEP 1: Page opens  ->  find out who the admin is
        // =====================================================
        protected void Page_Load(object sender, EventArgs e)
        {
            // Not logged in, or the session ran out (for example after resting for a while)?
            // Then go to the login page. We do NOT guess an admin any more.
            int signedInAdmin = CurrentUserId();
            if (signedInAdmin == 0)
            {
                Response.Redirect("~/Default.aspx");     // the first page of the website (it has the Log In button)
                return;
            }

            // Remember this admin in the browser until he/she clicks Logout
            WAPP_EduCore.Data.LoginKeeper.Remember(Context, signedInAdmin);

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
            try { FillDropdowns(); } catch (Exception ex) { ShowProblem("the lists", ex); }          // course filter + lesson lists
            try { LoadStats(); } catch (Exception ex) { ShowProblem("the number boxes", ex); }      // the 5 number boxes
            try { LoadQuizzes(); } catch (Exception ex) { ShowProblem("the quiz table", ex); }      // the table + the hidden questions
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
        //  THE DROPDOWNS
        // =====================================================
        private void FillDropdowns()
        {
            // 1) the course filter above the table
            DataTable courses = GetTable("SELECT course_id, title FROM public.courses ORDER BY title");

            string keep = ddlCourse.SelectedValue;
            ddlCourse.Items.Clear();
            ddlCourse.Items.Add(new ListItem("All courses", "0"));
            foreach (DataRow r in courses.Rows)
                ddlCourse.Items.Add(new ListItem(r["title"].ToString(), r["course_id"].ToString()));
            if (ddlCourse.Items.FindByValue(keep) != null) ddlCourse.SelectedValue = keep;

            // 2) the lesson lists in the popups
            string lessonSql =
                @"SELECT l.lesson_id, l.lesson_title, c.title AS course_title
                  FROM public.lesson l
                  LEFT JOIN public.courses c ON c.course_id = l.course_id ";

            // Create popup: only the lessons that do NOT have a quiz yet
            DataTable free = GetTable(lessonSql +
                @"WHERE l.quiz_id IS NULL
                    AND NOT EXISTS (SELECT 1 FROM public.quiz z WHERE z.lesson_id = l.lesson_id)
                  ORDER BY c.title, l.lesson_id");
            FillLessonList(ddlAddLesson, free, "Select a lesson");

            // Edit popup: all the lessons (the page checks that the new lesson is free)
            DataTable all = GetTable(lessonSql + "ORDER BY c.title, l.lesson_id");
            FillLessonList(ddlEditLesson, all, null);
        }

        // Puts lessons in ONE dropdown, written like:  Course name  -  Lesson name
        private void FillLessonList(DropDownList list, DataTable lessons, string firstText)
        {
            string keep = list.SelectedValue;
            list.Items.Clear();

            if (firstText != null) list.Items.Add(new ListItem(firstText, ""));

            foreach (DataRow r in lessons.Rows)
            {
                string course = r["course_title"] == DBNull.Value ? "(no course)" : r["course_title"].ToString();
                list.Items.Add(new ListItem(course + "  -  " + r["lesson_title"], r["lesson_id"].ToString()));
            }

            if (list.Items.FindByValue(keep) != null) list.SelectedValue = keep;
        }


        // =====================================================
        //  THE 5 NUMBER BOXES
        // =====================================================
        private void LoadStats()
        {
            int total = GetNumber("SELECT COUNT(*) FROM public.quiz");
            int questions = GetNumber("SELECT COUNT(*) FROM public.quiz_questions");
            int noQuestions = GetNumber("SELECT COUNT(*) FROM public.quiz q WHERE NOT EXISTS (SELECT 1 FROM public.quiz_questions k WHERE k.quiz_id = q.quiz_id)");
            int lessonsWithQuiz = GetNumber("SELECT COUNT(DISTINCT lesson_id) FROM public.quiz WHERE lesson_id IS NOT NULL");
            int lessons = GetNumber("SELECT COUNT(*) FROM public.lesson");
            int pass = GetNumber("SELECT COALESCE(ROUND(AVG(passing_score)), 0) FROM public.quiz");
            int attempts = ColumnExists("quiz_score", "quiz_id") ? GetNumber("SELECT COUNT(*) FROM public.quiz_score") : 0;

            lblTotal.Text = total.ToString("N0");
            lblTotalNote.Text = noQuestions + " without questions";

            lblQuestions.Text = questions.ToString("N0");
            double perQuiz = (total == 0) ? 0 : (double)questions / total;
            lblQuestionsNote.Text = perQuiz.ToString("0.0", CultureInfo.InvariantCulture) + " per quiz";

            lblLessons.Text = lessonsWithQuiz.ToString("N0");
            lblLessonsNote.Text = Percent(lessonsWithQuiz, lessons) + " of lessons";

            lblPass.Text = pass + "%";

            lblAttempts.Text = attempts.ToString("N0");
        }


        // =====================================================
        //  THE QUIZ TABLE  (search + filters + pages)
        // =====================================================
        private void LoadQuizzes()
        {
            string from = FromAndWhere();

            // 1) How many quizzes match the search?
            int total;
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*)" + from, conn))
            {
                AddFilters(cmd);
                total = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 2) Make sure the page number is not too big
            int lastPage = Math.Max(0, (total - 1) / PageSize);
            if (CurrentPage > lastPage) CurrentPage = lastPage;
            if (CurrentPage < 0) CurrentPage = 0;

            // 3) Get only the quizzes for this page
            DataTable table = new DataTable();
            string sql =
                @"SELECT q.quiz_id, q.quiz_title, COALESCE(q.passing_score, 0) AS pass,
                         l.lesson_id, l.lesson_title, c.title AS course_title, q.created_at,
                         COALESCE(k.n, 0) AS qcount, COALESCE(s.n, 0) AS attempts"
                + from +
                @" ORDER BY q.quiz_id ASC
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
            List<long> ids = new List<long>();
            foreach (DataRow r in table.Rows)
            {
                rows.Append(BuildRow(r));
                ids.Add(Convert.ToInt64(r["quiz_id"]));
            }
            litRows.Text = rows.ToString();

            // 5) The questions of these quizzes (hidden in the page, the popup reads them)
            litQuestions.Text = BuildHiddenQuestions(ids);

            // 6) Show the table OR the "No results found" box
            pnlTable.Visible = (total > 0);
            pnlEmpty.Visible = (total == 0);

            // 7) "Showing 1-10 of 40" + Previous / Next buttons
            int first = CurrentPage * PageSize + 1;
            int last = CurrentPage * PageSize + table.Rows.Count;
            lblShowing.Text = "Showing " + first + "–" + last + " of " + total.ToString("N0");

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
            cmd.Parameters.AddWithValue("@qs", ddlQuestionFilter.SelectedValue);
        }

        // Makes ONE table row.
        private string BuildRow(DataRow r)
        {
            int id = Convert.ToInt32(r["quiz_id"]);
            string title = r["quiz_title"] == DBNull.Value ? "Untitled quiz" : r["quiz_title"].ToString();
            string lesson = r["lesson_title"] == DBNull.Value ? "" : r["lesson_title"].ToString();
            string lessonId = r["lesson_id"] == DBNull.Value ? "" : r["lesson_id"].ToString();
            string course = r["course_title"] == DBNull.Value ? "" : r["course_title"].ToString();
            string pass = r["pass"].ToString();
            int qcount = Convert.ToInt32(r["qcount"]);
            string attempts = r["attempts"].ToString();

            StringBuilder sb = new StringBuilder();

            // The row itself (click = open the quiz popup with its questions)
            sb.Append("<tr class='clickable' id='row" + id + "' onclick='openDetail(" + id + ")'");
            sb.Append(Attr("title", title));
            sb.Append(Attr("lesson", lesson));
            sb.Append(Attr("lessonid", lessonId));
            sb.Append(Attr("course", course));
            sb.Append(Attr("pass", pass));
            sb.Append(Attr("attempts", attempts));
            sb.Append(">");

            // The cells
            sb.Append("<td class='grey-text'>" + id + "</td>");
            sb.Append("<td>" + Server.HtmlEncode(title) + "</td>");
            sb.Append("<td class='grey-text'>" + (lesson == "" ? "—" : Server.HtmlEncode(lesson)) + "</td>");
            sb.Append("<td class='grey-text'>" + (course == "" ? "—" : Server.HtmlEncode(course)) + "</td>");

            // 0 questions gets a yellow badge so it stands out
            if (qcount == 0)
                sb.Append("<td class='center'><span class='badge pending'>No questions</span></td>");
            else
                sb.Append("<td class='center'>" + qcount + "</td>");

            sb.Append("<td class='center'>" + pass + "%</td>");
            sb.Append("<td class='center grey-text'>" + attempts + "</td>");

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

        // Hides the questions of the quizzes on this page inside the page, like:
        //   <div id='qs12'>  <span id='qd55' data-text='...' data-a='...' ...></span> ...  </div>
        // The quiz popup reads them with JavaScript.
        private string BuildHiddenQuestions(List<long> quizIds)
        {
            if (quizIds.Count == 0) return "";

            // one empty box for every quiz (so a quiz without questions also has a box)
            Dictionary<long, StringBuilder> boxes = new Dictionary<long, StringBuilder>();
            foreach (long id in quizIds) boxes[id] = new StringBuilder();

            // The quiz numbers are written straight into the SQL, like:  3, 7, 12
            // (they come from our own database, so it is safe)
            List<string> idParts = new List<string>();
            foreach (long n in quizIds) idParts.Add(n.ToString(CultureInfo.InvariantCulture));

            DataTable questions = new DataTable();
            try
            {
                questions = GetTable(
                    @"SELECT question_id, quiz_id, question_text,
                             option_a, option_b, option_c, option_d,
                             correct_answer::text AS correct_answer, explanation,
                             COALESCE(difficulty::text, '') AS difficulty, COALESCE(points::text, '') AS points
                      FROM public.quiz_questions
                      WHERE quiz_id IN (" + string.Join(", ", idParts.ToArray()) + @")
                      ORDER BY quiz_id, question_id");
            }
            catch (Exception ex)
            {
                // the quiz table still shows, and a red box tells what went wrong with the questions
                ShowProblem("the questions", ex);
            }

            foreach (DataRow r in questions.Rows)
            {
                long quizId = Convert.ToInt64(r["quiz_id"]);
                StringBuilder sb = boxes[quizId];

                sb.Append("<span id='qd" + r["question_id"] + "' data-id='" + r["question_id"] + "'");
                sb.Append(Attr("text", Text(r["question_text"])));
                sb.Append(Attr("a", Text(r["option_a"])));
                sb.Append(Attr("b", Text(r["option_b"])));
                sb.Append(Attr("c", Text(r["option_c"])));
                sb.Append(Attr("d", Text(r["option_d"])));
                sb.Append(Attr("correct", LetterOf(Text(r["correct_answer"]), Text(r["option_a"]), Text(r["option_b"]), Text(r["option_c"]), Text(r["option_d"]))));
                sb.Append(Attr("explain", Text(r["explanation"])));
                sb.Append(Attr("difficulty", Text(r["difficulty"])));
                sb.Append(Attr("points", Text(r["points"])));
                sb.Append("></span>");
            }

            StringBuilder all = new StringBuilder();
            foreach (long id in quizIds)
            {
                all.Append("<div id='qs" + id + "'>" + boxes[id] + "</div>");
            }
            return all.ToString();
        }

        // The column correct_answer holds the right answer. It is usually a letter (A, B, C or D),
        // but if it holds the answer TEXT instead, we find which option it is. Gives "" when unknown.
        private string LetterOf(string saved, string a, string b, string c, string d)
        {
            saved = saved.Trim();
            if (saved == "") return "";

            if (saved.Length == 1 && "ABCDabcd".IndexOf(saved) >= 0) return saved.ToUpper();

            string[] options = { a, b, c, d };
            string[] letters = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                if (options[i] != "" && string.Equals(options[i].Trim(), saved, StringComparison.OrdinalIgnoreCase)) return letters[i];
            }
            return "";
        }

        // A value for a column whose type we do not want to guess ("Unknown" = the database decides how to read it)
        private NpgsqlParameter Guess(string name, string value)
        {
            NpgsqlParameter p = new NpgsqlParameter(name, NpgsqlDbType.Unknown);
            p.Value = value;
            return p;
        }

        // A database value as text ("" when empty)
        private string Text(object value)
        {
            return value == DBNull.Value ? "" : value.ToString();
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

        // ----- Create quiz popup -----
        protected void btnAddQuiz_Click(object sender, EventArgs e)
        {
            lblAddError.Text = "";

            string title = txtAddTitle.Text.Trim();
            int lessonId;
            int.TryParse(Request.Form[ddlAddLesson.UniqueID], out lessonId);

            int pass;
            string error = CheckQuiz(title, lessonId, txtAddPass.Text, out pass);
            if (error == "" && LessonHasOtherQuiz(lessonId, 0))
                error = "That lesson already has a quiz. A lesson can only have one quiz.";

            if (error != "")
            {
                lblAddError.Text = error;
                ReopenScript("openModal('addModal');");
                return;
            }

            try
            {
                using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
                using (NpgsqlTransaction transaction = conn.BeginTransaction())
                {
                    // 1) the new quiz
                    long newId;
                    using (NpgsqlCommand cmd = new NpgsqlCommand(
                        "INSERT INTO public.quiz (quiz_title, passing_score, lesson_id) VALUES (@title, @pass, @lesson) RETURNING quiz_id", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@pass", pass);
                        cmd.Parameters.AddWithValue("@lesson", lessonId);
                        newId = Convert.ToInt64(cmd.ExecuteScalar());
                    }

                    // 2) tell the lesson about its quiz
                    using (NpgsqlCommand cmd = new NpgsqlCommand("UPDATE public.lesson SET quiz_id = @quiz WHERE lesson_id = @lesson", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@quiz", newId);
                        cmd.Parameters.AddWithValue("@lesson", lessonId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    ShowMessage("Created! \"" + title + "\" was added. Click it to add questions.", true);
                }

                txtAddTitle.Text = "";
                txtAddPass.Text = "50";
            }
            catch (Exception ex)
            {
                lblAddError.Text = "Could not create the quiz: " + ex.Message;
                ReopenScript("openModal('addModal');");
            }
        }

        // ----- Edit quiz popup: Save changes -----
        protected void btnSaveEdit_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(hfEditId.Value, out id)) return;

            string title = txtEditTitle.Text.Trim();
            int lessonId;
            int.TryParse(Request.Form[ddlEditLesson.UniqueID], out lessonId);

            int pass;
            string error = CheckQuiz(title, lessonId, txtEditPass.Text, out pass);
            if (error == "" && LessonHasOtherQuiz(lessonId, id))
                error = "That lesson already has another quiz. A lesson can only have one quiz.";

            if (error != "")
            {
                ShowMessage(error, false);
                ReopenScript("openModal('editModal');");
                return;
            }

            try
            {
                using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
                using (NpgsqlTransaction transaction = conn.BeginTransaction())
                {
                    // 1) the old lesson forgets this quiz
                    using (NpgsqlCommand cmd = new NpgsqlCommand("UPDATE public.lesson SET quiz_id = NULL WHERE quiz_id = @id", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    // 2) the quiz itself
                    using (NpgsqlCommand cmd = new NpgsqlCommand(
                        "UPDATE public.quiz SET quiz_title = @title, passing_score = @pass, lesson_id = @lesson WHERE quiz_id = @id", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@pass", pass);
                        cmd.Parameters.AddWithValue("@lesson", lessonId);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    // 3) the (new) lesson knows its quiz
                    using (NpgsqlCommand cmd = new NpgsqlCommand("UPDATE public.lesson SET quiz_id = @id WHERE lesson_id = @lesson", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@lesson", lessonId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }

                ShowMessage("Saved! \"" + title + "\" was updated.", true);
            }
            catch (Exception ex)
            {
                ShowMessage("Could not save: " + ex.Message, false);
                ReopenScript("openModal('editModal');");
            }
        }

        // Checks the title, the lesson and the passing score. Gives back "" when everything is fine.
        private string CheckQuiz(string title, int lessonId, string passText, out int pass)
        {
            pass = 0;
            if (title == "") return "Please type a quiz title.";
            if (lessonId <= 0) return "Please choose a lesson.";
            if (!int.TryParse(passText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out pass) || pass > 100)
                return "The passing score must be a whole number from 0 to 100.";
            return "";
        }

        // Does this lesson already have a quiz that is NOT the quiz "exceptId"?
        private bool LessonHasOtherQuiz(int lessonId, int exceptId)
        {
            int count = GetNumberWith(
                @"SELECT (SELECT COUNT(*) FROM public.quiz WHERE lesson_id = @lesson AND quiz_id <> @except)
                       + (SELECT COUNT(*) FROM public.lesson WHERE lesson_id = @lesson AND quiz_id IS NOT NULL AND quiz_id <> @except)",
                new NpgsqlParameter("@lesson", lessonId),
                new NpgsqlParameter("@except", exceptId));
            return count > 0;
        }


        // ----- Delete quiz popup: the admin clicked "Confirm" -----
        protected void btnConfirmDelete_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(hfDeleteId.Value, out id)) return;

            // Everything that belongs to a quiz, from the INSIDE to the OUTSIDE.
            // Each line:  { the SQL,  the table.column it needs }   (a line is skipped if the table does not exist)
            string[][] belongsToQuiz =
            {
                new string[] { "UPDATE public.quiz SET question_id = NULL WHERE quiz_id = @id", "quiz.question_id" },
                new string[] { "UPDATE public.lesson SET quiz_id = NULL WHERE quiz_id = @id",   "lesson.quiz_id" },
                new string[] { "DELETE FROM public.quiz_score WHERE quiz_id = @id",             "quiz_score.quiz_id" },
                new string[] { "DELETE FROM public.quiz_questions WHERE quiz_id = @id",         "quiz_questions.quiz_id" }
            };

            try
            {
                using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
                {
                    // 1) Is the quiz still there?
                    object found;
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT quiz_title FROM public.quiz WHERE quiz_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        found = cmd.ExecuteScalar();
                    }

                    if (found == null)
                    {
                        ShowMessage("That quiz was not found. Maybe it was already deleted.", false);
                        return;
                    }
                    string title = found == DBNull.Value ? "The quiz" : found.ToString();

                    // 2) Which of the lines above can we use?
                    List<string> usable = new List<string>();
                    foreach (string[] line in belongsToQuiz)
                    {
                        bool allThere = true;
                        for (int i = 1; i < line.Length; i++)
                        {
                            string[] parts = line[i].Split('.');
                            if (!ColumnExists(conn, parts[0], parts[1])) allThere = false;
                        }
                        if (allThere) usable.Add(line[0]);
                    }

                    // 3) Delete everything together. All or nothing (a "transaction").
                    using (NpgsqlTransaction transaction = conn.BeginTransaction())
                    {
                        foreach (string sql in usable)
                        {
                            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM public.quiz WHERE quiz_id = @id", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }

                    ShowMessage("Deleted! \"" + title + "\" was permanently removed, together with its questions and scores.", true);
                }
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


        // ----- Question popup: Save question (new or changed) -----
        protected void btnSaveQuestion_Click(object sender, EventArgs e)
        {
            int questionId, quizId;
            int.TryParse(hfQId.Value, out questionId);
            if (!int.TryParse(hfQQuiz.Value, out quizId)) return;

            string text = txtQText.Text.Trim();
            string a = txtQA.Text.Trim();
            string b = txtQB.Text.Trim();
            string c = txtQC.Text.Trim();
            string d = txtQD.Text.Trim();
            string correct = ddlQCorrect.SelectedValue;
            string explanation = txtQExplain.Text.Trim();
            string difficulty = ddlQDifficulty.SelectedValue;
            int points = 0;

            // Check what the admin typed
            string error = "";

            if (text == "") error = "Please type the question.";
            else if (a == "" || b == "") error = "Answer A and answer B are needed.";
            else if ((correct == "A" && a == "") || (correct == "B" && b == "") || (correct == "C" && c == "") || (correct == "D" && d == ""))
                error = "The correct answer must be one of the answers you filled in.";
            else if (!int.TryParse(txtQPoints.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out points) || points > 1000)
                error = "Points must be a whole number from 0 to 1000.";

            if (error != "")
            {
                ShowMessage(error, false);
                ReopenScript("show('qTitle', '" + (questionId == 0 ? "Add Question" : "Edit Question") + "'); openModal('questionModal');");
                return;
            }

            try
            {
                List<NpgsqlParameter> p = new List<NpgsqlParameter>();
                p.Add(new NpgsqlParameter("@text", text));
                p.Add(new NpgsqlParameter("@a", a));
                p.Add(new NpgsqlParameter("@b", b));
                p.Add(new NpgsqlParameter("@c", c == "" ? (object)DBNull.Value : c));
                p.Add(new NpgsqlParameter("@d", d == "" ? (object)DBNull.Value : d));
                p.Add(Guess("@answer", correct));
                p.Add(new NpgsqlParameter("@explain", explanation == "" ? (object)DBNull.Value : explanation));
                p.Add(Guess("@diff", difficulty));
                p.Add(Guess("@points", points.ToString(CultureInfo.InvariantCulture)));

                if (questionId == 0)
                {
                    p.Add(new NpgsqlParameter("@quiz", quizId));
                    RunSql(@"INSERT INTO public.quiz_questions
                                (quiz_id, question_text, option_a, option_b, option_c, option_d, correct_answer, explanation, difficulty, points)
                             VALUES (@quiz, @text, @a, @b, CAST(@c AS text), CAST(@d AS text), @answer, CAST(@explain AS text), @diff, @points)", p.ToArray());
                    ShowMessage("Question added.", true);
                }
                else
                {
                    p.Add(new NpgsqlParameter("@id", questionId));
                    RunSql(@"UPDATE public.quiz_questions
                             SET question_text = @text,
                                 option_a = @a, option_b = @b, option_c = CAST(@c AS text), option_d = CAST(@d AS text),
                                 correct_answer = @answer, explanation = CAST(@explain AS text),
                                 difficulty = @diff, points = @points
                             WHERE question_id = @id", p.ToArray());
                    ShowMessage("Question saved.", true);
                }

                // Go back to the popup we came from (Edit Quiz or Quiz detail), so the admin sees the questions
                ReopenScript(BackTo(hfQFrom.Value, quizId));
            }
            catch (Exception ex)
            {
                ShowMessage("Could not save the question: " + ex.Message, false);
                ReopenScript("show('qTitle', '" + (questionId == 0 ? "Add Question" : "Edit Question") + "'); openModal('questionModal');");
            }
        }

        // ----- Delete ONE question: the admin clicked "Confirm" -----
        protected void btnConfirmQuestionDelete_Click(object sender, EventArgs e)
        {
            int questionId, quizId;
            if (!int.TryParse(hfQDeleteId.Value, out questionId)) return;
            int.TryParse(hfQDeleteQuiz.Value, out quizId);

            try
            {
                using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
                {
                    // the quiz may point to this question, so it forgets it first
                    if (ColumnExists(conn, "quiz", "question_id"))
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand("UPDATE public.quiz SET question_id = NULL WHERE question_id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", questionId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM public.quiz_questions WHERE question_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", questionId);
                        cmd.ExecuteNonQuery();
                    }
                }

                ShowMessage("Question deleted.", true);
            }
            catch (Exception ex)
            {
                ShowMessage("Could not delete the question: " + ex.Message, false);
            }

            ReopenScript(BackTo(hfQDeleteFrom.Value, quizId));
        }

        // ----- Logout (the "Logout" option in the drop-down menu) -----
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            WAPP_EduCore.Data.LoginKeeper.Forget(Context);   // remove the "remember me" cookie
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/auth/login.aspx");
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

        // Runs a little JavaScript after the page reloads (used to open a popup again)
        private void ReopenScript(string script)
        {
            ClientScript.RegisterStartupScript(GetType(), "reopen", script, true);
        }

        // The script that opens the popup we came from: the Edit Quiz popup or the Quiz detail popup
        private string BackTo(string from, int quizId)
        {
            return (from == "edit" ? "openEdit(" : "openDetail(") + quizId + ");";
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

        // Does this table have this column? (opens its own connection)
        private bool ColumnExists(string table, string column)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            {
                return ColumnExists(conn, table, column);
            }
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

        // 12 Sep 2026
        private string FormatDate(object value)
        {
            if (value == null || value == DBNull.Value) return "—";
            return Convert.ToDateTime(value).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        }

        // 12.5%
        private string Percent(int part, int whole)
        {
            double p = (whole == 0) ? 0 : part * 100.0 / whole;
            return p.ToString("0.0", CultureInfo.InvariantCulture) + "%";
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

        // Same, but with parameters
        private int GetNumberWith(string sql, params NpgsqlParameter[] parameters)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
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

            // The session was forgotten (idle for a long time)? Ask the "remember me" cookie who the admin is.
            return WAPP_EduCore.Data.LoginKeeper.Restore(Context);
        }
    }
}