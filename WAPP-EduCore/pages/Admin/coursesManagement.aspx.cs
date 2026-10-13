using System;
using System.Data;
using System.Globalization;
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

                if (Session["user_id"] != null)
                {
                    cmd.CommandText = "SELECT full_name FROM public.users WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(Session["user_id"]));
                }
                else
                {
                    cmd.CommandText = "SELECT full_name FROM public.users WHERE role = 'Admin' ORDER BY user_id LIMIT 1";
                }

                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    string fullName = result.ToString().Trim();
                    AdminName = Server.HtmlEncode(fullName);

                    string[] words = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 1)
                        AdminInitials = words[0].Substring(0, 1).ToUpper();
                    else if (words.Length > 1)
                        AdminInitials = (words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, 1)).ToUpper();
                }
            }
        }


        // =====================================================
        //  THE TOPIC DROPDOWN  +  the suggestion list in the popups
        // =====================================================
        private void RefreshTopicList()
        {
            string keep = ddlTopic.SelectedValue;     // remember what was chosen

            DataTable topics = GetTable("SELECT DISTINCT category FROM public.courses WHERE category IS NOT NULL AND category <> '' ORDER BY category");

            ddlTopic.Items.Clear();
            ddlTopic.Items.Add(new ListItem("All topics", ""));

            StringBuilder options = new StringBuilder();
            foreach (DataRow r in topics.Rows)
            {
                string topic = r["category"].ToString();
                ddlTopic.Items.Add(new ListItem(topic, topic));
                options.Append("<option value='" + Server.HtmlEncode(topic) + "'></option>");
            }

            if (ddlTopic.Items.FindByValue(keep) != null)
                ddlTopic.SelectedValue = keep;        // choose it again

            litTopicOptions.Text = options.ToString();
        }


        // =====================================================
        //  THE 5 NUMBER BOXES
        // =====================================================
        private void LoadStats()
        {
            int total = GetNumber("SELECT COUNT(*) FROM public.courses");
            int topics = GetNumber("SELECT COUNT(DISTINCT category) FROM public.courses");
            int lessons = GetNumber("SELECT COUNT(*) FROM public.lesson");
            int newThisMonth = GetNumber("SELECT COUNT(*) FROM public.courses WHERE created_at >= date_trunc('month', now())");
            int noLessons = GetNumber(
                @"SELECT COUNT(*) FROM public.courses c
                  WHERE NOT EXISTS (SELECT 1 FROM public.lesson l WHERE l.course_id = c.course_id)");

            lblTotal.Text = total.ToString("N0");
            lblTotalNote.Text = "Across " + topics + " topics";
            lblTopics.Text = topics.ToString("N0");
            lblLessons.Text = lessons.ToString("N0");
            lblNew.Text = newThisMonth.ToString("N0");
            lblNoLessons.Text = noLessons.ToString("N0");
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
            DataTable table = new DataTable();
            string sql =
                @"SELECT c.course_id, c.title, c.category, c.duration::text AS duration, c.created_at,
                         COALESCE(l.n, 0) AS lessons,
                         CASE WHEN c.course_img::text ~ '^(https?://|/|~/)' THEN c.course_img::text ELSE '' END AS img"
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
            int lessons = Convert.ToInt32(r["lessons"]);

            string img = r["img"].ToString();
            if (img.StartsWith("~/")) img = ResolveUrl(img);      // ~/images/a.png  ->  /images/a.png

            StringBuilder sb = new StringBuilder();

            // The row itself (click = open the detail popup)
            sb.Append("<tr class='clickable' id='row" + id + "' onclick='openDetail(" + id + ")'");
            sb.Append(Attr("title", title));
            sb.Append(Attr("topic", topic));
            sb.Append(Attr("duration", duration));
            sb.Append(Attr("lessons", lessons.ToString()));
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

            sb.Append("<td class='grey-text'>" + FormatDate(r["created_at"]) + "</td>");

            // The 2 little icons: view and edit
            sb.Append("<td>");
            sb.Append(IconButton("blue", "View", "openDetail(" + id + ")",
                "<path d='M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z'/><circle cx='12' cy='12' r='3'/>"));
            sb.Append(IconButton("", "Edit", "openEdit(" + id + ")",
                "<path d='M12 20h9'/><path d='M16.5 3.5a2.1 2.1 0 013 3L7 19l-4 1 1-4z'/>"));
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

            string title = txtEditTitle.Text.Trim();
            string topic = txtEditTopic.Text.Trim();
            string duration = txtEditDuration.Text.Trim();

            if (title == "")
            {
                ShowMessage("The course title cannot be empty.", false);
                ReopenPopup("editModal");
                return;
            }

            try
            {
                RunSql("UPDATE public.courses SET title = @title, category = @category, duration = @duration WHERE course_id = @id",
                    new NpgsqlParameter("@title", title),
                    new NpgsqlParameter("@category", topic == "" ? (object)DBNull.Value : topic),
                    DurationParameter(duration),
                    new NpgsqlParameter("@id", id));

                ShowMessage("Saved! \"" + title + "\" was updated.", true);
            }
            catch (Exception ex)
            {
                ShowMessage("Could not save: " + ex.Message, false);
                ReopenPopup("editModal");
            }
        }

        // ----- Create course popup -----
        protected void btnAddCourse_Click(object sender, EventArgs e)
        {
            lblAddError.Text = "";

            string title = txtAddTitle.Text.Trim();
            string topic = txtAddTopic.Text.Trim();
            string duration = txtAddDuration.Text.Trim();

            if (title == "")
            {
                lblAddError.Text = "Please type a course title.";
                ReopenPopup("addModal");
                return;
            }

            try
            {
                RunSql("INSERT INTO public.courses (title, category, duration) VALUES (@title, @category, @duration)",
                    new NpgsqlParameter("@title", title),
                    new NpgsqlParameter("@category", topic == "" ? (object)DBNull.Value : topic),
                    DurationParameter(duration));

                ShowMessage("Created! \"" + title + "\" was added to the catalogue.", true);

                // clear the boxes
                txtAddTitle.Text = "";
                txtAddTopic.Text = "";
                txtAddDuration.Text = "";
            }
            catch (Exception ex)
            {
                lblAddError.Text = "Could not create the course: " + ex.Message;
                ReopenPopup("addModal");
            }
        }


        // =====================================================
        //  HELPERS  (small tools used again and again)
        // =====================================================

        // The duration column can be a number or text.
        // "Unknown" tells the database: "you decide how to read this".
        private NpgsqlParameter DurationParameter(string text)
        {
            NpgsqlParameter p = new NpgsqlParameter("@duration", NpgsqlDbType.Unknown);
            p.Value = (text == "") ? (object)DBNull.Value : text;
            return p;
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
    }
}