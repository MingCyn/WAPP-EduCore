using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WAPP_EduCore.pages.Student
{
    public partial class certificates : System.Web.UI.Page
    {
        // Malaysia is UTC+8. Dates are shown in local time.
        private const int UtcOffsetHours = 8;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public class CertCourse
        {
            public long CourseId { get; set; }
            public string Title { get; set; }
            public string Tutor { get; set; }
            public string LevelCategory { get; set; }
            public int Percent { get; set; }
            public string LessonsText { get; set; }
            public string QuizzesText { get; set; }
            public bool Eligible { get; set; }
            public bool HasCertificate { get; set; }
            public long CertId { get; set; }
            public string Code { get; set; }
            public string DateText { get; set; }
            public string ViewUrl { get; set; }
        }

        private long UserId { get { return Convert.ToInt64(Session["UserID"]); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }

            long viewId;
            if (long.TryParse(Request.QueryString["view"], out viewId))
            {
                ShowCertificate(viewId);
                return;
            }

            litHeading.Text = "Certificates";
            litSub.Text = "Your course completion certificates, and how close you are to the next one.";

            // The Claim Certificate buttons are plain HTML submit buttons: name="claim", value = course id
            long claimId;
            if (IsPostBack && long.TryParse(Request.Form["claim"], out claimId))
            {
                ClaimCertificate(claimId);
            }

            BindList();
        }

        // ---------- the list ----------
        private void BindList()
        {
            List<CertCourse> all;
            try
            {
                all = LoadCourses(0);
            }
            catch (Exception ex)
            {
                ShowError("Could not load your certificates: " + ex.Message);
                pnlList.Visible = false;
                return;
            }

            List<CertCourse> earned = all.Where(c => c.HasCertificate).ToList();
            List<CertCourse> ready = all.Where(c => !c.HasCertificate && c.Eligible).ToList();
            List<CertCourse> progress = all.Where(c => !c.HasCertificate && !c.Eligible)
                                           .OrderByDescending(c => c.Percent).ToList();

            rptReady.DataSource = ready;
            rptReady.DataBind();
            rptEarned.DataSource = earned;
            rptEarned.DataBind();
            rptProgress.DataSource = progress;
            rptProgress.DataBind();

            pnlReady.Visible = ready.Count > 0;
            pnlEarned.Visible = earned.Count > 0;
            pnlProgress.Visible = progress.Count > 0;
            pnlNone.Visible = all.Count == 0;

            litSummary.Text = all.Count == 0 ? "" :
                earned.Count + " earned \u00B7 " + ready.Count + " ready to claim \u00B7 " + progress.Count + " in progress";
        }

        // All enrolled courses with progress, quiz results and any certificate already issued.
        // courseId = 0 loads every enrolled course, otherwise just that course.
        private List<CertCourse> LoadCourses(long courseId)
        {
            var prm = new Dictionary<string, object> { { "@uid", UserId } };
            string filter = "";
            if (courseId > 0)
            {
                filter = " AND c.course_id = @cid";
                prm["@cid"] = courseId;
            }

            DataTable t = Db.Query(
                "SELECT c.course_id, c.title, c.category, c.course_level, COALESCE(u.full_name, 'TBA') AS tutor, " +
                "  (SELECT COUNT(*) FROM lesson l WHERE l.course_id = c.course_id) AS total_lessons, " +
                "  (SELECT COUNT(*) FROM lesson_progress lp JOIN lesson l2 ON l2.lesson_id = lp.lesson_id " +
                "     WHERE l2.course_id = c.course_id AND lp.user_id = @uid AND lp.is_completed) AS done_lessons, " +
                "  (SELECT COUNT(DISTINCT q.quiz_id) FROM quiz q " +
                "     JOIN lesson l3 ON (l3.lesson_id = q.lesson_id OR l3.quiz_id = q.quiz_id) " +
                "     WHERE l3.course_id = c.course_id) AS total_quizzes, " +
                "  (SELECT COUNT(DISTINCT q2.quiz_id) FROM quiz q2 " +
                "     JOIN lesson l4 ON (l4.lesson_id = q2.lesson_id OR l4.quiz_id = q2.quiz_id) " +
                "     JOIN quiz_score s ON s.quiz_id = q2.quiz_id AND s.user_id = @uid " +
                "     WHERE l4.course_id = c.course_id AND s.total_score >= COALESCE(q2.passing_score, 50)) AS passed_quizzes, " +
                "  ce.certificate_id, ce.certificate_code, ce.issued_date " +
                "FROM enrollment e " +
                "JOIN courses c ON c.course_id = e.course_id " +
                "LEFT JOIN users u ON u.user_id = c.user_id " +
                "LEFT JOIN certificates ce ON ce.course_id = c.course_id AND ce.user_id = e.user_id " +
                "WHERE e.user_id = @uid" + filter + " ORDER BY c.title", prm);

            var list = new List<CertCourse>();
            foreach (DataRow r in t.Rows)
            {
                long totalLessons = Convert.ToInt64(r["total_lessons"]);
                long doneLessons = Math.Min(Convert.ToInt64(r["done_lessons"]), totalLessons);
                long totalQuizzes = Convert.ToInt64(r["total_quizzes"]);
                long passedQuizzes = Math.Min(Convert.ToInt64(r["passed_quizzes"]), totalQuizzes);

                bool eligible = totalLessons > 0 && doneLessons >= totalLessons && passedQuizzes >= totalQuizzes;
                long denominator = totalLessons + totalQuizzes;
                int percent = eligible ? 100
                    : denominator > 0 ? (int)((doneLessons + passedQuizzes) * 100 / denominator) : 0;

                bool has = r["certificate_id"] != DBNull.Value;
                long certId = has ? Convert.ToInt64(r["certificate_id"]) : 0;

                list.Add(new CertCourse
                {
                    CourseId = Convert.ToInt64(r["course_id"]),
                    Title = Convert.ToString(r["title"]),
                    Tutor = Convert.ToString(r["tutor"]),
                    LevelCategory = JoinNonEmpty(" \u00B7 ", Convert.ToString(r["course_level"]), Convert.ToString(r["category"])),
                    Percent = percent,
                    LessonsText = totalLessons == 0 ? "No lessons yet" : doneLessons + " of " + totalLessons + " lessons",
                    QuizzesText = totalQuizzes == 0 ? "No quiz required" : passedQuizzes + " of " + totalQuizzes + " quizzes passed",
                    Eligible = eligible,
                    HasCertificate = has,
                    CertId = certId,
                    Code = has ? Convert.ToString(r["certificate_code"]) : "",
                    DateText = has ? ToLocal(r["issued_date"]).ToString("d MMMM yyyy", Inv) : "",
                    ViewUrl = has ? ResolveUrl("~/pages/Student/certificates.aspx?view=" + certId) : ""
                });
            }
            return list;
        }

        // ---------- claiming ----------
        private void ClaimCertificate(long courseId)
        {
            long certId = 0;
            try
            {
                // check again from the database, never trust the button alone
                CertCourse c = LoadCourses(courseId).FirstOrDefault();
                if (c == null) return;

                if (c.HasCertificate)
                {
                    certId = c.CertId;
                }
                else if (!c.Eligible)
                {
                    ShowError("This course is not complete yet. Finish every lesson and pass every quiz first.");
                    return;
                }
                else
                {
                    certId = IssueCertificate(courseId);
                }
            }
            catch (Exception ex)
            {
                ShowError("Could not issue the certificate: " + ex.Message);
                return;
            }

            if (certId > 0)
                Response.Redirect("~/pages/Student/certificates.aspx?view=" + certId);
        }

        private long IssueCertificate(long courseId)
        {
            DateTime localNow = DateTime.UtcNow.AddHours(UtcOffsetHours);

            for (int attempt = 0; attempt < 5; attempt++)
            {
                DataTable inserted = Db.Query(
                    "INSERT INTO certificates (user_id, course_id, certificate_code) " +
                    "VALUES (@uid, @cid, @code) ON CONFLICT DO NOTHING RETURNING certificate_id",
                    new Dictionary<string, object>
                    {
                        { "@uid", UserId }, { "@cid", courseId }, { "@code", NewCode(localNow) }
                    });
                if (inserted.Rows.Count > 0) return Convert.ToInt64(inserted.Rows[0]["certificate_id"]);

                // nothing inserted: the student already has it, or the random code collided
                DataTable existing = Db.Query(
                    "SELECT certificate_id FROM certificates WHERE user_id = @uid AND course_id = @cid",
                    new Dictionary<string, object> { { "@uid", UserId }, { "@cid", courseId } });
                if (existing.Rows.Count > 0) return Convert.ToInt64(existing.Rows[0]["certificate_id"]);
            }

            throw new InvalidOperationException("Could not generate a unique certificate code. Please try again.");
        }

        // EDU-20261022-K7Q2MX  (letters and digits without 0/O/1/I so the code is easy to read out)
        private static string NewCode(DateTime localNow)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // 32 characters
            byte[] bytes = new byte[6];
            using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(bytes);

            var sb = new StringBuilder("EDU-" + localNow.ToString("yyyyMMdd", Inv) + "-");
            foreach (byte b in bytes) sb.Append(alphabet[b % alphabet.Length]);
            return sb.ToString();
        }

        // ---------- one certificate ----------
        private void ShowCertificate(long certId)
        {
            DataTable t = Db.Query(
                "SELECT ce.certificate_code, ce.issued_date, c.title, c.category, c.course_level, " +
                "  u.full_name AS student, COALESCE(tu.full_name, 'EduCore Tutor') AS tutor " +
                "FROM certificates ce " +
                "JOIN courses c ON c.course_id = ce.course_id " +
                "JOIN users u ON u.user_id = ce.user_id " +
                "LEFT JOIN users tu ON tu.user_id = c.user_id " +
                "WHERE ce.certificate_id = @id AND ce.user_id = @uid",
                new Dictionary<string, object> { { "@id", certId }, { "@uid", UserId } });

            if (t.Rows.Count == 0)
            {
                Response.Redirect("~/pages/Student/certificates.aspx");
                return;
            }

            DataRow r = t.Rows[0];
            string student = HttpUtility.HtmlEncode(Convert.ToString(r["student"]));
            string course = HttpUtility.HtmlEncode(Convert.ToString(r["title"]));
            string tutor = HttpUtility.HtmlEncode(Convert.ToString(r["tutor"]));
            string code = HttpUtility.HtmlEncode(Convert.ToString(r["certificate_code"]));
            string date = HttpUtility.HtmlEncode(ToLocal(r["issued_date"]).ToString("d MMMM yyyy", Inv));
            string sub = HttpUtility.HtmlEncode(
                JoinNonEmpty(" \u00B7 ", Convert.ToString(r["course_level"]), Convert.ToString(r["category"])));

            litHeading.Text = "Certificate";
            litSub.Text = "Print it, or choose Save as PDF in the print window.";
            Page.Title = "Certificate - EduCore";

            litCert.Text =
                "<div class=\"cert\"><div class=\"cert-frame\">" +
                  "<div><div class=\"cert-brand\">EduCore</div><div class=\"cert-kicker\">Certificate of Completion</div></div>" +
                  "<div>" +
                    "<div class=\"cert-line\">This certifies that</div>" +
                    "<div class=\"cert-name\">" + student + "</div>" +
                    "<div class=\"cert-line\">has successfully completed the course</div>" +
                    "<div class=\"cert-course\">" + course + "</div>" +
                    (sub.Length > 0 ? "<div class=\"cert-sub\">" + sub + "</div>" : "") +
                  "</div>" +
                  "<div class=\"cert-foot\">" +
                    "<div><div class=\"cert-val\">" + date + "</div><div class=\"cert-lbl\">Date Issued</div></div>" +
                    "<div class=\"cert-seal\">&#10003;</div>" +
                    "<div><div class=\"cert-val\">" + tutor + "</div><div class=\"cert-lbl\">Course Tutor</div></div>" +
                  "</div>" +
                  "<div class=\"cert-id\">Certificate ID: " + code + "</div>" +
                "</div></div>";

            pnlList.Visible = false;
            pnlCert.Visible = true;
        }

        // ---------- helpers ----------
        private void ShowError(string text)
        {
            pnlError.Visible = true;
            litError.Text = text;
        }

        private static DateTime ToLocal(object utcValue)
        {
            if (utcValue == null || utcValue == DBNull.Value) return DateTime.MinValue;
            return DateTime.SpecifyKind(Convert.ToDateTime(utcValue), DateTimeKind.Utc).AddHours(UtcOffsetHours);
        }

        private static string JoinNonEmpty(string separator, params string[] parts)
        {
            return string.Join(separator, parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
}