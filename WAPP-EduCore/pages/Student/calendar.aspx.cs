using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WAPP_EduCore.pages.Student
{
    public partial class calendar : System.Web.UI.Page
    {
        // Malaysia is UTC+8. The database does the time zone conversion, so no hour maths is done here.
        private const int UtcOffsetHours = 8;
        private const string Tz = "Asia/Kuala_Lumpur";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private const int StartHour = 8;
        private const int SlotCount = 7;
        private const double SlotPx = 64;
        private const double PitchPx = 70;

        public class UpItem
        {
            public long Id { get; set; }
            public string Type { get; set; }
            public string Css { get; set; }
            public string Title { get; set; }
            public string When { get; set; }
        }

        private class Ev
        {
            public long Id;
            public string Title;
            public string Type;
            public string Course;
            public DateTime Start;   // Malaysia wall-clock time
            public DateTime End;     // Malaysia wall-clock time
        }

        private string _view;
        private DateTime _anchor;
        private DateTime _today;

        private bool IsGuest { get { return Session["UserID"] == null; } }
        private long UserId { get { return Convert.ToInt64(Session["UserID"]); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            _today = DateTime.UtcNow.AddHours(UtcOffsetHours).Date;

            string v = (Request.QueryString["view"] ?? "week").ToLowerInvariant();
            _view = (v == "month" || v == "day") ? v : "week";

            DateTime d;
            _anchor = DateTime.TryParseExact(Request.QueryString["date"], "yyyy-MM-dd", Inv,
                DateTimeStyles.None, out d) ? d.Date : _today;

            if (!IsPostBack)
            {
                txtDate.Text = _anchor.ToString("yyyy-MM-dd", Inv);
                txtStart.Text = "09:00";
                txtEnd.Text = "10:00";
                if (!IsGuest)
                    LoadCourseList();
            }

            BuildCalendar();
            if (!IsPostBack) BindUpcoming();
        }

        private void BuildCalendar()
        {
            DateTime rangeStart, rangeEnd, prev, next;
            string title;
            int weeks = 0;

            if (_view == "day")
            {
                rangeStart = _anchor;
                rangeEnd = _anchor.AddDays(1);
                prev = _anchor.AddDays(-1);
                next = _anchor.AddDays(1);
                title = _anchor.ToString("dddd, d MMMM yyyy", Inv);
            }
            else if (_view == "month")
            {
                DateTime first = new DateTime(_anchor.Year, _anchor.Month, 1);
                int lead = ((int)first.DayOfWeek + 6) % 7;
                weeks = (int)Math.Ceiling((lead + DateTime.DaysInMonth(first.Year, first.Month)) / 7.0);
                rangeStart = first.AddDays(-lead);
                rangeEnd = rangeStart.AddDays(weeks * 7);
                prev = first.AddMonths(-1);
                next = first.AddMonths(1);
                title = first.ToString("MMMM yyyy", Inv);
            }
            else
            {
                DateTime mon = MondayOf(_anchor);
                DateTime sun = mon.AddDays(6);
                rangeStart = mon;
                rangeEnd = mon.AddDays(7);
                prev = mon.AddDays(-7);
                next = mon.AddDays(7);
                title = mon.Month == sun.Month
                    ? mon.ToString("MMMM d", Inv) + "-" + sun.Day + ", " + sun.Year
                    : mon.ToString("MMM d", Inv) + " - " + sun.ToString("MMM d, yyyy", Inv);
            }

            litTitle.Text = title;
            litNav.Text =
                "<span class=\"cl-nav\"><a href=\"" + Href(_view, prev) + "\" title=\"Previous\">&lt;</a>" +
                "<a href=\"" + Href(_view, next) + "\" title=\"Next\">&gt;</a></span>" +
                "<a class=\"cl-today\" href=\"" + Href(_view, _today) + "\">Today</a>";

            var views = new StringBuilder();
            string[] keys = { "month", "week", "day" };
            string[] labels = { "Month", "Week", "Day" };
            for (int i = 0; i < keys.Length; i++)
            {
                views.Append("<a class=\"" + (keys[i] == _view ? "active" : "") + "\" href=\"" +
                           Href(keys[i], _anchor) + "\">" + labels[i] + "</a>");
            }
            litViews.Text = views.ToString();

            try
            {
                List<Ev> evs = IsGuest ? new List<Ev>() : LoadEvents(rangeStart, rangeEnd);
                litGrid.Text = _view == "month"
                    ? BuildMonth(rangeStart, weeks, evs)
                    : BuildTimeGrid(rangeStart, _view == "day" ? 1 : 7, evs);
            }
            catch (Exception ex)
            {
                litGrid.Text = "<div class=\"cl-empty\">Could not load the calendar: " +
                               HttpUtility.HtmlEncode(ex.Message) +
                               "<br />Check that the event_type column exists (see the setup SQL).</div>";
            }
        }

        private List<Ev> LoadEvents(DateTime rangeStart, DateTime rangeEnd)
        {
            if (IsGuest) return new List<Ev>();

            DataTable t = Db.Query(
                "SELECT e.event_id, e.event_title, e.event_type, " +
                "  (e.start_time AT TIME ZONE '" + Tz + "') AS start_local, " +
                "  (e.end_time AT TIME ZONE '" + Tz + "') AS end_local, " +
                "  c.title AS course_title " +
                "FROM calendar_events e LEFT JOIN courses c ON c.course_id = e.course_id " +
                "WHERE e.user_id = @uid " +
                "  AND e.start_time >= (CAST(@from AS timestamp) AT TIME ZONE '" + Tz + "') " +
                "  AND e.start_time <  (CAST(@to AS timestamp) AT TIME ZONE '" + Tz + "') " +
                "ORDER BY e.start_time",
                new Dictionary<string, object>
                {
                    { "@uid", UserId },
                    { "@from", rangeStart.ToString("yyyy-MM-dd HH:mm:ss", Inv) },
                    { "@to", rangeEnd.ToString("yyyy-MM-dd HH:mm:ss", Inv) }
                });

            var list = new List<Ev>();
            foreach (DataRow r in t.Rows)
            {
                list.Add(new Ev
                {
                    Id = Convert.ToInt64(r["event_id"]),
                    Title = Convert.ToString(r["event_title"]),
                    Type = Convert.ToString(r["event_type"]),
                    Course = Convert.ToString(r["course_title"]),
                    Start = Convert.ToDateTime(r["start_local"]),
                    End = Convert.ToDateTime(r["end_local"])
                });
            }
            return list;
        }

        private string BuildTimeGrid(DateTime start, int dayCount, List<Ev> evs)
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"cl-grid\" style=\"--n:" + dayCount + "\">");

            sb.Append("<div></div>");
            for (int d = 0; d < dayCount; d++)
            {
                DateTime day = start.AddDays(d);
                sb.Append("<a class=\"cl-dayhead" + (day == _today ? " today" : "") + "\" href=\"" +
                          Href("day", day) + "\">" + day.ToString("ddd d", Inv) + "</a>");
            }

            sb.Append("<div class=\"cl-times\">");
            for (int s = 0; s < SlotCount; s++)
            {
                int hour = StartHour + 2 * s;
                int h12 = hour % 12 == 0 ? 12 : hour % 12;
                sb.Append("<div class=\"cl-time\">" + h12 + (hour < 12 ? " AM" : " PM") + "</div>");
            }
            sb.Append("</div>");

            for (int d = 0; d < dayCount; d++)
            {
                DateTime day = start.AddDays(d);
                sb.Append("<div class=\"cl-col\">");
                for (int s = 0; s < SlotCount; s++) sb.Append("<div class=\"cl-slot\"></div>");

                List<Ev> dayEvs = evs.Where(x => x.Start.Date == day).OrderBy(x => x.Start).ToList();

                var laneEnds = new List<DateTime>();
                var laneOf = new int[dayEvs.Count];
                for (int i = 0; i < dayEvs.Count; i++)
                {
                    int lane = -1;
                    for (int l = 0; l < laneEnds.Count; l++)
                    {
                        if (laneEnds[l] <= dayEvs[i].Start) { lane = l; break; }
                    }
                    if (lane < 0) { laneEnds.Add(DateTime.MinValue); lane = laneEnds.Count - 1; }
                    laneEnds[lane] = EffectiveEnd(dayEvs[i]);
                    laneOf[i] = lane;
                }
                int lanes = Math.Max(1, laneEnds.Count);

                for (int i = 0; i < dayEvs.Count; i++)
                {
                    Ev ev = dayEvs[i];
                    double total = SlotCount * 120.0;
                    double startMin = ev.Start.Hour * 60 + ev.Start.Minute - StartHour * 60;
                    double endMin = ev.End.Date > ev.Start.Date
                        ? total
                        : ev.End.Hour * 60 + ev.End.Minute - StartHour * 60;
                    if (endMin <= startMin) endMin = startMin + 30;

                    startMin = Math.Max(0, Math.Min(total - 15, startMin));
                    endMin = Math.Max(startMin + 15, Math.Min(total, endMin));

                    double top = startMin / 120.0 * PitchPx;
                    double height = Math.Max(26, (endMin - startMin) / 120.0 * PitchPx - 2);
                    double width = 100.0 / lanes;
                    double left = laneOf[i] * width;

                    string time = ev.Start.ToString("HH:mm", Inv) + " - " + ev.End.ToString("HH:mm", Inv);
                    string tip = ev.Title + " (" + time + ")" +
                                 (string.IsNullOrWhiteSpace(ev.Course) ? "" : " - " + ev.Course);

                    sb.Append("<div class=\"cl-ev " + TypeCss(ev.Type) + "\" style=\"top:" + F(top) +
                              "px;height:" + F(height) + "px;left:calc(" + F(left) + "% + 2px);width:calc(" +
                              F(width) + "% - 4px)\" title=\"" + HttpUtility.HtmlAttributeEncode(tip) + "\">" +
                              "<strong>" + HttpUtility.HtmlEncode(ev.Title) + "</strong><small>" + time + "</small></div>");
                }

                sb.Append("</div>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        private string BuildMonth(DateTime gridStart, int weeks, List<Ev> evs)
        {
            Dictionary<DateTime, List<Ev>> byDay = evs
                .GroupBy(x => x.Start.Date)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Start).ToList());

            var sb = new StringBuilder("<div class=\"cl-month\">");
            string[] names = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            foreach (string n in names) sb.Append("<div class=\"cl-mhead\">" + n + "</div>");

            for (int i = 0; i < weeks * 7; i++)
            {
                DateTime day = gridStart.AddDays(i);
                string cls = "cl-mcell" + (day.Month != _anchor.Month ? " muted" : "") + (day == _today ? " today" : "");

                sb.Append("<div class=\"" + cls + "\"><a class=\"cl-mnum\" href=\"" + Href("day", day) + "\">" +
                          day.Day + "</a>");

                List<Ev> list;
                if (byDay.TryGetValue(day, out list))
                {
                    foreach (Ev ev in list.Take(3))
                    {
                        sb.Append("<div class=\"cl-chip " + TypeCss(ev.Type) + "\" title=\"" +
                                  HttpUtility.HtmlAttributeEncode(ev.Title) + "\">" +
                                  ev.Start.ToString("HH:mm", Inv) + " " + HttpUtility.HtmlEncode(ev.Title) + "</div>");
                    }
                    if (list.Count > 3)
                        sb.Append("<div class=\"cl-more\">+" + (list.Count - 3) + " more</div>");
                }
                sb.Append("</div>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        private void BindUpcoming()
        {
            var items = new List<UpItem>();

            if (IsGuest)
            {
                rptUpcoming.DataSource = items;
                rptUpcoming.DataBind();
                pnlNoUpcoming.Visible = true;
                return;
            }

            try
            {
                DataTable t = Db.Query(
                    "SELECT event_id, event_title, event_type, " +
                    "  (start_time AT TIME ZONE '" + Tz + "') AS start_local " +
                    "FROM calendar_events " +
                    "WHERE user_id = @uid AND start_time >= now() ORDER BY start_time LIMIT 5",
                    new Dictionary<string, object> { { "@uid", UserId } });

                foreach (DataRow r in t.Rows)
                {
                    string type = Convert.ToString(r["event_type"]);
                    items.Add(new UpItem
                    {
                        Id = Convert.ToInt64(r["event_id"]),
                        Type = type,
                        Css = TypeCss(type),
                        Title = Convert.ToString(r["event_title"]),
                        When = Convert.ToDateTime(r["start_local"]).ToString("MMM d, h:mm tt", Inv)
                    });
                }
            }
            catch (Exception) { }

            rptUpcoming.DataSource = items;
            rptUpcoming.DataBind();
            pnlNoUpcoming.Visible = items.Count == 0;
        }

        protected void rptUpcoming_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "delete") return;

            if (IsGuest)
            {
                FormError("Please log in now to unlock all functions and manage events!");
                return;
            }

            long id;
            if (long.TryParse(Convert.ToString(e.CommandArgument), out id))
            {
                Db.Execute(
                    "DELETE FROM calendar_events WHERE event_id = @id AND user_id = @uid",
                    new Dictionary<string, object> { { "@id", id }, { "@uid", UserId } });
            }
            Response.Redirect(Request.RawUrl);
        }

        private void LoadCourseList()
        {
            ddlCourse.Items.Clear();
            ddlCourse.Items.Add(new ListItem("No course (personal)", ""));

            DataTable t = Db.Query(
                "SELECT c.course_id, c.title FROM courses c " +
                "JOIN enrollment e ON e.course_id = c.course_id " +
                "WHERE e.user_id = @uid ORDER BY c.title",
                new Dictionary<string, object> { { "@uid", UserId } });

            foreach (DataRow r in t.Rows)
                ddlCourse.Items.Add(new ListItem(Convert.ToString(r["title"]), Convert.ToString(r["course_id"])));
        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            if (IsGuest)
            {
                FormError("Please log in now to unlock all functions and add custom events!");
                return;
            }

            string title = txtTitle.Text.Trim();
            if (title.Length == 0) { FormError("Please enter an event title."); return; }

            string dateText = txtDate.Text.Trim();
            DateTime startLocal, endLocal;
            if (!DateTime.TryParseExact(dateText + " " + txtStart.Text.Trim(), "yyyy-MM-dd HH:mm", Inv,
                    DateTimeStyles.None, out startLocal))
            { FormError("Please enter a valid date and start time."); return; }
            if (!DateTime.TryParseExact(dateText + " " + txtEnd.Text.Trim(), "yyyy-MM-dd HH:mm", Inv,
                    DateTimeStyles.None, out endLocal))
            { FormError("Please enter a valid end time."); return; }
            if (endLocal <= startLocal) { FormError("The end time must be after the start time."); return; }

            try
            {
                object courseId = DBNull.Value;
                long cid;
                if (long.TryParse(ddlCourse.SelectedValue, out cid))
                {
                    DataTable en = Db.Query(
                        "SELECT 1 FROM enrollment WHERE user_id = @uid AND course_id = @cid",
                        new Dictionary<string, object> { { "@uid", UserId }, { "@cid", cid } });
                    if (en.Rows.Count > 0) courseId = cid;
                }

                // Save event into EduCore database
                Db.Execute(
                    "INSERT INTO calendar_events (user_id, course_id, event_title, event_type, start_time, end_time) " +
                    "VALUES (@uid, @cid, @t, @type, " +
                    "  (CAST(@s AS timestamp) AT TIME ZONE '" + Tz + "'), " +
                    "  (CAST(@e AS timestamp) AT TIME ZONE '" + Tz + "'))",
                    new Dictionary<string, object>
                    {
                        { "@uid", UserId },
                        { "@cid", courseId },
                        { "@t", title },
                        { "@type", ddlType.SelectedValue },
                        { "@s", startLocal.ToString("yyyy-MM-dd HH:mm:ss", Inv) },
                        { "@e", endLocal.ToString("yyyy-MM-dd HH:mm:ss", Inv) }
                    });

                // Automatically push event to user's real-time Google Calendar via API
                // Automatically push event to user's real-time Google Calendar via API
                try
                {
                    PushEventToGoogleCalendar(UserId, title, startLocal, endLocal);
                }
                catch (Exception ex)
                {
                    // CHANGE THIS TO SHOW THE ERROR:
                    FormError("Saved locally, but Google API failed: " + ex.Message);
                    return;
                }
            }
            catch (Exception ex)
            {
                FormError("Could not save the event: " + ex.Message);
                return;
            }

            Response.Redirect(ResolveUrl("~/pages/Student/calendar.aspx?view=" + _view +
                                     "&date=" + startLocal.ToString("yyyy-MM-dd", Inv)));
        }

        // Real-time synchronization helper method to push event to Google Calendar API
        private void PushEventToGoogleCalendar(long userId, string title, DateTime startLocal, DateTime endLocal)
        {
            DataTable dt = Db.Query("SELECT google_access_token FROM public.users WHERE user_id = @uid",
                                    new Dictionary<string, object> { { "@uid", userId } });

            if (dt.Rows.Count == 0 || dt.Rows[0]["google_access_token"] == DBNull.Value)
            {
                return; // User did not log in via Google or didn't grant token permissions
            }

            string accessToken = Convert.ToString(dt.Rows[0]["google_access_token"]);

            // Change "primary" to your specific Google email address, or keep "primary" 
            string calendarId = "primary"; // or "joslyn.cyn05@gmail.com"
            string url = $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var eventData = new Dictionary<string, object>
    {
        { "summary", title },
        { "start", new Dictionary<string, string> { { "dateTime", startLocal.ToString("yyyy-MM-dd'T'HH:mm:ss") }, { "timeZone", Tz } } },
        { "end", new Dictionary<string, string> { { "dateTime", endLocal.ToString("yyyy-MM-dd'T'HH:mm:ss") }, { "timeZone", Tz } } }
    };

            string jsonPayload = new JavaScriptSerializer().Serialize(eventData);

            using (var writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(jsonPayload);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                // Event successfully written to Google Calendar
            }
        }

        protected void btnExportIcs_Click(object sender, EventArgs e)
        {
            if (IsGuest)
            {
                ClientScript.RegisterStartupScript(GetType(), "openLoginPrompt", "openLoginPrompt();", true);
                return;
            }

            // Fetch all user events from the database
            DataTable t = Db.Query(
                "SELECT event_title, start_time, end_time FROM calendar_events WHERE user_id = @uid",
                new Dictionary<string, object> { { "@uid", UserId } });

            int successCount = 0;
            foreach (DataRow r in t.Rows)
            {
                string title = Convert.ToString(r["event_title"]);
                DateTime start = Convert.ToDateTime(r["start_time"]);
                DateTime end = Convert.ToDateTime(r["end_time"]);

                try
                {
                    // Push each event directly to the user's Google Calendar via API
                    PushEventToGoogleCalendar(UserId, title, start, end);
                    successCount++;
                }
                catch (Exception)
                {
                    // Skip failed ones without breaking the loop
                }
            }

            // Refresh the page with a message or alert indicating successful sync
            ScriptManager.RegisterStartupScript(this, GetType(), "syncAlert", $"alert('Successfully synced {successCount} events to your Google Calendar!');", true);
        }

        private void FormError(string text)
        {
            if (IsGuest)
            {
                ClientScript.RegisterStartupScript(GetType(), "openLoginPrompt", "openLoginPrompt();", true);
            }
            else
            {
                lblFormError.Text = HttpUtility.HtmlEncode(text);
                lblFormError.Visible = true;
                ClientScript.RegisterStartupScript(GetType(), "openAdd", "openAdd();", true);
            }
        }

        private static DateTime MondayOf(DateTime d)
        {
            return d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
        }

        private static DateTime EffectiveEnd(Ev ev)
        {
            return ev.End > ev.Start ? ev.End : ev.Start.AddMinutes(30);
        }

        private string Href(string view, DateTime date)
        {
            return HttpUtility.HtmlAttributeEncode(ResolveUrl(
                "~/pages/Student/calendar.aspx?view=" + view + "&date=" + date.ToString("yyyy-MM-dd", Inv)));
        }

        private static string TypeCss(string type)
        {
            switch ((type ?? "").ToLowerInvariant())
            {
                case "live class": return "ev-live";
                case "deadline": return "ev-deadline";
                case "exam": return "ev-exam";
                case "self-study": return "ev-study";
                default: return "ev-other";
            }
        }

        private static string F(double v)
        {
            return v.ToString("0.##", Inv);
        }
    }
}