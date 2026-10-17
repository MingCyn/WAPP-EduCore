using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;

namespace WAPP_EduCore.pages.Student
{
    public partial class WebForm5 : System.Web.UI.Page
    {
        // Malaysia is UTC+8. Activity is grouped by the student's local day.
        private const int UtcOffsetHours = 8;
        private const string Tz = "Asia/Kuala_Lumpur";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public class MilestoneItem
        {
            public string Title { get; set; }
            public string Sub { get; set; }
            public DateTime When { get; set; }
            public string DateText { get; set; }
        }

        private long UserId { get { return Convert.ToInt64(Session["UserID"]); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }

            if (!IsPostBack) LoadPage();
        }

        private void LoadPage()
        {
            var p = new Dictionary<string, object> { { "@uid", UserId } };
            DateTime today = DateTime.UtcNow.AddHours(UtcOffsetHours).Date;

            // ---------- stat cards ----------
            DataTable counts = Db.Query(
                "SELECT " +
                " (SELECT COUNT(*) FROM enrollment WHERE user_id = @uid) AS enrolled, " +
                " (SELECT COUNT(*) FROM certificates WHERE user_id = @uid) AS certs, " +
                " (SELECT COUNT(*) FROM lesson_progress WHERE user_id = @uid AND is_completed) AS lessons_done, " +
                " (SELECT COUNT(*) FROM lesson_progress WHERE user_id = @uid AND is_completed " +
                "    AND (completed_at AT TIME ZONE '" + Tz + "')::date >= (now() AT TIME ZONE '" + Tz + "')::date - 6) AS lessons_week", p);
            DataRow c = counts.Rows[0];

            // courses where every lesson is completed
            DataTable completedCourses = Db.Query(
                "SELECT c.course_id, c.title, c.category, MAX(lp.completed_at) AS happened " +
                "FROM enrollment e " +
                "JOIN courses c ON c.course_id = e.course_id " +
                "JOIN lesson l ON l.course_id = c.course_id " +
                "JOIN lesson_progress lp ON lp.lesson_id = l.lesson_id AND lp.user_id = e.user_id AND lp.is_completed " +
                "WHERE e.user_id = @uid " +
                "GROUP BY c.course_id, c.title, c.category " +
                "HAVING COUNT(DISTINCT l.lesson_id) = (SELECT COUNT(*) FROM lesson l2 WHERE l2.course_id = c.course_id)", p);

            litCompleted.Text = completedCourses.Rows.Count.ToString();
            litEnrolled.Text = Convert.ToString(c["enrolled"]);
            litLessons.Text = Convert.ToString(c["lessons_done"]);
            litLessonsWeek.Text = Convert.ToString(c["lessons_week"]);
            litCerts.Text = Convert.ToString(c["certs"]);

            // ---------- activity per day (lessons completed + quizzes taken) ----------
            DataTable act = Db.Query(
                "SELECT d, COUNT(*) AS n FROM (" +
                "  SELECT (completed_at AT TIME ZONE '" + Tz + "')::date AS d FROM lesson_progress " +
                "    WHERE user_id = @uid AND is_completed AND completed_at IS NOT NULL " +
                "  UNION ALL " +
                "  SELECT (done_time AT TIME ZONE '" + Tz + "')::date FROM quiz_score " +
                "    WHERE user_id = @uid AND done_time IS NOT NULL" +
                ") t GROUP BY d ORDER BY d", p);

            var days = new Dictionary<DateTime, int>();
            foreach (DataRow r in act.Rows)
                days[Convert.ToDateTime(r["d"]).Date] = Convert.ToInt32(r["n"]);

            // streak: if nothing yet today, the streak still counts up to yesterday
            DateTime cursor = days.ContainsKey(today) ? today : today.AddDays(-1);
            int current = 0;
            while (days.ContainsKey(cursor)) { current++; cursor = cursor.AddDays(-1); }

            int best = 0, run = 0;
            DateTime? prev = null;
            foreach (DateTime d in days.Keys.OrderBy(x => x))
            {
                run = (prev.HasValue && (d - prev.Value).Days == 1) ? run + 1 : 1;
                if (run > best) best = run;
                prev = d;
            }
            litStreak.Text = current.ToString();
            litBest.Text = Math.Max(best, current).ToString();

            // ---------- weekly bars (W1 = oldest, W8 = this week) ----------
            int[] weeks = new int[8];
            foreach (var kv in days)
            {
                int ago = (today - kv.Key).Days;
                if (ago >= 0 && ago < 56) weeks[7 - ago / 7] += kv.Value;
            }
            litWeekly.Text = BuildWeekly(weeks);

            // ---------- heatmap ----------
            litHeat.Text = BuildHeatmap(days, today);

            // ---------- skill radar: average best quiz score per category ----------
            DataTable skills = Db.Query(
                "SELECT c.category, ROUND(AVG(best.total_score)) AS avg_score " +
                "FROM (SELECT quiz_id, MAX(total_score) AS total_score FROM quiz_score " +
                "      WHERE user_id = @uid GROUP BY quiz_id) best " +
                "JOIN quiz q ON q.quiz_id = best.quiz_id " +
                "JOIN lesson l ON (l.lesson_id = q.lesson_id OR l.quiz_id = q.quiz_id) " +
                "JOIN courses c ON c.course_id = l.course_id " +
                "WHERE c.category IS NOT NULL AND c.category <> '' " +
                "GROUP BY c.category ORDER BY avg_score DESC LIMIT 6", p);
            litRadar.Text = BuildRadar(skills);

            // ---------- milestones ----------
            var items = new List<MilestoneItem>();

            foreach (DataRow r in completedCourses.Rows)
            {
                string cat = Convert.ToString(r["category"]);
                items.Add(new MilestoneItem
                {
                    Title = "Completed: " + Convert.ToString(r["title"]),
                    Sub = string.IsNullOrWhiteSpace(cat) ? "Course completed" : cat,
                    When = ToLocal(r["happened"])
                });
            }

            DataTable perfect = Db.Query(
                "SELECT q.quiz_title, " +
                "  (SELECT c.title FROM lesson l JOIN courses c ON c.course_id = l.course_id " +
                "     WHERE l.lesson_id = q.lesson_id OR l.quiz_id = q.quiz_id LIMIT 1) AS course_title, " +
                "  MIN(qs.done_time) AS happened " +
                "FROM quiz_score qs JOIN quiz q ON q.quiz_id = qs.quiz_id " +
                "WHERE qs.user_id = @uid AND qs.total_score >= 100 " +
                "GROUP BY q.quiz_id, q.quiz_title, q.lesson_id", p);
            foreach (DataRow r in perfect.Rows)
            {
                items.Add(new MilestoneItem
                {
                    Title = "Perfect Score: " + Convert.ToString(r["quiz_title"]),
                    Sub = Convert.ToString(r["course_title"]),
                    When = ToLocal(r["happened"])
                });
            }

            DataTable certs = Db.Query(
                "SELECT c.title, ce.issued_date FROM certificates ce " +
                "JOIN courses c ON c.course_id = ce.course_id WHERE ce.user_id = @uid", p);
            foreach (DataRow r in certs.Rows)
            {
                items.Add(new MilestoneItem
                {
                    Title = "Certificate Earned",
                    Sub = Convert.ToString(r["title"]),
                    When = ToLocal(r["issued_date"])
                });
            }

            var latest = items.OrderByDescending(m => m.When).Take(5).ToList();
            foreach (var m in latest) m.DateText = m.When.ToString("d MMM yyyy", Inv);

            rptMilestones.DataSource = latest;
            rptMilestones.DataBind();
            pnlNoMilestones.Visible = latest.Count == 0;
        }

        private DateTime ToLocal(object utcValue)
        {
            if (utcValue == null || utcValue == DBNull.Value) return DateTime.MinValue;
            DateTime dt = DateTime.SpecifyKind(Convert.ToDateTime(utcValue), DateTimeKind.Utc);
            return dt.AddHours(UtcOffsetHours);
        }

        // ---------- weekly bar chart ----------
        private string BuildWeekly(int[] weeks)
        {
            int max = weeks.Max();
            if (max == 0)
                return "<div class=\"gr-empty\">No activity in the last 8 weeks yet.</div>";

            var sb = new StringBuilder("<div class=\"wk\">");
            for (int i = 0; i < weeks.Length; i++)
            {
                int h = (int)Math.Round(170.0 * weeks[i] / max);
                if (h < 3) h = 3;
                sb.Append("<div class=\"wk-col\"><div class=\"wk-val\">" + weeks[i] + "</div>" +
                          "<div class=\"wk-bar\" style=\"height:" + h + "px\" title=\"" + weeks[i] + " activities\"></div>" +
                          "<div class=\"wk-lbl\">W" + (i + 1) + "</div></div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        // ---------- heatmap: 13 weeks, one column per week, one row per weekday ----------
        private string BuildHeatmap(Dictionary<DateTime, int> days, DateTime today)
        {
            DateTime monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            DateTime start = monday.AddDays(-12 * 7);

            var sb = new StringBuilder("<div class=\"hm-grid\">");
            for (int w = 0; w < 13; w++)
            {
                for (int d = 0; d < 7; d++)
                {
                    DateTime day = start.AddDays(w * 7 + d);
                    if (day > today)
                    {
                        sb.Append("<span class=\"hm hm-future\"></span>");
                        continue;
                    }

                    int n = days.ContainsKey(day) ? days[day] : 0;
                    int level = n == 0 ? 0 : n == 1 ? 1 : n == 2 ? 2 : n <= 4 ? 3 : 4;
                    sb.Append("<span class=\"hm hm" + level + "\" title=\"" +
                              day.ToString("d MMM yyyy", Inv) + ": " + n + (n == 1 ? " activity" : " activities") +
                              "\"></span>");
                }
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        // ---------- radar chart (needs 3 or more categories) ----------
        private string BuildRadar(DataTable skills)
        {
            int n = skills.Rows.Count;
            if (n == 0)
                return "<div class=\"gr-empty\">Complete a quiz to see your skills here.</div>";

            if (n < 3) return BuildSkillBars(skills);

            const double cx = 240, cy = 155, R = 100;
            var sb = new StringBuilder();
            sb.Append("<svg viewBox=\"0 0 480 310\" class=\"radar\" role=\"img\" aria-label=\"Average quiz score by subject\">");

            double[] rings = { 1.0, 0.75, 0.5, 0.25 };
            foreach (double f in rings)
            {
                sb.Append("<polygon points=\"" + Polygon(n, cx, cy, null, R * f) + "\" fill=\"" +
                          (f == 1.0 ? "#fbead6" : "none") + "\" stroke=\"#e6d3b5\" stroke-width=\"1\" />");
            }

            for (int i = 0; i < n; i++)
            {
                double a = Angle(i, n);
                sb.Append("<line x1=\"" + F(cx) + "\" y1=\"" + F(cy) + "\" x2=\"" + F(cx + R * Math.Cos(a)) +
                          "\" y2=\"" + F(cy + R * Math.Sin(a)) + "\" stroke=\"#e6d3b5\" stroke-width=\"1\" />");
            }

            double[] vals = new double[n];
            for (int i = 0; i < n; i++)
                vals[i] = Math.Max(0, Math.Min(100, Convert.ToDouble(skills.Rows[i]["avg_score"]))) / 100.0;

            sb.Append("<polygon points=\"" + Polygon(n, cx, cy, vals, R) +
                      "\" fill=\"rgba(80,104,200,0.25)\" stroke=\"#5068c8\" stroke-width=\"2\" />");

            for (int i = 0; i < n; i++)
            {
                double a = Angle(i, n);
                double lx = cx + (R + 16) * Math.Cos(a);
                double ly = cy + (R + 16) * Math.Sin(a);
                string anchor = Math.Cos(a) > 0.3 ? "start" : Math.Cos(a) < -0.3 ? "end" : "middle";
                double dy = Math.Sin(a) > 0.3 ? 12 : Math.Sin(a) < -0.3 ? -4 : 4;

                string label = Convert.ToString(skills.Rows[i]["category"]);
                if (label.Length > 18) label = label.Substring(0, 17) + "\u2026";
                label += " (" + Math.Round(vals[i] * 100) + "%)";

                sb.Append("<text x=\"" + F(lx) + "\" y=\"" + F(ly + dy) + "\" text-anchor=\"" + anchor +
                          "\" font-size=\"12\" fill=\"#16223b\">" + HttpUtility.HtmlEncode(label) + "</text>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        // fewer than 3 categories: plain bars with the same data
        private string BuildSkillBars(DataTable skills)
        {
            var sb = new StringBuilder("<div class=\"skill-list\">");
            foreach (DataRow r in skills.Rows)
            {
                double v = Math.Max(0, Math.Min(100, Convert.ToDouble(r["avg_score"])));
                sb.Append("<div class=\"skill-row\"><span class=\"skill-name\">" +
                          HttpUtility.HtmlEncode(Convert.ToString(r["category"])) +
                          "</span><div class=\"skill-track\"><div class=\"skill-fill\" style=\"width:" + F(v) +
                          "%\"></div></div><span class=\"skill-val\">" + F(v) + "%</span></div>");
            }
            sb.Append("</div><p class=\"gr-sub\" style=\"margin-top:14px\">Take quizzes in 3 or more subjects to unlock the radar chart.</p>");
            return sb.ToString();
        }

        private static double Angle(int i, int n)
        {
            return -Math.PI / 2 + i * 2 * Math.PI / n;
        }

        private static string Polygon(int n, double cx, double cy, double[] fractions, double radius)
        {
            var parts = new List<string>();
            for (int i = 0; i < n; i++)
            {
                double a = Angle(i, n);
                double r = fractions == null ? radius : radius * fractions[i];
                parts.Add(F(cx + r * Math.Cos(a)) + "," + F(cy + r * Math.Sin(a)));
            }
            return string.Join(" ", parts);
        }

        private static string F(double v)
        {
            return v.ToString("0.#", Inv);
        }
    }
}