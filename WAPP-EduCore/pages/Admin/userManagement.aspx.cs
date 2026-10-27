using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Npgsql;
using WAPP_EduCore.Data;

namespace WAPP_EduCore.pages.Admin
{
    // NOTE: the class name is WebForm2 because your .aspx first line says Inherits="...WebForm2"
    public partial class WebForm2 : System.Web.UI.Page
    {
        // These are shown in the sidebar and the top-right circle
        protected string AdminName = "Administrator";
        protected string AdminInitials = "AD";

        private int adminId = 0;            // the id of the admin who is logged in
        private const int PageSize = 10;    // how many users per page (change me!)

        // The page number we are on (0 = first page). It is remembered between clicks.
        private int CurrentPage
        {
            get { return ViewState["page"] == null ? 0 : (int)ViewState["page"]; }
            set { ViewState["page"] = value; }
        }

        // The SQL "WHERE" part used by the search + role filter
        private const string WhereText =
            @" WHERE (@search = '' OR full_name ILIKE @like OR email ILIKE @like)
                 AND (@role = '' OR role = @role) ";


        // =====================================================
        //  STEP 1: Page opens  ->  find out who the admin is
        // =====================================================
        protected void Page_Load(object sender, EventArgs e)
        {
            LoadAdminInfo();
        }

        // =====================================================
        //  STEP 2: PreRender runs AFTER every button click and
        //  just BEFORE the page is drawn. So the numbers and the
        //  table are always fresh.
        // =====================================================
        protected void Page_PreRender(object sender, EventArgs e)
        {
            LoadStats();
            LoadUsers();
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
                    cmd.CommandText = "SELECT user_id, full_name FROM public.users WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(Session["user_id"]));
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
                            AdminInitials = (words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, 1)).ToUpper();
                    }
                }
            }
        }


        // =====================================================
        //  THE 5 NUMBER BOXES
        // =====================================================
        private void LoadStats()
        {
            int total = GetNumber("SELECT COUNT(*) FROM public.users");
            int students = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Student'");
            int tutors = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Tutor'");
            int admins = GetNumber("SELECT COUNT(*) FROM public.users WHERE role = 'Admin'");
            int newThisMonth = GetNumber("SELECT COUNT(*) FROM public.users WHERE created_at >= date_trunc('month', now())");

            lblTotal.Text = total.ToString("N0");
            lblNew.Text = newThisMonth.ToString("N0");

            lblStudents.Text = students.ToString("N0");
            lblStudentsNote.Text = Percent(students, total) + " of users";

            lblTutors.Text = tutors.ToString("N0");
            lblTutorsNote.Text = Percent(tutors, total) + " of users";

            lblAdmins.Text = admins.ToString("N0");
            lblAdminsNote.Text = Percent(admins, total) + " of users";
        }


        // =====================================================
        //  THE USER TABLE  (search + filter + pages)
        // =====================================================
        private void LoadUsers()
        {
            // 1) How many users match the search?
            int total;
            using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*) FROM public.users" + WhereText, conn))
            {
                AddFilters(cmd);
                total = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 2) Make sure the page number is not too big
            int lastPage = Math.Max(0, (total - 1) / PageSize);
            if (CurrentPage > lastPage) CurrentPage = lastPage;
            if (CurrentPage < 0) CurrentPage = 0;

            // 3) Get only the 10 users for this page
            DataTable table = new DataTable();
            string sql =
                @"SELECT user_id, full_name, email, dob, gender, role, created_at
                  FROM public.users" + WhereText +
                @" ORDER BY user_id ASC
                  LIMIT @size OFFSET @skip";   // ASC = smallest first (1, 2, 3...). Change to DESC for newest first.

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

            // 6) "Showing 1-10 of 12,842 users" + Previous / Next buttons
            int from = CurrentPage * PageSize + 1;
            int to = CurrentPage * PageSize + table.Rows.Count;
            lblShowing.Text = "Showing " + from + "–" + to + " of " + total.ToString("N0") + " users";

            btnPrev.Enabled = (CurrentPage > 0);
            btnNext.Enabled = (CurrentPage < lastPage);
        }

        // Gives the search words to the database command
        private void AddFilters(NpgsqlCommand cmd)
        {
            string search = txtSearch.Text.Trim();
            cmd.Parameters.AddWithValue("@search", search);
            cmd.Parameters.AddWithValue("@like", "%" + search + "%");
            cmd.Parameters.AddWithValue("@role", ddlRole.SelectedValue);
        }

        // Makes ONE table row.
        // The data-... parts hide the user's info inside the row,
        // so the popup can read it with JavaScript when you click.
        private string BuildRow(DataRow r)
        {
            int id = Convert.ToInt32(r["user_id"]);
            string name = r["full_name"].ToString();
            string email = r["email"].ToString();
            string role = r["role"].ToString();
            string roleText = (role == "Admin") ? "Administrator" : role;

            StringBuilder sb = new StringBuilder();

            // The row itself (click = open the detail popup)
            sb.Append("<tr class='clickable' id='row" + id + "' onclick='openDetail(" + id + ")'");
            sb.Append(Attr("name", name));
            sb.Append(Attr("email", email));
            sb.Append(Attr("role", role));
            sb.Append(Attr("roletext", roleText));
            sb.Append(Attr("dob", FormatDate(r["dob"])));                 // 30 May 2012 (shown in the popup)
            sb.Append(Attr("dobraw", r["dob"] == DBNull.Value ? "" :      // 2012-05-30 (the Edit box needs this style)
                Convert.ToDateTime(r["dob"]).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            sb.Append(Attr("gender", r["gender"] == DBNull.Value ? "—" : r["gender"].ToString()));      // shown in the popup
            sb.Append(Attr("genderraw", r["gender"] == DBNull.Value ? "" : r["gender"].ToString()));    // used by the Edit box
            sb.Append(Attr("joined", FormatDate(r["created_at"])));
            sb.Append(">");

            // The cells
            sb.Append("<td class='grey-text'>" + id + "</td>");
            sb.Append("<td>" + Server.HtmlEncode(name) + "</td>");
            sb.Append("<td class='grey-text'>" + Server.HtmlEncode(email) + "</td>");
            sb.Append("<td class='grey-text'>" + FormatDate(r["dob"]) + "</td>");
            sb.Append("<td class='grey-text'>" + (r["gender"] == DBNull.Value ? "—" : Server.HtmlEncode(r["gender"].ToString())) + "</td>");
            sb.Append("<td>" + roleText + "</td>");
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

            string name = txtEditName.Text.Trim();
            string email = txtEditEmail.Text.Trim();
            string role = ddlEditRole.SelectedValue;
            string dobText = txtEditDob.Text.Trim();

            // Gender: "Not set" (empty) is saved as nothing
            object genderValue = (ddlEditGender.SelectedValue == "") ? (object)DBNull.Value : ddlEditGender.SelectedValue;

            if (name == "" || email == "")
            {
                ShowMessage("Name and email cannot be empty.", false);
                ReopenPopup("editModal");
                return;
            }

            // Date of birth: empty is allowed (saved as nothing), but it must be a real date
            object dobValue = DBNull.Value;
            if (dobText != "")
            {
                DateTime dob;
                if (!DateTime.TryParseExact(dobText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
                {
                    ShowMessage("Please write the date of birth like 30/05/2012 (day/month/year).", false);
                    ReopenPopup("editModal");
                    return;
                }
                dobValue = dob;
            }

            if (id == adminId && role != "Admin")
            {
                ShowMessage("You cannot remove your own Administrator role.", false);
                return;
            }

            try
            {
                RunSql("UPDATE public.users SET full_name = @name, email = @email, dob = @dob, gender = @gender, role = @role WHERE user_id = @id",
                    new NpgsqlParameter("@name", name),
                    new NpgsqlParameter("@email", email),
                    new NpgsqlParameter("@dob", dobValue),
                    new NpgsqlParameter("@gender", genderValue),
                    new NpgsqlParameter("@role", role),
                    new NpgsqlParameter("@id", id));

                ShowMessage("Saved! " + name + " was updated.", true);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message.Contains("users_email_key") ? "That email address is already used." : "Could not save: " + ex.Message, false);
            }
        }

        // ----- Add user popup -----
        protected void btnAddUser_Click(object sender, EventArgs e)
        {
            lblAddError.Text = "";

            string name = txtAddName.Text.Trim();
            string email = txtAddEmail.Text.Trim();
            string password = txtAddPassword.Text;
            string dobText = txtAddDob.Text.Trim();

            DateTime dob;

            if (name == "" || email == "" || password == "" || dobText == "")
            {
                lblAddError.Text = "Please fill in every box.";
                ReopenPopup("addModal");
                return;
            }

            if (password.Length < 8)
            {
                lblAddError.Text = "Password must be at least 8 characters.";
                ReopenPopup("addModal");
                return;
            }

            if (!DateTime.TryParseExact(dobText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
            {
                lblAddError.Text = "Please write the date of birth like 30/05/2012 (day/month/year).";
                ReopenPopup("addModal");
                return;
            }

            try
            {
                // Make sure the "profile" table exists (the database may need it when a user is added)
                EnsureProfileTable();

                // Same as signup.aspx: the password is saved the same way
                RunSql(@"INSERT INTO public.users (full_name, email, dob, gender, password_hash, role)
                         VALUES (@name, @email, @dob, @gender, @password, @role)",
                    new NpgsqlParameter("@name", name),
                    new NpgsqlParameter("@email", email),
                    new NpgsqlParameter("@dob", dob),
                    new NpgsqlParameter("@gender", ddlAddGender.SelectedValue),
                    new NpgsqlParameter("@password", password),
                    new NpgsqlParameter("@role", ddlAddRole.SelectedValue));

                ShowMessage("Added! " + name + " can now sign in.", true);

                // clear the boxes
                txtAddName.Text = "";
                txtAddEmail.Text = "";
                txtAddDob.Text = "";
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("users_email_key"))
                    lblAddError.Text = "This email address is already registered.";
                else if (ex.Message.Contains("42P01"))
                    lblAddError.Text = "Could not add the user: the database wants a table that is missing (" + ex.Message + "). Please create that table in Supabase.";
                else
                    lblAddError.Text = "Could not add the user: " + ex.Message;
                ReopenPopup("addModal");
            }
        }


        // ----- Delete popup: the admin clicked "Confirm" -----
        protected void btnConfirmDelete_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(hfDeleteId.Value, out id)) return;

            // The admin must not delete their OWN account (they would be locked out)
            if (id == adminId)
            {
                ShowMessage("You cannot delete your own account.", false);
                return;
            }

            // These are the things that belong to a user. They are removed together with the user,
            // otherwise the database says "this user is still used somewhere".
            // Each line:  { the SQL,  the table.column it needs }   (a line is skipped if the table does not exist)
            string[][] belongsToUser =
            {
                new string[] { "DELETE FROM public.discussion_replies WHERE post_id IN (SELECT post_id FROM public.discussion_posts WHERE user_id = @id)",
                               "discussion_replies.post_id", "discussion_posts.post_id", "discussion_posts.user_id" },
                new string[] { "DELETE FROM public.discussion_replies WHERE user_id = @id", "discussion_replies.user_id" },
                new string[] { "DELETE FROM public.discussion_posts WHERE user_id = @id",   "discussion_posts.user_id" },
                new string[] { "DELETE FROM public.messages WHERE sender_id = @id",         "messages.sender_id" },
                new string[] { "DELETE FROM public.messages WHERE receiver_id = @id",       "messages.receiver_id" },
                new string[] { "DELETE FROM public.calendar_events WHERE user_id = @id",    "calendar_events.user_id" },
                new string[] { "DELETE FROM public.certificates WHERE user_id = @id",       "certificates.user_id" },
                new string[] { "DELETE FROM public.enrollment WHERE user_id = @id",         "enrollment.user_id" },
                new string[] { "DELETE FROM public.lesson_progress WHERE user_id = @id",    "lesson_progress.user_id" },
                new string[] { "DELETE FROM public.quiz_score WHERE user_id = @id",         "quiz_score.user_id" },
                new string[] { "DELETE FROM public.profile WHERE user_id = @id",            "profile.user_id" }
            };

            try
            {
                using (NpgsqlConnection conn = DatabaseHelper.GetConnection())
                {
                    // 1) Is the user still there? (and what is the name, for the message)
                    object found;
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT full_name FROM public.users WHERE user_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        found = cmd.ExecuteScalar();
                    }

                    if (found == null)
                    {
                        ShowMessage("That user was not found. Maybe it was already deleted.", false);
                        return;
                    }
                    string name = found.ToString();

                    // 2) Which of the lines above can we use? (only the tables that exist)
                    List<string> usable = new List<string>();
                    foreach (string[] line in belongsToUser)
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

                        using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM public.users WHERE user_id = @id", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();   // everything worked: save it for real
                    }

                    ShowMessage("Deleted! " + name + " was permanently removed.", true);
                }
            }
            catch (PostgresException ex)
            {
                // 23503 = "another table still uses this user" (for example the courses a tutor made)
                if (ex.SqlState == "23503")
                    ShowMessage("This user cannot be deleted yet, because they are still used in the \"" + ex.TableName +
                                "\" table (for example courses they created). Remove or change those first.", false);
                else
                    ShowMessage("Could not delete: " + ex.Message, false);
            }
            catch (Exception ex)
            {
                ShowMessage("Could not delete: " + ex.Message, false);
            }
        }

        // ----- Logout (the "Logout" option in the drop-down menu) -----
        protected void lnkLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();        // forget who was logged in
            Session.Abandon();      // end the session
            Response.Redirect("~/auth/login.aspx");   // go to the login page
        }


        // =====================================================
        //  HELPERS  (small tools used again and again)
        // =====================================================

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

        // Makes the "profile" table if it is not there yet.
        // Why? Supabase may have a rule (trigger) that fills the profile table
        // every time a new user is added. If the table is missing, we get error 42P01.
        // "IF NOT EXISTS" = do nothing when the table is already there (so it is safe to run every time).
        private void EnsureProfileTable()
        {
            try
            {
                RunSql(@"CREATE TABLE IF NOT EXISTS public.profile (
                            profile_id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            user_id             integer,
                            phone               text,
                            program             text,
                            learning_preference text,
                            daily_goal_hours    numeric,
                            high_contrast       boolean DEFAULT false,
                            large_font          boolean DEFAULT false,
                            reduced_motion      boolean DEFAULT false
                         )");

                // One profile per user. The database rule that fills this table uses
                // "ON CONFLICT (user_id)", and that only works when user_id is UNIQUE.
                // (Without this we get error 42P10.) Safe to run again and again.
                RunSql("CREATE UNIQUE INDEX IF NOT EXISTS profile_user_id_key ON public.profile (user_id)");
            }
            catch
            {
                // Could not create it (maybe no permission). Do nothing here,
                // the Add User code below will show the real error to the admin.
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

        // 79.6%
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