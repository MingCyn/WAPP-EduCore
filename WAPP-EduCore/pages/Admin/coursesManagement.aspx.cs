using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Npgsql;
using NpgsqlTypes;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    // NOTE: the class name is WebForm4 because your .aspx first line says Inherits="...WebForm4"
    public partial class WebForm4 : System.Web.UI.Page
    {
        // These are shown in the sidebar and the top-right circle
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        private int adminId = 0;            // the id of the admin who is logged in
        private const int PageSize = 10;    // how many courses per page (change me!)

        // The page number we are on (0 = first page). It is remembered between clicks.
        private int CurrentPage
        {
            get { return ViewState["page"] == null ? 0 : (int)ViewState["page"]; }
            set { ViewState["page"] = value; }
        }

        // The SQL for "which courses?"  (used for counting AND for the table)
        // c = the course,  l.n = how many lessons that course has
        private const string FromAndWhere =
            @" FROM public.courses c
               LEFT JOIN (SELECT course_id, COUNT(*) AS n FROM public.lesson GROUP BY course_id) l
                      ON l.course_id = c.course_id
               WHERE (@search = '' OR c.title ILIKE @like OR c.category ILIKE @like)
                 AND (@topic = '' OR c.category = @topic)
                 AND (@lessons = ''
                      OR (@lessons = 'with'    AND COALESCE(l.n, 0) > 0)
                      OR (@lessons = 'without' AND COALESCE(l.n, 0) = 0)) ";

        // The name of the "description" column in the courses table.
        // We look for it in the database, because it may be called differently.
        private string descriptionColumn = null;
        private bool descriptionChecked = false;


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
            // Show the Description boxes only if the table has a description column
            bool hasDescription = (DescriptionColumn() != null);
            pnlAddDesc.Visible = hasDescription;
            pnlEditDesc.Visible = hasDescription;

            RefreshTopicList();   // topics in the dropdown + the suggestion list
            LoadStats();          // the 5 number boxes
            LoadCourses();        // the table
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
                        adminId = Convert.ToInt32(reader[0]);
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
        //  THE TOPIC DROPDOWN  +  the suggestion list in the popups
        // =====================================================
        private void RefreshTopicList()
        {
            string keep = ddlTopic.SelectedValue;     // remember what was chosen
            string keepAdd = ddlAddTopic.SelectedValue;     // the Create popup dropdown remembers its choice too
            string keepEdit = ddlEditTopic.SelectedValue;   // the Edit popup dropdown remembers its choice too

            DataTable topics = GetTable("SELECT DISTINCT category FROM public.courses WHERE category IS NOT NULL AND category <> '' ORDER BY category");

            ddlTopic.Items.Clear();
            ddlTopic.Items.Add(new ListItem("All topics", ""));

            ddlAddTopic.Items.Clear();
            ddlAddTopic.Items.Add(new ListItem("Select topic", ""));

            ddlEditTopic.Items.Clear();
            ddlEditTopic.Items.Add(new ListItem("Select topic", ""));

            StringBuilder options = new StringBuilder();
            foreach (DataRow r in topics.Rows)
            {
                string topic = r["category"].ToString();
                ddlTopic.Items.Add(new ListItem(topic, topic));
                ddlAddTopic.Items.Add(new ListItem(topic, topic));    // same topics in the Create popup
                ddlEditTopic.Items.Add(new ListItem(topic, topic));   // same topics in the Edit popup
                options.Append("<option value='" + Server.HtmlEncode(topic) + "'></option>");
            }

            if (ddlTopic.Items.FindByValue(keep) != null)
                ddlTopic.SelectedValue = keep;        // choose it again

            if (ddlAddTopic.Items.FindByValue(keepAdd) != null)
                ddlAddTopic.SelectedValue = keepAdd;

            if (ddlEditTopic.Items.FindByValue(keepEdit) != null)
                ddlEditTopic.SelectedValue = keepEdit;

            litTopicOptions.Text = options.ToString();
        }


        // =====================================================
        //  THE 5 NUMBER BOXES
        // =====================================================
        private void LoadStats()
        {
            int total = GetNumber("SELECT COUNT(*) FROM public.courses");
            int topics = GetNumber("SELECT COUNT(DISTINCT category) FROM public.courses");

            // A course belongs to a subject when the subject is in its title or in its topic
            int biology = CountSubject("biology");
            int chemistry = CountSubject("chemistry");
            int physics = CountSubject("physics");

            lblTotal.Text = total.ToString("N0");
            lblTotalNote.Text = "Across " + topics + " topics";
            lblTopics.Text = topics.ToString("N0");

            lblBiology.Text = biology.ToString("N0");
            lblBiologyNote.Text = Percent(biology, total) + " of all courses";

            lblChemistry.Text = chemistry.ToString("N0");
            lblChemistryNote.Text = Percent(chemistry, total) + " of all courses";

            lblPhysics.Text = physics.ToString("N0");
            lblPhysicsNote.Text = Percent(physics, total) + " of all courses";
        }

        // How many courses have this subject word in the title or the topic?
        private int CountSubject(string subject)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM public.courses WHERE title ILIKE @word OR category ILIKE @word", conn))
            {
                cmd.Parameters.AddWithValue("@word", "%" + subject + "%");
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }


        // =====================================================
        //  THE COURSE TABLE  (search + filters + pages)
        // =====================================================
        private void LoadCourses()
        {
            // 1) How many courses match the search?
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

            // 3) Get only the 10 courses for this page
            //    (duration::text = show the duration as plain text, whatever type it is)
            //    (img = the picture link, but only if it looks like a real link)
            //    (description = the description text, or empty when there is no such column)
            string descriptionPart = (DescriptionColumn() == null)
                ? "''"
                : "COALESCE(c." + DescriptionColumn() + "::text, '')";

            // level = the course_level column (SPM / IGCSE), or empty when there is no such column
            string levelPart = ColumnExists("course_level") ? "COALESCE(c.course_level::text, '')" : "''";

            // status = "draft" or "published" (empty when there is no such column)
            string statusPart = ColumnExists("status") ? "COALESCE(c.status::text, '')" : "''";

            // reference = the list of books, saved as text like  ["Book one", "Book two"]
            string referencePart = ColumnExists("reference") ? "COALESCE(c.\"reference\"::text, '')" : "''";

            DataTable table = new DataTable();
            string sql =
                @"SELECT c.course_id, c.title, c.category, c.duration::text AS duration, c.created_at,
                         COALESCE(l.n, 0) AS lessons,
                         CASE WHEN c.course_img::text ~ '^(https?://|/|~/)' THEN c.course_img::text ELSE '' END AS img,
                         " + descriptionPart + @" AS description,
                         " + levelPart + @" AS level,
                         " + statusPart + @" AS status,
                         " + referencePart + @" AS reference"
                + FromAndWhere +
                @" ORDER BY c.course_id ASC
                   LIMIT @size OFFSET @skip";   // ASC = smallest first (1, 2, 3...)

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

            // 6) "Showing 1-10 of 486" + Previous / Next buttons
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
            cmd.Parameters.AddWithValue("@search", search);
            cmd.Parameters.AddWithValue("@like", "%" + search + "%");
            cmd.Parameters.AddWithValue("@topic", ddlTopic.SelectedValue);
            cmd.Parameters.AddWithValue("@lessons", ddlLessons.SelectedValue);
        }

        // Makes ONE table row.
        // The data-... parts hide the course's info inside the row,
        // so the popup can read it with JavaScript when you click.
        private string BuildRow(DataRow r)
        {
            int id = Convert.ToInt32(r["course_id"]);
            string title = r["title"].ToString();
            string topic = r["category"] == DBNull.Value ? "" : r["category"].ToString();
            string duration = r["duration"] == DBNull.Value ? "" : r["duration"].ToString();
            string description = r["description"].ToString();
            int lessons = Convert.ToInt32(r["lessons"]);

            // "published" stays published, everything else counts as a draft
            string status = (r["status"].ToString().Trim().ToLower() == "published") ? "published" : "draft";

            string img = r["img"].ToString();
            if (img.StartsWith("~/")) img = ResolveUrl(img);      // ~/images/a.png  ->  /images/a.png

            StringBuilder sb = new StringBuilder();

            // The row itself (click = open the detail popup)
            sb.Append("<tr class='clickable' id='row" + id + "' onclick='openDetail(" + id + ")'");
            sb.Append(Attr("title", title));
            sb.Append(Attr("topic", topic));
            sb.Append(Attr("duration", duration));
            sb.Append(Attr("description", description));
            sb.Append(Attr("level", r["level"].ToString()));
            sb.Append(Attr("lessons", lessons.ToString()));
            sb.Append(Attr("status", status));
            sb.Append(Attr("reference", r["reference"].ToString()));
            sb.Append(Attr("created", FormatDate(r["created_at"])));
            sb.Append(Attr("img", img));
            sb.Append(">");

            // The cells
            sb.Append("<td class='grey-text'>" + id + "</td>");
            sb.Append("<td>" + Server.HtmlEncode(title) + "</td>");
            sb.Append("<td>" + (topic == "" ? "—" : Server.HtmlEncode(topic)) + "</td>");
            sb.Append("<td>" + (duration == "" ? "—" : Server.HtmlEncode(duration)) + "</td>");

            // 0 lessons gets a yellow badge so it stands out
            if (lessons == 0)
                sb.Append("<td><span class='badge pending'>No lessons</span></td>");
            else
                sb.Append("<td>" + lessons + "</td>");

            // Status: green "Published" or red "Draft"
            if (status == "published")
                sb.Append("<td><span class='badge published'>Published</span></td>");
            else
                sb.Append("<td><span class='badge draft'>Draft</span></td>");

            sb.Append("<td class='grey-text'>" + FormatDate(r["created_at"]) + "</td>");

            // The 3 little icons: view, edit and delete
            sb.Append("<td>");
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

            string name = txtEditTitle.Text.Trim();
            string topic = ddlEditTopic.SelectedValue.Trim();   // picked from the dropdown
            string description = txtEditDescription.Text.Trim();

            if (name == "")
            {
                ShowMessage("The course name cannot be empty.", false);
                ReopenPopup("editModal");
                return;
            }

            // The title is saved like  "SPM Biology: Cell Physiology & Organization"
            string title = BuildTitle(ddlEditLevel.SelectedValue, topic, name);

            // The duration is saved like  "4 weeks (3 hrs/week)"
            string duration;
            string durationError;
            if (!TryBuildDuration(ddlEditWeeks.SelectedValue, ddlEditHours.SelectedValue, out duration, out durationError))
            {
                ShowMessage(durationError, false);
                ReopenPopup("editModal");
                return;
            }

            // The command is built from small pieces, so the description is only
            // saved when the table really has a description column.
            string sql = "UPDATE public.courses SET title = @title, category = @category";
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
            parameters.Add(new NpgsqlParameter("@title", title));
            parameters.Add(new NpgsqlParameter("@category", topic == "" ? (object)DBNull.Value : topic));

            // Both duration boxes empty = keep the duration that is already saved
            if (duration != "")
            {
                sql += ", duration = @duration";
                parameters.Add(DurationParameter(duration));
            }

            if (DescriptionColumn() != null)
            {
                sql += ", " + DescriptionColumn() + " = @description";
                parameters.Add(new NpgsqlParameter("@description", description));
            }

            // Save the SPM / IGCSE choice in the course_level column too
            if (ColumnExists("course_level"))
            {
                sql += ", course_level = @level";
                parameters.Add(new NpgsqlParameter("@level", ddlEditLevel.SelectedValue == "" ? (object)DBNull.Value : ddlEditLevel.SelectedValue));
            }

            // Save the status: "draft" (unpublished) or "published"
            if (ColumnExists("status"))
            {
                sql += ", status = @status";
                parameters.Add(TextParameter("@status", ddlEditStatus.SelectedValue == "published" ? "published" : "draft"));
            }

            // Save the references as a list (one reference per line in the box)
            if (ColumnExists("reference"))
            {
                sql += ", \"reference\" = @reference";
                parameters.Add(TextParameter("@reference", ReferencesToJson(txtEditReferences.Text)));
            }

            // Remember WHEN it was changed
            if (ColumnExists("updated_at"))
                sql += ", updated_at = now()";

            string newImage = "";       // the new picture (if the admin chose one)
            string oldImage = "";       // the picture the course had before

            try
            {
                // A new picture was chosen: save the file, then store its path
                newImage = SaveCourseImage(fuEditImage);
                if (newImage != "")
                {
                    oldImage = Convert.ToString(GetValueOrNull("SELECT course_img::text FROM public.courses WHERE course_id = @id", new NpgsqlParameter("@id", id)));
                    sql += ", course_img = @img";
                    parameters.Add(new NpgsqlParameter("@img", newImage));
                }

                sql += " WHERE course_id = @id";
                parameters.Add(new NpgsqlParameter("@id", id));

                RunSql(sql, parameters.ToArray());

                // The old picture file is not needed any more (only files WE saved are deleted)
                if (newImage != "" && oldImage.StartsWith("~/images/courses/"))
                    DeleteImageFile(oldImage);

                ShowMessage("Saved! \"" + title + "\" was updated.", true);
            }
            catch (Exception ex)
            {
                DeleteImageFile(newImage);   // saving failed: remove the new picture file again

                ShowMessage(ex.Message.StartsWith("Please choose") || ex.Message.StartsWith("The picture")
                    ? ex.Message
                    : "Could not save: " + ex.Message, false);
                ReopenPopup("editModal");
            }
        }

        // ----- Create course popup -----
        protected void btnAddCourse_Click(object sender, EventArgs e)
        {
            lblAddError.Text = "";

            string name = txtAddTitle.Text.Trim();
            string topic = ddlAddTopic.SelectedValue.Trim();   // picked from the dropdown
            string description = txtAddDescription.Text.Trim();

            if (name == "")
            {
                lblAddError.Text = "Please type a course name.";
                ReopenPopup("addModal");
                return;
            }

            // The title is saved like  "SPM Biology: Cell Physiology & Organization"
            string title = BuildTitle(ddlAddLevel.SelectedValue, topic, name);

            // The duration is saved like  "4 weeks (3 hrs/week)"
            string duration;
            string durationError;
            if (!TryBuildDuration(ddlAddWeeks.SelectedValue, ddlAddHours.SelectedValue, out duration, out durationError))
            {
                lblAddError.Text = durationError;
                ReopenPopup("addModal");
                return;
            }

            string imagePath = "";

            try
            {
                // 1) Save the picture file (if the admin chose one)
                imagePath = SaveCourseImage(fuAddImage);

                // 2) Make lists of the columns we want to fill, and the values for them
                List<string> columns = new List<string>();
                List<string> values = new List<string>();
                List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();

                columns.Add("title");
                values.Add("@title");
                parameters.Add(new NpgsqlParameter("@title", title));

                columns.Add("category");
                values.Add("@category");
                parameters.Add(new NpgsqlParameter("@category", topic == "" ? (object)DBNull.Value : topic));

                // (no duration typed = nothing saved)
                columns.Add("duration");
                values.Add("@duration");
                parameters.Add(DurationParameter(duration));

                if (imagePath != "")
                {
                    columns.Add("course_img");
                    values.Add("@img");
                    parameters.Add(new NpgsqlParameter("@img", imagePath));
                }

                // SPM / IGCSE also goes in the course_level column
                if (ddlAddLevel.SelectedValue != "" && ColumnExists("course_level"))
                {
                    columns.Add("course_level");
                    values.Add("@level");
                    parameters.Add(new NpgsqlParameter("@level", ddlAddLevel.SelectedValue));
                }

                if (DescriptionColumn() != null)
                {
                    columns.Add(DescriptionColumn());
                    values.Add("@description");
                    parameters.Add(new NpgsqlParameter("@description", description));
                }

                // Status: "draft" (unpublished) or "published"
                if (ColumnExists("status"))
                {
                    columns.Add("status");
                    values.Add("@status");
                    parameters.Add(TextParameter("@status", ddlAddStatus.SelectedValue == "published" ? "published" : "draft"));
                }

                // References: one per line in the box, saved as a list
                if (ColumnExists("reference") && ReferencesToJson(txtAddReferences.Text) != "")
                {
                    columns.Add("\"reference\"");
                    values.Add("@reference");
                    parameters.Add(TextParameter("@reference", ReferencesToJson(txtAddReferences.Text)));
                }

                // If the table also remembers WHO made the course, save the admin's id
                if (adminId > 0 && ColumnExists("user_id"))
                {
                    columns.Add("user_id");
                    values.Add("@userid");
                    parameters.Add(new NpgsqlParameter("@userid", adminId));
                }

                // 3) Put it in the database
                string sql = "INSERT INTO public.courses (" + string.Join(", ", columns.ToArray()) + ")" +
                             " VALUES (" + string.Join(", ", values.ToArray()) + ")";
                RunSql(sql, parameters.ToArray());

                ShowMessage("Created! \"" + title + "\" was added to the catalogue.", true);

                // clear the boxes
                txtAddTitle.Text = "";
                txtAddDescription.Text = "";
                ddlAddTopic.SelectedIndex = 0;
                txtAddReferences.Text = "";
                ddlAddStatus.SelectedIndex = 0;
                ddlAddWeeks.SelectedIndex = 0;
                ddlAddHours.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                // something went wrong: remove the picture we just saved, so no extra files stay behind
                DeleteImageFile(imagePath);

                lblAddError.Text = ex.Message.StartsWith("Please choose") || ex.Message.StartsWith("The picture")
                    ? ex.Message
                    : "Could not create the course: " + ex.Message;
                ReopenPopup("addModal");
            }
        }


        // ----- Delete popup: the admin clicked "Confirm" -----
        protected void btnConfirmDelete_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(hfDeleteId.Value, out id)) return;

            // "the lessons of this course" (used in many lines below)
            string lessonsOfCourse = "(SELECT lesson_id FROM public.lesson WHERE course_id = @id)";

            // "the discussion posts of this course" (the course itself, or one of its lessons)
            string postsOfCourse = "(SELECT post_id FROM public.discussion_posts WHERE course_id = @id OR lesson_id IN " + lessonsOfCourse + ")";

            // Everything that belongs to a course, from the INSIDE to the OUTSIDE.
            // Each line:  { the SQL,  the table.column it needs }   (a line is skipped if the table does not exist)
            string[][] belongsToCourse =
            {
                // quizzes and lessons can point to each other, so we break those links first
                new string[] { "UPDATE public.quiz SET question_id = NULL WHERE lesson_id IN " + lessonsOfCourse,
                               "quiz.question_id", "quiz.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "UPDATE public.lesson SET quiz_id = NULL WHERE course_id = @id",
                               "lesson.quiz_id", "lesson.course_id" },

                // discussions
                new string[] { "DELETE FROM public.discussion_replies WHERE post_id IN " + postsOfCourse,
                               "discussion_replies.post_id", "discussion_posts.post_id", "discussion_posts.course_id", "discussion_posts.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.discussion_posts WHERE course_id = @id OR lesson_id IN " + lessonsOfCourse,
                               "discussion_posts.course_id", "discussion_posts.lesson_id", "lesson.lesson_id", "lesson.course_id" },

                // quizzes of the lessons: scores, questions, then the quiz itself
                new string[] { "DELETE FROM public.quiz_score WHERE quiz_id IN (SELECT quiz_id FROM public.quiz WHERE lesson_id IN " + lessonsOfCourse + ")",
                               "quiz_score.quiz_id", "quiz.quiz_id", "quiz.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.quiz_questions WHERE quiz_id IN (SELECT quiz_id FROM public.quiz WHERE lesson_id IN " + lessonsOfCourse + ")",
                               "quiz_questions.quiz_id", "quiz.quiz_id", "quiz.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.quiz WHERE lesson_id IN " + lessonsOfCourse,
                               "quiz.lesson_id", "lesson.lesson_id", "lesson.course_id" },

                // the lessons: progress, then the lessons themselves
                new string[] { "DELETE FROM public.lesson_progress WHERE lesson_id IN " + lessonsOfCourse,
                               "lesson_progress.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.lesson WHERE course_id = @id", "lesson.course_id" },

                // the rest that points to the course
                new string[] { "DELETE FROM public.enrollment WHERE course_id = @id",      "enrollment.course_id" },
                new string[] { "DELETE FROM public.certificates WHERE course_id = @id",    "certificates.course_id" },
                new string[] { "DELETE FROM public.calendar_events WHERE course_id = @id", "calendar_events.course_id" }
            };

            string imagePath = "";

            try
            {
                string title;

                using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
                {
                    // 1) Is the course still there? (and its title + picture, for later)
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT title, course_img::text FROM public.courses WHERE course_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ShowMessage("That course was not found. Maybe it was already deleted.", false);
                                return;
                            }
                            title = reader[0].ToString();
                            imagePath = reader.IsDBNull(1) ? "" : reader[1].ToString();
                        }
                    }

                    // 2) Which of the lines above can we use? (only the tables that exist)
                    List<string> usable = new List<string>();
                    foreach (string[] line in belongsToCourse)
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
                        foreach (string sql in usable)
                        {
                            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM public.courses WHERE course_id = @id", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();   // everything worked: save it for real
                    }
                }

                // 4) The picture file we saved for this course is not needed any more
                if (imagePath.StartsWith("~/images/courses/"))
                    DeleteImageFile(imagePath);

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
            WAPP_EduCore.Data.LoginKeeper.Forget(Context);   // remove the "remember me" cookie
            Session.Clear();        // forget who was logged in
            Session.Abandon();      // end the session
            Response.Redirect("~/auth/login.aspx");   // go to the login page
        }


        // =====================================================
        //  PICTURE FILES
        // =====================================================

        // Saves the chosen picture in the folder  images/courses
        // and gives back the path to store in the database, like  ~/images/courses/abc123.png
        // (gives back "" when no picture was chosen)
        private string SaveCourseImage(FileUpload upload)
        {
            if (!upload.HasFile) return "";

            string extension = Path.GetExtension(upload.FileName).ToLower();
            string[] allowed = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

            if (Array.IndexOf(allowed, extension) < 0)
                throw new Exception("Please choose a picture file (png, jpg, gif or webp).");

            if (upload.PostedFile.ContentLength > 3 * 1024 * 1024)
                throw new Exception("The picture is too big. Please choose one under 3 MB.");

            string folder = Server.MapPath("~/images/courses/");
            Directory.CreateDirectory(folder);                       // make the folder if it is not there yet

            string fileName = Guid.NewGuid().ToString("N") + extension;   // a random name, so two files never clash
            upload.SaveAs(Path.Combine(folder, fileName));

            return "~/images/courses/" + fileName;
        }

        // Deletes a picture file we saved (used when creating the course failed)
        private void DeleteImageFile(string imagePath)
        {
            if (imagePath == "") return;

            try
            {
                string fullPath = Server.MapPath(imagePath);
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch
            {
                // if it cannot be deleted, it is not a big problem
            }
        }


        // =====================================================
        //  FINDING COLUMNS IN THE courses TABLE
        // =====================================================

        // Does the courses table have a column with this name?
        private bool ColumnExists(string name)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'courses' AND column_name = @name", conn))
            {
                cmd.Parameters.AddWithValue("@name", name);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        // Does ANY table have this column? (used by the delete function)
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

        // The name of the description column, or null when there is none
        private string DescriptionColumn()
        {
            if (!descriptionChecked)
            {
                descriptionChecked = true;

                string[] possibleNames = { "course_description", "description" };
                foreach (string name in possibleNames)
                {
                    if (ColumnExists(name))
                    {
                        descriptionColumn = name;
                        break;
                    }
                }
            }
            return descriptionColumn;
        }


        // =====================================================
        //  HELPERS  (small tools used again and again)
        // =====================================================

        // Makes the title like:  SPM Biology: Cell Physiology & Organization
        //   level = SPM or IGCSE,  topic = Biology,  name = Cell Physiology & Organization
        private string BuildTitle(string level, string topic, string name)
        {
            if (level == "") return name;
            return level + (topic != "" ? " " + topic : "") + ": " + name;
        }

        // Makes the duration like:  4 weeks (3 hrs/week)
        // Both boxes empty  -> duration = ""  (nothing)
        // Gives back false (and an error text) when something is wrong.
        private bool TryBuildDuration(string weeksText, string hoursText, out string duration, out string error)
        {
            duration = "";
            error = "";

            weeksText = weeksText.Trim();
            hoursText = hoursText.Trim();

            if (weeksText == "" && hoursText == "") return true;

            int weeks;
            int hours;

            if (!int.TryParse(weeksText, NumberStyles.None, CultureInfo.InvariantCulture, out weeks) || weeks < 1 || weeks > 24 ||
                !int.TryParse(hoursText, NumberStyles.None, CultureInfo.InvariantCulture, out hours) || hours < 1 || hours > 24)
            {
                error = "Please type the duration as both the weeks and the hours per week (whole numbers from 1 to 24).";
                return false;
            }

            duration = weeks + (weeks == 1 ? " week" : " weeks") + " (" + hours + " hrs/week)";
            return true;
        }

        // 12.5%
        private string Percent(int part, int whole)
        {
            double p = (whole == 0) ? 0 : part * 100.0 / whole;
            return p.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        // The duration column can be a number or text.
        // "Unknown" tells the database: "you decide how to read this".
        private NpgsqlParameter DurationParameter(string text)
        {
            NpgsqlParameter p = new NpgsqlParameter("@duration", NpgsqlDbType.Unknown);
            p.Value = (text == "") ? (object)DBNull.Value : text;
            return p;
        }

        // A text value for a column that can be text OR json OR an enum.
        // "Unknown" tells the database: "you decide how to read this". Empty text = nothing (NULL).
        private NpgsqlParameter TextParameter(string name, string text)
        {
            NpgsqlParameter p = new NpgsqlParameter(name, NpgsqlDbType.Unknown);
            p.Value = (text == "") ? (object)DBNull.Value : text;
            return p;
        }

        // Turns the lines the admin typed into a list the database keeps:
        //   Book one
        //   Book two        ->   ["Book one", "Book two"]
        // Empty lines are skipped. No lines at all -> "" (nothing).
        private string ReferencesToJson(string text)
        {
            string[] lines = text.Replace("\r", "").Split('\n');

            List<string> items = new List<string>();
            foreach (string line in lines)
            {
                string clean = line.Trim();
                if (clean != "") items.Add("\"" + JsonSafe(clean) + "\"");
            }

            if (items.Count == 0) return "";
            return "[" + string.Join(", ", items.ToArray()) + "]";
        }

        // Puts a backslash before the letters that would break the list:  "  and  \
        private string JsonSafe(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in text)
            {
                if (ch == '"') sb.Append("\\\"");
                else if (ch == '\\') sb.Append("\\\\");
                else if (ch < ' ') sb.Append(' ');          // tabs and other hidden characters become a space
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        // Shows a green (ok) or red (error) message at the top
        private void ShowMessage(string text, bool ok)
        {
            litMessage.Text = "<div class='msg " + (ok ? "ok" : "error") + "'>" + Server.HtmlEncode(text) + "</div>";
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

            // The session was forgotten (idle for a long time)? Ask the "remember me" cookie who the admin is.
            return WAPP_EduCore.Data.LoginKeeper.Restore(Context);
        }
    }
}