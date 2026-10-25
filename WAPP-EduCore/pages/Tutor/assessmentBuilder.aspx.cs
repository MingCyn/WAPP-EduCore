using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Npgsql;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class assessmentBuilder : System.Web.UI.Page
    {
        private const string ConnectionName = "SupabasePostgres";
        private int courseId;
        private int lessonId;

        protected string QuizTitleValue = "";
        protected string QuizPassValue = "50";
        protected List<QuestionDto> ExistingQuestions = new List<QuestionDto>();

        public class QuestionDto
        {
            public int QuestionId { get; set; }
            public string Prompt { get; set; }
            public string Difficulty { get; set; }
            public int Points { get; set; }
            public string OptA { get; set; }
            public string OptB { get; set; }
            public string OptC { get; set; }
            public string OptD { get; set; }
            public string CorrectAnswer { get; set; }
            public string Explanation { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserRole"] == null ||
                !Session["UserRole"].ToString().Equals("Tutor", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("../../auth/login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            // Always parse query parameters on every request (crucial for PostBacks)
            int.TryParse(Request.QueryString["courseId"], out courseId);
            int.TryParse(Request.QueryString["lessonId"], out lessonId);

            LnkBackToCourse.HRef = courseId > 0
                ? "courseBuilder.aspx?courseId=" + courseId + (lessonId > 0 ? "&lessonId=" + lessonId : "")
                : "courses.aspx";

            // Error handling: Check if accessed directly without course/lesson context
            if (courseId <= 0 || lessonId <= 0 || !LessonBelongsToCourse())
            {
                // Hide the builder content and show the warning message view
                PnlBuilderContent.Visible = false;
                PnlBuilderError.Visible = true;
                return;
            }

            // Context is valid, show the builder interface
            PnlBuilderContent.Visible = true;
            PnlBuilderError.Visible = false;

            if (!IsPostBack)
            {
                LoadExistingAssessment();
            }
        }

        // Button event to handle redirecting back to courses list
        protected void BtnGoToCourses_Click(object sender, EventArgs e)
        {
            Response.Redirect("courses.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private bool LessonBelongsToCourse()
        {
            string connectionString = ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString;
            using (var conn = new NpgsqlConnection(connectionString))
            using (var cmd = new NpgsqlCommand("SELECT 1 FROM public.lesson WHERE lesson_id = @lid AND course_id = @cid", conn))
            {
                cmd.Parameters.AddWithValue("lid", lessonId);
                cmd.Parameters.AddWithValue("cid", courseId);
                conn.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        private void LoadExistingAssessment()
        {
            string connectionString = ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString;
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                long quizId = GetLessonQuizId(conn, null);
                if (quizId <= 0) return;

                // 1. Load Quiz details (Title, Passing Score)
                using (var cmd = new NpgsqlCommand("SELECT quiz_title, passing_score FROM quiz WHERE quiz_id = @quizId", conn))
                {
                    cmd.Parameters.AddWithValue("quizId", quizId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            QuizTitleValue = r["quiz_title"] != DBNull.Value ? r["quiz_title"].ToString() : "";
                            QuizPassValue = r["passing_score"] != DBNull.Value ? r["passing_score"].ToString() : "50";
                        }
                    }
                }

                // 2. Load Quiz Questions
                using (var cmd = new NpgsqlCommand("SELECT question_id, question_text, difficulty, points, option_a, option_b, option_c, option_d, correct_answer, explanation FROM quiz_questions WHERE quiz_id = @quizId ORDER BY question_id", conn))
                {
                    cmd.Parameters.AddWithValue("quizId", quizId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            ExistingQuestions.Add(new QuestionDto
                            {
                                QuestionId = Convert.ToInt32(r["question_id"]),
                                Prompt = r["question_text"] != DBNull.Value ? r["question_text"].ToString() : "",
                                Difficulty = r["difficulty"] != DBNull.Value ? r["difficulty"].ToString() : "Medium",
                                Points = r["points"] != DBNull.Value ? Convert.ToInt32(r["points"]) : 10,
                                OptA = r["option_a"] != DBNull.Value ? r["option_a"].ToString() : "",
                                OptB = r["option_b"] != DBNull.Value ? r["option_b"].ToString() : "",
                                OptC = r["option_c"] != DBNull.Value ? r["option_c"].ToString() : "",
                                OptD = r["option_d"] != DBNull.Value ? r["option_d"].ToString() : "",
                                CorrectAnswer = r["correct_answer"] != DBNull.Value ? r["correct_answer"].ToString() : "A",
                                Explanation = r["explanation"] != DBNull.Value ? r["explanation"].ToString() : ""
                            });
                        }
                    }
                }
            }
        }

        protected void BtnPublishAssessment_Click(object sender, EventArgs e)
        {
            SaveAssessment(publish: true);
        }

        private void ShowStatus(string message, bool isError = true)
        {
            LblStatus.Text = Server.HtmlEncode(message);
            LblStatus.Visible = true;

            if (isError)
            {
                // Inline error styling matching your UI theme
                LblStatus.Style["background-color"] = "#fdf2f2";
                LblStatus.Style["color"] = "#991b1b";
                LblStatus.Style["border"] = "1px solid #fecaca";
            }
            else
            {
                // Success styling
                LblStatus.Style["background-color"] = "#f0fdf4";
                LblStatus.Style["color"] = "#166534";
                LblStatus.Style["border"] = "1px solid #bbf7d0";
            }
        }

        private void SaveAssessment(bool publish)
        {
            if (lessonId <= 0 || !LessonBelongsToCourse())
            {
                ShowStatus("Open the assessment builder from a lesson in Course Builder.", true);
                return;
            }

            var form = Request.Unvalidated.Form;
            string title = (form["assessTitle"] ?? "").Trim();

            // 1. Validation: Ensure Assessment Title is provided before saving/publishing
            if (string.IsNullOrEmpty(title))
            {
                ShowStatus("Please enter an Assessment Title before saving.", true);
                return;
            }

            // 2. Validation: Ensure at least one question prompt is submitted
            string[] prompts = form.GetValues("prompt") ?? new string[0];
            int validQuestionCount = 0;
            foreach (var p in prompts)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    validQuestionCount++;
                }
            }

            if (validQuestionCount == 0)
            {
                ShowStatus("Cannot save an assessment without at least one question.", true);
                return;
            }

            int passingScore;
            string passText = (form["assessPass"] ?? "50").Replace("%", "").Trim();
            if (!int.TryParse(passText, out passingScore)) passingScore = 50;
            passingScore = Math.Max(0, Math.Min(100, passingScore));

            string connectionString = ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString;
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    long quizId = GetLessonQuizId(conn, transaction);
                    if (quizId > 0)
                    {
                        Execute(conn, transaction,
                            "UPDATE quiz SET quiz_title = @title, passing_score = @pass WHERE quiz_id = @quiz",
                            new Dictionary<string, object>
                            {
                                { "title", title }, { "pass", passingScore }, { "quiz", quizId }
                            });
                        Execute(conn, transaction, "DELETE FROM quiz_questions WHERE quiz_id = @quiz",
                            new Dictionary<string, object> { { "quiz", quizId } });
                    }
                    else
                    {
                        // Clean insert: only insert what the quiz table actually needs (Title, Passing Score, Lesson ID)
                        quizId = ExecuteScalar(conn, transaction,
                            "INSERT INTO quiz (quiz_title, passing_score, lesson_id) VALUES (@title, @pass, @lesson) RETURNING quiz_id",
                            new Dictionary<string, object>
                            {
                                { "title", title },
                                { "pass", passingScore },
                                { "lesson", lessonId }
                            });

                        Execute(conn, transaction, "UPDATE public.lesson SET quiz_id = @quiz WHERE lesson_id = @lesson AND course_id = @course",
                            new Dictionary<string, object>
                            {
                                { "quiz", quizId },
                                { "lesson", lessonId },
                                { "course", courseId }
                            });
                    }

                    // Save questions submitted from form inputs
                    string[] difficulties = form.GetValues("difficulty") ?? new string[0];
                    string[] pointsArr = form.GetValues("points") ?? new string[0];
                    string[] optA = form.GetValues("optA") ?? new string[0];
                    string[] optB = form.GetValues("optB") ?? new string[0];
                    string[] optC = form.GetValues("optC") ?? new string[0];
                    string[] optD = form.GetValues("optD") ?? new string[0];
                    string[] explanations = form.GetValues("explanation") ?? new string[0];
                    string[] qids = form.GetValues("qid") ?? new string[0];

                    for (int i = 0; i < prompts.Length; i++)
                    {
                        string promptText = (prompts[i] ?? "").Trim();
                        if (promptText.Length == 0) continue;

                        int pts = 10;
                        if (i < pointsArr.Length) int.TryParse(pointsArr[i], out pts);

                        string qIdentifier = i < qids.Length ? qids[i] : (i + 1).ToString();
                        string correctOpt = form["correct_" + qIdentifier] ?? "A";

                        Execute(conn, transaction,
                            "INSERT INTO quiz_questions (quiz_id, question_text, difficulty, points, option_a, option_b, option_c, option_d, correct_answer, explanation) " +
                            "VALUES (@quiz, @text, @diff, @pts, @a, @b, @c, @d, @correct, @explanation)",
                            new Dictionary<string, object>
                            {
                                { "quiz", quizId },
                                { "text", promptText },
                                { "diff", i < difficulties.Length ? difficulties[i] : "Medium" },
                                { "pts", pts },
                                { "a", i < optA.Length && !string.IsNullOrWhiteSpace(optA[i]) ? (object)optA[i] : DBNull.Value },
                                { "b", i < optB.Length && !string.IsNullOrWhiteSpace(optB[i]) ? (object)optB[i] : DBNull.Value },
                                { "c", i < optC.Length && !string.IsNullOrWhiteSpace(optC[i]) ? (object)optC[i] : DBNull.Value },
                                { "d", i < optD.Length && !string.IsNullOrWhiteSpace(optD[i]) ? (object)optD[i] : DBNull.Value },
                                { "correct", correctOpt },
                                { "explanation", i < explanations.Length && !string.IsNullOrWhiteSpace(explanations[i]) ? (object)explanations[i] : DBNull.Value }
                            });
                    }

                    transaction.Commit();
                }
            }

            ShowStatus("Assessment saved successfully.", false);
        }

        private long GetLessonQuizId(NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            using (var cmd = new NpgsqlCommand("SELECT quiz_id FROM public.lesson WHERE lesson_id = @lesson AND course_id = @course", conn, transaction))
            {
                cmd.Parameters.AddWithValue("lesson", lessonId);
                cmd.Parameters.AddWithValue("course", courseId);
                object value = cmd.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt64(value);
            }
        }

        private static int Execute(NpgsqlConnection conn, NpgsqlTransaction transaction, string sql, Dictionary<string, object> values)
        {
            using (var cmd = new NpgsqlCommand(sql, conn, transaction))
            {
                foreach (KeyValuePair<string, object> value in values)
                    cmd.Parameters.AddWithValue(value.Key, value.Value ?? DBNull.Value);
                return cmd.ExecuteNonQuery();
            }
        }

        private static long ExecuteScalar(NpgsqlConnection conn, NpgsqlTransaction transaction, string sql, Dictionary<string, object> values)
        {
            using (var cmd = new NpgsqlCommand(sql, conn, transaction))
            {
                foreach (KeyValuePair<string, object> value in values)
                    cmd.Parameters.AddWithValue(value.Key, value.Value ?? DBNull.Value);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        protected void BtnSaveInteractive_Click(object sender, EventArgs e)
        {
            ShowStatus("Interactive object configuration saved.", false);
        }

        protected void BtnSaveLab_Click(object sender, EventArgs e)
        {
            ShowStatus("Virtual lab configuration saved.", false);
        }
    }
}