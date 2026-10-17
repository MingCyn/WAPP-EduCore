using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WAPP_EduCore.pages.Student
{
    public partial class WebForm3 : System.Web.UI.Page
    {
        // ---------- small classes used by the repeaters ----------
        public class OptionItem
        {
            public string Letter { get; set; }
            public string Text { get; set; }
            public string Css { get; set; }
        }

        public class NavItem
        {
            public int Index { get; set; }
            public int Number { get; set; }
            public string Css { get; set; }
        }

        // ---------- state ----------
        private long _quizId;
        private DataRow _quiz;
        private DataTable _questions;
        protected bool IsLocked;   // used by the markup to disable the options

        private long UserId { get { return Convert.ToInt64(Session["UserID"]); } }

        private int Index
        {
            get { return ViewState["Idx"] == null ? 0 : (int)ViewState["Idx"]; }
            set { ViewState["Idx"] = value; }
        }
        private string[] Answers
        {
            get { return (string[])ViewState["Ans"]; }
            set { ViewState["Ans"] = value; }
        }
        private bool[] CheckedFlags
        {
            get { return (bool[])ViewState["Chk"]; }
            set { ViewState["Chk"] = value; }
        }

        // ---------- page load ----------
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/auth/login.aspx");
                return;
            }

            if (!long.TryParse(Request.QueryString["quiz"], out _quizId))
            {
                Response.Redirect("~/pages/Student/myCourses.aspx");
                return;
            }

            // quiz + the course it belongs to (works whichever table holds the link)
            DataTable qt = Db.Query(
                "SELECT q.quiz_id, q.quiz_title, q.passing_score, " +
                "  (SELECT l.course_id FROM lesson l " +
                "     WHERE l.lesson_id = q.lesson_id OR l.quiz_id = q.quiz_id LIMIT 1) AS course_id " +
                "FROM quiz q WHERE q.quiz_id = @qid",
                new Dictionary<string, object> { { "@qid", _quizId } });

            if (qt.Rows.Count == 0)
            {
                Response.Redirect("~/pages/Student/myCourses.aspx");
                return;
            }
            _quiz = qt.Rows[0];
            litQuizTitle.Text = Convert.ToString(_quiz["quiz_title"]);

            // only enrolled students may take the quiz
            if (_quiz["course_id"] != DBNull.Value)
            {
                DataTable en = Db.Query(
                    "SELECT 1 FROM enrollment WHERE user_id = @uid AND course_id = @cid",
                    new Dictionary<string, object> { { "@uid", UserId }, { "@cid", Convert.ToInt64(_quiz["course_id"]) } });
                if (en.Rows.Count == 0)
                {
                    Response.Redirect("~/pages/Student/courseOverview.aspx?id=" + Convert.ToInt64(_quiz["course_id"]));
                    return;
                }
            }

            // result screen after submitting (the page is reloaded with ?result=)
            long resultId;
            if (long.TryParse(Request.QueryString["result"], out resultId))
            {
                ShowResult(resultId);
                return;
            }

            _questions = Db.Query(
                "SELECT question_id, question_text, option_a, option_b, option_c, option_d, correct_answer, explanation " +
                "FROM quiz_questions WHERE quiz_id = @qid ORDER BY question_id",
                new Dictionary<string, object> { { "@qid", _quizId } });

            if (_questions.Rows.Count == 0)
            {
                pnlQuiz.Visible = false;
                chipBox.Visible = false;
                pnlEmpty.Visible = true;
                return;
            }

            if (!IsPostBack)
            {
                int n = _questions.Rows.Count;
                Answers = new string[n];
                CheckedFlags = new bool[n];
                Index = 0;
                RenderQuestion();
            }
        }

        // ---------- showing one question ----------
        private void RenderQuestion()
        {
            int n = _questions.Rows.Count;
            int i = Index;
            DataRow q = _questions.Rows[i];

            string chosen = Answers[i] ?? "";
            bool locked = CheckedFlags[i];
            string correct = CorrectLetter(q);
            IsLocked = locked;

            litNum.Text = (i + 1).ToString();
            litTotal.Text = n.ToString();
            litQNum.Text = (i + 1).ToString();
            litQuestion.Text = Convert.ToString(q["question_text"]);

            // option cards
            var options = new List<OptionItem>();
            string[] letters = { "A", "B", "C", "D" };
            string[] columns = { "option_a", "option_b", "option_c", "option_d" };
            for (int k = 0; k < 4; k++)
            {
                string text = Convert.ToString(q[columns[k]]);
                if (string.IsNullOrWhiteSpace(text)) continue;

                string css = "opt";
                if (chosen == letters[k]) css += " selected";
                if (locked)
                {
                    if (letters[k] == correct) css += " correct";
                    else if (chosen == letters[k]) css += " wrong";
                }
                options.Add(new OptionItem { Letter = letters[k], Text = text, Css = css });
            }
            rptOptions.DataSource = options;
            rptOptions.DataBind();

            // progress circles
            var nav = new List<NavItem>();
            for (int k = 0; k < n; k++)
            {
                string css = "dot";
                if (!string.IsNullOrEmpty(Answers[k])) css += " answered";
                if (k == i) css += " current";
                nav.Add(new NavItem { Index = k, Number = k + 1, Css = css });
            }
            rptNav.DataSource = nav;
            rptNav.DataBind();

            // buttons
            btnPrev.Visible = i > 0;
            btnCheck.Visible = !locked;
            btnNext.Visible = locked && i < n - 1;

            // explanation (shown only after Check Answer)
            pnlExplain.Visible = locked;
            if (locked)
            {
                bool ok = chosen == correct;
                pnlExplain.CssClass = "explain " + (ok ? "ok" : "bad");
                litExplainHead.Text = ok ? "Correct!" : "Incorrect. The correct answer is " + correct + ".";
                litExplain.Text = Convert.ToString(q["explanation"]);
            }
        }

        // The correct answer as a letter (accepts "B" or the full answer text)
        private string CorrectLetter(DataRow q)
        {
            string ca = Convert.ToString(q["correct_answer"]).Trim();
            string up = ca.ToUpperInvariant();
            if (up == "A" || up == "B" || up == "C" || up == "D") return up;

            string[] letters = { "A", "B", "C", "D" };
            string[] columns = { "option_a", "option_b", "option_c", "option_d" };
            for (int k = 0; k < 4; k++)
            {
                if (string.Equals(Convert.ToString(q[columns[k]]).Trim(), ca, StringComparison.OrdinalIgnoreCase))
                    return letters[k];
            }
            return up;
        }

        // ---------- events ----------
        protected void rptOptions_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "pick") return;
            if (CheckedFlags[Index]) return;   // locked after Check Answer

            string[] a = Answers;
            a[Index] = Convert.ToString(e.CommandArgument);
            Answers = a;
            lblMsg.Visible = false;
            RenderQuestion();
        }

        protected void btnCheck_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Answers[Index]))
            {
                lblMsg.Text = "Please choose an answer first.";
                lblMsg.Visible = true;
                RenderQuestion();
                return;
            }

            bool[] c = CheckedFlags;
            c[Index] = true;
            CheckedFlags = c;
            lblMsg.Visible = false;
            RenderQuestion();
        }

        protected void btnPrev_Click(object sender, EventArgs e)
        {
            if (Index > 0) Index = Index - 1;
            lblMsg.Visible = false;
            RenderQuestion();
        }

        protected void btnNext_Click(object sender, EventArgs e)
        {
            if (Index < _questions.Rows.Count - 1) Index = Index + 1;
            lblMsg.Visible = false;
            RenderQuestion();
        }

        protected void rptNav_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int target;
            if (int.TryParse(Convert.ToString(e.CommandArgument), out target)
                && target >= 0 && target < _questions.Rows.Count)
            {
                Index = target;
            }
            lblMsg.Visible = false;
            RenderQuestion();
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            int n = _questions.Rows.Count;
            string[] answers = Answers;

            int correctCount = 0;
            for (int k = 0; k < n; k++)
            {
                if (!string.IsNullOrEmpty(answers[k]) && answers[k] == CorrectLetter(_questions.Rows[k]))
                    correctCount++;
            }

            long percent = (long)Math.Round(correctCount * 100.0 / n);

            DataTable saved = Db.Query(
                "INSERT INTO quiz_score (quiz_id, user_id, total_score, grade, done_time) " +
                "VALUES (@qid, @uid, @score, @grade, now()) RETURNING score_id",
                new Dictionary<string, object>
                {
                    { "@qid", _quizId },
                    { "@uid", UserId },
                    { "@score", percent },
                    { "@grade", GradeFor(percent) }
                });

            long scoreId = Convert.ToInt64(saved.Rows[0]["score_id"]);

            // redirect so that refreshing the page cannot save the score twice
            Response.Redirect("~/pages/Student/assessment.aspx?quiz=" + _quizId + "&result=" + scoreId);
        }

        // ---------- result screen ----------
        private void ShowResult(long scoreId)
        {
            DataTable r = Db.Query(
                "SELECT total_score, grade FROM quiz_score " +
                "WHERE score_id = @sid AND user_id = @uid AND quiz_id = @qid",
                new Dictionary<string, object>
                {
                    { "@sid", scoreId }, { "@uid", UserId }, { "@qid", _quizId }
                });

            if (r.Rows.Count == 0)
            {
                Response.Redirect("~/pages/Student/assessment.aspx?quiz=" + _quizId);
                return;
            }

            long score = Convert.ToInt64(r.Rows[0]["total_score"]);
            long passMark = _quiz["passing_score"] == DBNull.Value ? 50 : Convert.ToInt64(_quiz["passing_score"]);

            litScore.Text = score.ToString();
            litGrade.Text = Convert.ToString(r.Rows[0]["grade"]);
            litResultText.Text = score >= passMark ? "Well done, you passed!" : "You did not pass this time.";

            lnkRetake.NavigateUrl = "~/pages/Student/assessment.aspx?quiz=" + _quizId;
            lnkBack.NavigateUrl = _quiz["course_id"] == DBNull.Value
                ? "~/pages/Student/myCourses.aspx"
                : "~/pages/Student/courseOverview.aspx?id=" + Convert.ToInt64(_quiz["course_id"]);

            pnlQuiz.Visible = false;
            chipBox.Visible = false;
            pnlResult.Visible = true;
        }

        // Change these cut-offs if your team uses a different grading scale
        private string GradeFor(long percent)
        {
            if (percent >= 80) return "A";
            if (percent >= 70) return "B";
            if (percent >= 60) return "C";
            if (percent >= 50) return "D";
            return "F";
        }
    }
}