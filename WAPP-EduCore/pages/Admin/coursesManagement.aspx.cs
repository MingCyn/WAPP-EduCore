using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Npgsql;
using NpgsqlTypes;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    public partial class WebForm4 : System.Web.UI.Page
    {
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        private int adminId = 0;

        private const string ImageFolder = "~/uploads/courses/";
        private const string OldImageFolder = "~/images/courses/";
        private const int MaxImageBytes = 3 * 1024 * 1024; // 3 MB
        private const int PageSize = 10;

        private int CurrentPage
        {
            get { return ViewState["page"] == null ? 0 : (int)ViewState["page"]; }
            set { ViewState["page"] = value; }
        }

        private const string FromAndWhere =
            @" FROM public.courses c
               LEFT JOIN (SELECT course_id, COUNT(*) AS n FROM public.lesson GROUP BY course_id) l
                      ON l.course_id = c.course_id
               WHERE (@search = '' OR c.title ILIKE @like OR c.category ILIKE @like)
                 AND (@topic = '' OR c.category = @topic)
                 AND (@lessons = ''
                      OR (@lessons = 'with'    AND COALESCE(l.n, 0) > 0)
                      OR (@lessons = 'without' AND COALESCE(l.n, 0) = 0)) ";

        private string descriptionColumn = null;
        private bool descriptionChecked = false;

        protected void Page_Load(object sender, EventArgs e)
        {
            int signedInAdmin = CurrentUserId();
            if (signedInAdmin == 0)
            {
                Response.Redirect("~/Default.aspx");
                return;
            }

            WAPP_EduCore.Data.LoginKeeper.Remember(Context, signedInAdmin);
            LoadAdminInfo();
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            bool hasDescription = (DescriptionColumn() != null);
            pnlAddDesc.Visible = hasDescription;
            pnlEditDesc.Visible = hasDescription;

            RefreshTopicList();
            LoadStats();
            LoadCourses();
        }

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
                        if (words.Length > 0)
                            AdminInitials = words[0].Substring(0, 1).ToUpper();
                    }
                }
            }
        }

        private void RefreshTopicList()
        {
            string keep = ddlTopic.SelectedValue;
            string keepAdd = ddlAddTopic.SelectedValue;
            string keepEdit = ddlEditTopic.SelectedValue;

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
                ddlAddTopic.Items.Add(new ListItem(topic, topic));
                ddlEditTopic.Items.Add(new ListItem(topic, topic));
                options.Append("<option value='" + Server.HtmlEncode(topic) + "'></option>");
            }

            if (ddlTopic.Items.FindByValue(keep) != null)
                ddlTopic.SelectedValue = keep;

            if (ddlAddTopic.Items.FindByValue(keepAdd) != null)
                ddlAddTopic.SelectedValue = keepAdd;

            if (ddlEditTopic.Items.FindByValue(keepEdit) != null)
                ddlEditTopic.SelectedValue = keepEdit;

            litTopicOptions.Text = options.ToString();
        }

        private void LoadStats()
        {
            int total = GetNumber("SELECT COUNT(*) FROM public.courses");
            int topics = GetNumber("SELECT COUNT(DISTINCT category) FROM public.courses");

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

        private void LoadCourses()
        {
            int total;
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*)" + FromAndWhere, conn))
            {
                AddFilters(cmd);
                total = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int lastPage = Math.Max(0, (total - 1) / PageSize);
            if (CurrentPage > lastPage) CurrentPage = lastPage;
            if (CurrentPage < 0) CurrentPage = 0;

            string descriptionPart = (DescriptionColumn() == null)
                ? "''"
                : "COALESCE(c." + DescriptionColumn() + "::text, '')";

            string levelPart = ColumnExists("course_level") ? "COALESCE(c.course_level::text, '')" : "''";
            string statusPart = ColumnExists("status") ? "COALESCE(c.status::text, '')" : "''";
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

            StringBuilder rows = new StringBuilder();
            foreach (DataRow r in table.Rows)
            {
                rows.Append(BuildRow(r));
            }
            litRows.Text = rows.ToString();

            pnlTable.Visible = (total > 0);
            pnlEmpty.Visible = (total == 0);

            int from = CurrentPage * PageSize + 1;
            int to = CurrentPage * PageSize + table.Rows.Count;
            lblShowing.Text = "Showing " + (total == 0 ? 0 : from) + "–" + to + " of " + total.ToString("N0");

            btnPrev.Enabled = (CurrentPage > 0);
            btnNext.Enabled = (CurrentPage < lastPage);
        }

        private void AddFilters(NpgsqlCommand cmd)
        {
            string search = txtSearch.Text.Trim();
            cmd.Parameters.AddWithValue("@search", search);
            cmd.Parameters.AddWithValue("@like", "%" + search + "%");
            cmd.Parameters.AddWithValue("@topic", ddlTopic.SelectedValue);
            cmd.Parameters.AddWithValue("@lessons", ddlLessons.SelectedValue);
        }

        private string BuildRow(DataRow r)
        {
            int id = Convert.ToInt32(r["course_id"]);
            string title = r["title"].ToString();
            string topic = r["category"] == DBNull.Value ? "" : r["category"].ToString();
            string duration = r["duration"] == DBNull.Value ? "" : r["duration"].ToString();
            string description = r["description"].ToString();
            int lessons = Convert.ToInt32(r["lessons"]);
            string status = (r["status"].ToString().Trim().ToLower() == "published") ? "published" : "draft";

            string img = r["img"].ToString();
            if (img.StartsWith("~/")) img = ResolveUrl(img);

            StringBuilder sb = new StringBuilder();
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

            sb.Append("<td class='grey-text'>" + id + "</td>");
            sb.Append("<td>" + Server.HtmlEncode(title) + "</td>");
            sb.Append("<td>" + (topic == "" ? "—" : Server.HtmlEncode(topic)) + "</td>");
            sb.Append("<td>" + (duration == "" ? "—" : Server.HtmlEncode(duration)) + "</td>");

            if (lessons == 0)
                sb.Append("<td><span class='badge pending'>No lessons</span></td>");
            else
                sb.Append("<td>" + lessons + "</td>");

            if (status == "published")
                sb.Append("<td><span class='badge published'>Published</span></td>");
            else
                sb.Append("<td><span class='badge draft'>Draft</span></td>");

            sb.Append("<td class='grey-text'>" + FormatDate(r["created_at"]) + "</td>");

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

        protected void btnSaveEdit_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(hfEditId.Value, out id)) return;

            string name = txtEditTitle.Text.Trim();
            string topic = ddlEditTopic.SelectedValue.Trim();
            string description = txtEditDescription.Text.Trim();

            if (name == "")
            {
                ShowMessage("The course name cannot be empty.", false);
                ReopenPopup("editModal");
                return;
            }

            string title = name;
            string duration;
            string durationError;
            if (!TryBuildDuration(ddlEditWeeks.SelectedValue, ddlEditHours.SelectedValue, out duration, out durationError))
            {
                ShowMessage(durationError, false);
                ReopenPopup("editModal");
                return;
            }

            string sql = "UPDATE public.courses SET title = @title, category = @category";
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
            parameters.Add(new NpgsqlParameter("@title", title));
            parameters.Add(new NpgsqlParameter("@category", topic == "" ? (object)DBNull.Value : topic));

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

            if (ColumnExists("course_level"))
            {
                sql += ", course_level = @level";
                parameters.Add(new NpgsqlParameter("@level", ddlEditLevel.SelectedValue == "" ? (object)DBNull.Value : ddlEditLevel.SelectedValue));
            }

            if (ColumnExists("status"))
            {
                sql += ", status = @status";
                parameters.Add(TextParameter("@status", ddlEditStatus.SelectedValue == "published" ? "published" : "draft"));
            }

            if (ColumnExists("reference"))
            {
                sql += ", \"reference\" = @reference";
                parameters.Add(TextParameter("@reference", ReferencesToJson(txtEditReferences.Text)));
            }

            if (ColumnExists("updated_at"))
                sql += ", updated_at = now()";

            string newImage = "";
            string oldImage = "";

            try
            {
                if (fuEditImage.HasFile)
                {
                    string imgError;
                    if (!TrySaveCourseImage(fuEditImage, "course-" + id, out newImage, out imgError))
                    {
                        ShowMessage(imgError, false);
                        ReopenPopup("editModal");
                        return;
                    }
                }

                if (newImage != "")
                {
                    oldImage = Convert.ToString(GetValueOrNull("SELECT course_img::text FROM public.courses WHERE course_id = @id", new NpgsqlParameter("@id", id)));
                    sql += ", course_img = @img";
                    parameters.Add(new NpgsqlParameter("@img", newImage));
                }

                sql += " WHERE course_id = @id";
                parameters.Add(new NpgsqlParameter("@id", id));

                RunSql(sql, parameters.ToArray());

                if (newImage != "" && IsOurImage(oldImage))
                    DeleteImageFile(oldImage);

                ShowMessage("Saved! \"" + title + "\" was updated.", true);
            }
            catch (Exception ex)
            {
                DeleteImageFile(newImage);
                ShowMessage("Could not save: " + ex.Message, false);
                ReopenPopup("editModal");
            }
        }

        protected void btnAddCourse_Click(object sender, EventArgs e)
        {
            lblAddError.Text = "";

            string name = txtAddTitle.Text.Trim();
            string topic = ddlAddTopic.SelectedValue.Trim();
            string description = txtAddDescription.Text.Trim();

            if (name == "")
            {
                lblAddError.Text = "Please type a course name.";
                ReopenPopup("addModal");
                return;
            }

            string title = name;
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
                if (fuAddImage.HasFile)
                {
                    string imgError;
                    if (!TrySaveCourseImage(fuAddImage, "course-new", out imagePath, out imgError))
                    {
                        lblAddError.Text = imgError;
                        ReopenPopup("addModal");
                        return;
                    }
                }

                List<string> columns = new List<string>();
                List<string> values = new List<string>();
                List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();

                columns.Add("title");
                values.Add("@title");
                parameters.Add(new NpgsqlParameter("@title", title));

                columns.Add("category");
                values.Add("@category");
                parameters.Add(new NpgsqlParameter("@category", topic == "" ? (object)DBNull.Value : topic));

                columns.Add("duration");
                values.Add("@duration");
                parameters.Add(DurationParameter(duration));

                if (imagePath != "")
                {
                    columns.Add("course_img");
                    values.Add("@img");
                    parameters.Add(new NpgsqlParameter("@img", imagePath));
                }

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

                if (ColumnExists("status"))
                {
                    columns.Add("status");
                    values.Add("@status");
                    parameters.Add(TextParameter("@status", ddlAddStatus.SelectedValue == "published" ? "published" : "draft"));
                }

                if (ColumnExists("reference") && ReferencesToJson(txtAddReferences.Text) != "")
                {
                    columns.Add("\"reference\"");
                    values.Add("@reference");
                    parameters.Add(TextParameter("@reference", ReferencesToJson(txtAddReferences.Text)));
                }

                if (adminId > 0 && ColumnExists("user_id"))
                {
                    columns.Add("user_id");
                    values.Add("@userid");
                    parameters.Add(new NpgsqlParameter("@userid", adminId));
                }

                string sql = "INSERT INTO public.courses (" + string.Join(", ", columns.ToArray()) + ")" +
                             " VALUES (" + string.Join(", ", values.ToArray()) + ")";
                RunSql(sql, parameters.ToArray());

                ShowMessage("Created! \"" + title + "\" was added to the catalogue.", true);

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
                DeleteImageFile(imagePath);
                lblAddError.Text = "Could not create the course: " + ex.Message;
                ReopenPopup("addModal");
            }
        }

        protected void btnConfirmDelete_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(hfDeleteId.Value, out id)) return;

            string lessonsOfCourse = "(SELECT lesson_id FROM public.lesson WHERE course_id = @id)";
            string postsOfCourse = "(SELECT post_id FROM public.discussion_posts WHERE course_id = @id OR lesson_id IN " + lessonsOfCourse + ")";

            string[][] belongsToCourse =
            {
                new string[] { "UPDATE public.quiz SET question_id = NULL WHERE lesson_id IN " + lessonsOfCourse,
                               "quiz.question_id", "quiz.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "UPDATE public.lesson SET quiz_id = NULL WHERE course_id = @id",
                               "lesson.quiz_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.discussion_replies WHERE post_id IN " + postsOfCourse,
                               "discussion_replies.post_id", "discussion_posts.post_id", "discussion_posts.course_id", "discussion_posts.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.discussion_posts WHERE course_id = @id OR lesson_id IN " + lessonsOfCourse,
                               "discussion_posts.course_id", "discussion_posts.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.quiz_score WHERE quiz_id IN (SELECT quiz_id FROM public.quiz WHERE lesson_id IN " + lessonsOfCourse + ")",
                               "quiz_score.quiz_id", "quiz.quiz_id", "quiz.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.quiz_questions WHERE quiz_id IN (SELECT quiz_id FROM public.quiz WHERE lesson_id IN " + lessonsOfCourse + ")",
                               "quiz_questions.quiz_id", "quiz.quiz_id", "quiz.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.quiz WHERE lesson_id IN " + lessonsOfCourse,
                               "quiz.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.lesson_progress WHERE lesson_id IN " + lessonsOfCourse,
                               "lesson_progress.lesson_id", "lesson.lesson_id", "lesson.course_id" },
                new string[] { "DELETE FROM public.lesson WHERE course_id = @id", "lesson.course_id" },
                new string[] { "DELETE FROM public.enrollment WHERE course_id = @id",   "enrollment.course_id" },
                new string[] { "DELETE FROM public.certificates WHERE course_id = @id",   "certificates.course_id" },
                new string[] { "DELETE FROM public.calendar_events WHERE course_id = @id", "calendar_events.course_id" }
            };

            string imagePath = "";

            try
            {
                string title;
                using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
                {
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

                        transaction.Commit();
                    }
                }

                if (IsOurImage(imagePath))
                    DeleteImageFile(imagePath);

                ShowMessage("Deleted! \"" + title + "\" was permanently removed, together with everything linked to it.", true);
            }
            catch (Exception ex)
            {
                ShowMessage("Could not delete: " + ex.Message, false);
            }
        }

        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            WAPP_EduCore.Data.LoginKeeper.Forget(Context);
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/auth/login.aspx");
        }

        private bool TrySaveCourseImage(FileUpload upload, string namePrefix, out string savedPath, out string error)
        {
            savedPath = "";
            error = "";

            if (!upload.HasFile) return true;

            if (upload.PostedFiles != null && upload.PostedFiles.Count > 1)
            {
                error = "Please upload only one image file at a time.";
                return false;
            }

            HttpPostedFile file = upload.PostedFile;
            if (file.ContentLength > MaxImageBytes)
            {
                error = "The image must be 3 MB or smaller.";
                return false;
            }

            try
            {
                var head = new byte[12];
                int read = file.InputStream.Read(head, 0, head.Length);
                file.InputStream.Position = 0;

                string extension = DetectImageExtension(head, read);
                if (extension == null)
                {
                    error = "Please choose a valid JPG, PNG or WEBP image format.";
                    return false;
                }

                string folder = Server.MapPath(ImageFolder);
                Directory.CreateDirectory(folder);

                string fileName = namePrefix + "-" + Guid.NewGuid().ToString("N") + extension;
                string fullPath = Path.Combine(folder, fileName);

                file.SaveAs(fullPath);
                savedPath = ImageFolder + fileName;
                return true;
            }
            catch (Exception ex)
            {
                error = "The image could not be saved: " + ex.Message;
                return false;
            }
        }

        private static string DetectImageExtension(byte[] head, int length)
        {
            if (length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return ".jpg";
            if (length >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47 && head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A) return ".png";
            if (length >= 12 && head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F' && head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P') return ".webp";
            return null;
        }

        private bool IsOurImage(string imagePath)
        {
            return imagePath.StartsWith(ImageFolder, StringComparison.OrdinalIgnoreCase)
                || imagePath.StartsWith(OldImageFolder, StringComparison.OrdinalIgnoreCase);
        }

        private void DeleteImageFile(string imagePath)
        {
            if (imagePath == "") return;

            try
            {
                string fullPath = Server.MapPath(imagePath);
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch { }
        }

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

        private string Percent(int part, int whole)
        {
            double p = (whole == 0) ? 0 : part * 100.0 / whole;
            return p.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private NpgsqlParameter DurationParameter(string text)
        {
            NpgsqlParameter p = new NpgsqlParameter("@duration", NpgsqlDbType.Unknown);
            p.Value = (text == "") ? (object)DBNull.Value : text;
            return p;
        }

        private NpgsqlParameter TextParameter(string name, string text)
        {
            NpgsqlParameter p = new NpgsqlParameter(name, NpgsqlDbType.Unknown);
            p.Value = (text == "") ? (object)DBNull.Value : text;
            return p;
        }

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

        private string JsonSafe(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in text)
            {
                if (ch == '"') sb.Append("\\\"");
                else if (ch == '\\') sb.Append("\\\\");
                else if (ch < ' ') sb.Append(' ');
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        private void ShowMessage(string text, bool ok)
        {
            litMessage.Text = "<div class='msg " + (ok ? "ok" : "error") + "'>" + Server.HtmlEncode(text) + "</div>";
        }

        private void ReopenPopup(string popupId)
        {
            ClientScript.RegisterStartupScript(GetType(), "reopen", "openModal('" + popupId + "');", true);
        }

        private string Attr(string key, string value)
        {
            return " data-" + key + "='" + Server.HtmlEncode(value) + "'";
        }

        private string IconButton(string color, string title, string js, string svgInside)
        {
            return "<button type='button' class='icon-btn " + color + "' title='" + title + "' " +
                   "onclick='event.stopPropagation(); " + js + "'>" +
                   "<svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
                   svgInside + "</svg></button>";
        }

        private string FormatDate(object value)
        {
            if (value == null || value == DBNull.Value) return "—";
            return Convert.ToDateTime(value).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        }

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

        private int GetNumber(string sql)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

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

        private void RunSql(string sql, params NpgsqlParameter[] parameters)
        {
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        private int CurrentUserId()
        {
            int id;
            if (int.TryParse(Convert.ToString(Session["UserID"]), out id) && id > 0) return id;
            if (int.TryParse(Convert.ToString(Session["user_id"]), out id) && id > 0) return id;
            return WAPP_EduCore.Data.LoginKeeper.Restore(Context);
        }
    }
}