using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web.Script.Serialization;

namespace WAPP_EduCore.pages.General
{
    public partial class quiz : System.Web.UI.Page
    {
        // ------------------------------------------------------------------
        // Settings you may want to tweak
        // ------------------------------------------------------------------
        private const int SecondsPerQuestion = 60;   // total time = questions x this (min 5 minutes)
        private const int DefaultPassPercent = 50;   // used when quiz.passing_score is empty

        // Page the "Back to Lesson" button opens, e.g. lesson.aspx?lesson_id=3 (change to your real page)
        private const string LessonPagePath = "~/pages/General/lesson.aspx";
        private const string CoursesPagePath = "~/pages/General/courses.aspx";

        // Session keys checked to decide whether the visitor is signed in.
        // Change these to whatever your login page stores.
        private static readonly string[] UserIdSessionKeys = { "UserId", "user_id", "userId" };

        // ------------------------------------------------------------------

        protected long QuizIdForLinks { get { return QuizId; } }

        /// <summary>URL for the "Back to Lesson" button (set when the quiz is graded).</summary>
        protected string LessonUrl { get; private set; }

        // Result dialog look & wording, driven by the grade
        protected string ResultGrade { get; private set; }
        protected string ResultTitle { get; private set; }
        protected string ResultIcon { get; private set; }   // HTML entity, safe to output as-is

        private long QuizId
        {
            get
            {
                long id;
                return long.TryParse(Request.QueryString["quiz_id"], out id) ? id : 0;
            }
        }

        /// <summary>Returns the signed-in user's id, or null for a general (guest) visitor.</summary>
        private long? CurrentUserId()
        {
            foreach (string key in UserIdSessionKeys)
            {
                object value = Session[key];
                long id;
                if (value != null && long.TryParse(Convert.ToString(value), out id) && id > 0)
                    return id;
            }
            return null;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            if (QuizId <= 0)
            {
                ShowError("No quiz was selected. Open a quiz from one of your lessons.");
                return;
            }

            DataTable quizTable = Db.Query(
                "SELECT quiz_id, quiz_title FROM quiz WHERE quiz_id = @id",
                new Dictionary<string, object> { { "@id", QuizId } });

            if (quizTable.Rows.Count == 0)
            {
                ShowError("This quiz doesn't exist or has been removed.");
                return;
            }

            DataTable questions = Db.Query(
                @"SELECT question_id, question_text, option_a, option_b, option_c, option_d
                  FROM quiz_questions
                  WHERE quiz_id = @id
                  ORDER BY question_id",
                new Dictionary<string, object> { { "@id", QuizId } });

            if (questions.Rows.Count == 0)
            {
                ShowError("This quiz doesn't have any questions yet.");
                return;
            }

            litQuizTitle.Text = Server.HtmlEncode(Convert.ToString(quizTable.Rows[0]["quiz_title"]));
            RenderQuizScript(questions);
        }

        /// <summary>Sends the questions (no correct answers) to the browser as JSON.</summary>
        private void RenderQuizScript(DataTable questions)
        {
            var list = new List<object>();
            foreach (DataRow r in questions.Rows)
            {
                var options = new List<object>();
                AddOption(options, "A", r["option_a"]);
                AddOption(options, "B", r["option_b"]);
                AddOption(options, "C", r["option_c"]);
                AddOption(options, "D", r["option_d"]);

                list.Add(new Dictionary<string, object>
                {
                    { "id", Convert.ToString(r["question_id"]) },
                    { "text", Convert.ToString(r["question_text"]) },
                    { "options", options }
                });
            }

            int seconds = Math.Max(300, questions.Rows.Count * SecondsPerQuestion);

            var payload = new Dictionary<string, object>
            {
                { "questions", list },
                { "seconds", seconds },
                { "hiddenId", hfAnswers.ClientID },
                { "buttonId", btnSubmit.ClientID }
            };

            string json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(payload);
            json = json.Replace("<", "\\u003c").Replace(">", "\\u003e"); // keep </script> from breaking out

            litScript.Text = "<script>var QUIZ = " + json + ";</script>";
        }

        private static void AddOption(List<object> options, string key, object text)
        {
            string t = text == null || text == DBNull.Value ? "" : Convert.ToString(text);
            if (t.Length == 0) return; // skip empty options
            options.Add(new Dictionary<string, object> { { "key", key }, { "text", t } });
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            if (QuizId <= 0) { ShowError("No quiz was selected."); return; }

            // 1. Read the user's answers: { "questionId": "A", ... }
            Dictionary<string, string> answers;
            try
            {
                string raw = string.IsNullOrWhiteSpace(hfAnswers.Value) ? "{}" : hfAnswers.Value;
                answers = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(raw)
                          ?? new Dictionary<string, string>();
            }
            catch
            {
                answers = new Dictionary<string, string>();
            }

            // 2. Load correct answers and grade on the server (also builds the review)
            DataTable key = Db.Query(
                @"SELECT question_id, question_text, option_a, option_b, option_c, option_d,
                         correct_answer, explanation
                  FROM quiz_questions
                  WHERE quiz_id = @id
                  ORDER BY question_id",
                new Dictionary<string, object> { { "@id", QuizId } });

            int total = key.Rows.Count;
            if (total == 0) { ShowError("This quiz doesn't have any questions yet."); return; }

            int correct = 0;
            int number = 0;
            var review = new StringBuilder();
            foreach (DataRow r in key.Rows)
            {
                number++;
                string picked;
                answers.TryGetValue(Convert.ToString(r["question_id"]), out picked);
                picked = string.IsNullOrWhiteSpace(picked) ? null : picked.Trim().ToUpperInvariant();

                string right = CorrectLetter(r);
                bool isRight = picked != null && right != null && picked == right;
                if (isRight) correct++;

                review.Append(BuildReviewItem(number, r, picked, right, isRight));
            }

            int percent = (int)Math.Round(correct * 100.0 / total, MidpointRounding.AwayFromZero);
            string grade = ToGrade(percent);

            DataTable quizRow = Db.Query(
                "SELECT passing_score, lesson_id FROM quiz WHERE quiz_id = @id",
                new Dictionary<string, object> { { "@id", QuizId } });

            int passMark = DefaultPassPercent;
            if (quizRow.Rows.Count > 0 && quizRow.Rows[0]["passing_score"] != DBNull.Value)
                passMark = Convert.ToInt32(quizRow.Rows[0]["passing_score"]);
            bool passed = percent >= passMark;

            LessonUrl = CoursesPagePath.StartsWith("~") ? ResolveUrl(CoursesPagePath) : CoursesPagePath;
            if (quizRow.Rows.Count > 0 && quizRow.Rows[0]["lesson_id"] != DBNull.Value)
                LessonUrl = ResolveUrl(LessonPagePath) + "?lesson_id=" + Convert.ToInt64(quizRow.Rows[0]["lesson_id"]);

            // 3. Save ONLY if the visitor is signed in
            string saveNote;
            long? userId = CurrentUserId();
            if (userId.HasValue)
            {
                try
                {
                    Db.Query(
                        @"INSERT INTO quiz_score (quiz_id, user_id, total_score, grade, done_time)
                          VALUES (@quiz, @user, @score, @grade, NOW())
                          RETURNING score_id",
                        new Dictionary<string, object>
                        {
                            { "@quiz", QuizId },
                            { "@user", userId.Value },
                            { "@score", (long)correct },
                            { "@grade", grade }
                        });
                    saveNote = "Your result has been saved to your account.";
                }
                catch (Exception)
                {
                    saveNote = "We couldn't save your result this time, but your score is shown above.";
                }
            }
            else
            {
                saveNote = "You're not signed in, so this result won't be saved. Sign in to keep track of your scores.";
            }

            // 4. Show the congratulations dialog
            ResultGrade = grade;
            switch (grade)
            {
                case "A": ResultTitle = "Outstanding!"; ResultIcon = "&#127942;"; break; // trophy
                case "B": ResultTitle = "Great job!"; ResultIcon = "&#127881;"; break; // party popper
                case "C": ResultTitle = "Well done!"; ResultIcon = "&#11088;"; break; // star
                case "D": ResultTitle = "Good effort!"; ResultIcon = "&#128170;"; break; // flexed arm
                default: ResultTitle = "Keep going!"; ResultIcon = "&#128218;"; break; // books
            }

            litGrade.Text = Server.HtmlEncode(grade);
            litScore.Text = string.Format("You scored {0} / {1} ({2}%)", correct, total, percent);
            litVerdict.Text = passed
                ? "<span class=\"qz-verdict pass\">Passed</span>"
                : "<span class=\"qz-verdict fail\">Not passed yet - give it another try</span>";
            litSaveNote.Text = Server.HtmlEncode(saveNote);
            litReview.Text = review.ToString();
            litReviewSummary.Text = string.Format("{0} of {1} correct", correct, total);

            pnlQuiz.Visible = false;
            pnlError.Visible = false;
            pnlResult.Visible = true;
        }

        /// <summary>
        /// Returns the letter (A-D) of the correct option. Works whether correct_answer
        /// stores a letter ("B", "option_b") or the option's text. Null if it can't be matched.
        /// </summary>
        private static string CorrectLetter(DataRow row)
        {
            string value = row["correct_answer"] == DBNull.Value ? "" : Convert.ToString(row["correct_answer"]).Trim();
            if (value.Length == 0) return null;

            string normalized = value.ToUpperInvariant().Replace("OPTION_", "").Replace("OPTION ", "").Trim();
            if (normalized == "A" || normalized == "B" || normalized == "C" || normalized == "D")
                return normalized;

            foreach (string letter in new[] { "A", "B", "C", "D" })
            {
                object o = row["option_" + letter.ToLowerInvariant()];
                string text = o == DBNull.Value ? "" : Convert.ToString(o).Trim();
                if (text.Length > 0 && string.Equals(text, value, StringComparison.OrdinalIgnoreCase))
                    return letter;
            }
            return null;
        }

        private string BuildReviewItem(int number, DataRow r, string picked, string right, bool isRight)
        {
            string tagClass = picked == null ? "skip" : (isRight ? "ok" : "bad");
            string tagText = picked == null ? "Not answered" : (isRight ? "Correct" : "Incorrect");

            var sb = new StringBuilder();
            sb.Append("<article class=\"qz-rv\">");
            sb.AppendFormat("<div class=\"qz-rv-head\"><span class=\"qz-rv-q\">Question {0}</span><span class=\"qz-rv-tag {1}\">{2}</span></div>",
                number, tagClass, tagText);
            sb.AppendFormat("<h3 class=\"qz-rv-text\">{0}</h3>", Server.HtmlEncode(Convert.ToString(r["question_text"])));
            sb.Append("<ul class=\"qz-rv-opts\">");

            foreach (string letter in new[] { "A", "B", "C", "D" })
            {
                object o = r["option_" + letter.ToLowerInvariant()];
                string text = o == DBNull.Value ? "" : Convert.ToString(o);
                if (text.Length == 0) continue;

                bool isCorrectOpt = letter == right;
                bool isPicked = letter == picked;

                string cls = "qz-ro" + (isCorrectOpt ? " right" : "") + (isPicked && !isCorrectOpt ? " wrong" : "");
                string label = isCorrectOpt && isPicked ? "Your answer - correct"
                             : isCorrectOpt ? "Correct answer"
                             : isPicked ? "Your answer"
                             : "";

                sb.AppendFormat("<li class=\"{0}\"><span class=\"qz-key\">{1}</span><span class=\"qz-rt\">{2}</span>{3}</li>",
                    cls, letter, Server.HtmlEncode(text),
                    label.Length > 0 ? "<span class=\"qz-rl\">" + label + "</span>" : "");
            }
            sb.Append("</ul>");

            string explanation = r["explanation"] == DBNull.Value ? "" : Convert.ToString(r["explanation"]).Trim();
            if (explanation.Length > 0)
            {
                sb.AppendFormat("<div class=\"qz-expl\"><strong>Explanation</strong><p>{0}</p></div>",
                    Server.HtmlEncode(explanation));
            }

            sb.Append("</article>");
            return sb.ToString();
        }

        private static string ToGrade(int percent)
        {
            if (percent >= 90) return "A";
            if (percent >= 80) return "B";
            if (percent >= 70) return "C";
            if (percent >= 60) return "D";
            return "F";
        }

        private void ShowError(string message)
        {
            litError.Text = Server.HtmlEncode(message);
            pnlQuiz.Visible = false;
            pnlResult.Visible = false;
            pnlError.Visible = true;
        }
    }
}