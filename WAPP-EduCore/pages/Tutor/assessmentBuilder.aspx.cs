using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Web.UI;
using Npgsql;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class assessmentBuilder : System.Web.UI.Page
    {
        private const string ConnectionName = "SupabasePostgres";
        private int courseId;
        private int lessonId;

        // One quiz question as posted from the Question Builder
        private class QuestionDto
        {
            public string prompt { get; set; }
            public string difficulty { get; set; }
            public int points { get; set; }
            public List<string> options { get; set; }
            public string correct { get; set; }       // "A".."D"
            public string explanation { get; set; }
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

            int.TryParse(Request.QueryString["courseId"], out courseId);
            int.TryParse(Request.QueryString["lessonId"], out lessonId);
            LnkBackToCourse.HRef = courseId > 0
                ? "courseBuilder.aspx?courseId=" + courseId + (lessonId > 0 ? "&lessonId=" + lessonId : "")
                : "courses.aspx";

            if (lessonId <= 0 || !LessonBelongsToCourse())
            {
                ShowStatus("Open the assessment builder from a lesson in Course Builder.");
                BtnPublishAssessment.Enabled = false;
                BtnSaveAssessmentDraft.Enabled = false;
            }
        }

        private bool LessonBelongsToCourse()
        {
            string connectionString = ConfigurationManager.ConnectionStrings[ConnectionName].ConnectionString;
            using (var conn = new NpgsqlConnection(connectionString))
            using (var cmd = new NpgsqlCommand("SELECT 1 FROM lessons WHERE lesson_id = @lid AND course_id = @cid", conn))
            {
                cmd.Parameters.AddWithValue("lid", lessonId);
                cmd.Parameters.AddWithValue("cid", courseId);
                conn.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        // ---------------------------------------------------------------
        // Assessment tab
        // ---------------------------------------------------------------
        protected void BtnPublishAssessment_Click(object sender, EventArgs e)
        {
            SaveAssessment(publish: true);
        }

        protected void BtnSaveAssessmentDraft_Click(object sender, EventArgs e)
        {
            SaveAssessment(publish: false);
        }

        private void SaveAssessment(bool publish)
        {
            if (lessonId <= 0 || !LessonBelongsToCourse())
            {
                ShowStatus("Open the assessment builder from a lesson in Course Builder.");
                return;
            }

            List<QuestionDto> questions = ReadQuestionsFromForm();
            if (questions.Count == 0)
            {
                ShowStatus("Add at least one question before saving the assessment.");
                return;
            }

            var form = Request.Unvalidated.Form;
            string title = (form["assessTitle"] ?? "Untitled Assessment").Trim();
            if (title.Length == 0) title = "Untitled Assessment";

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
                        quizId = ExecuteScalar(conn, transaction,
                            "INSERT INTO quiz (quiz_title, passing_score, lesson_id) VALUES (@title, @pass, @lesson) RETURNING quiz_id",
                            new Dictionary<string, object>
                            {
                                { "title", title }, { "pass", passingScore }, { "lesson", lessonId }
                            });

                        Execute(conn, transaction, "UPDATE lessons SET quiz_id = @quiz WHERE lesson_id = @lesson AND course_id = @course",
                            new Dictionary<string, object>
                            {
                                { "quiz", quizId }, { "lesson", lessonId }, { "course", courseId }
                            });
                    }

                    foreach (QuestionDto question in questions)
                    {
                        Execute(conn, transaction,
                            "INSERT INTO quiz_questions (quiz_id, question_text, option_a, option_b, option_c, option_d, correct_answer, explanation) " +
                            "VALUES (@quiz, @text, @a, @b, @c, @d, @correct, @explanation)",
                            new Dictionary<string, object>
                            {
                                { "quiz", quizId },
                                { "text", question.prompt },
                                { "a", GetOption(question.options, 0) },
                                { "b", GetOption(question.options, 1) },
                                { "c", GetOption(question.options, 2) },
                                { "d", GetOption(question.options, 3) },
                                { "correct", question.correct ?? "" },
                                { "explanation", string.IsNullOrWhiteSpace(question.explanation) ? (object)DBNull.Value : question.explanation }
                            });
                    }

                    transaction.Commit();
                }
            }

            ShowStatus(string.Format("{0} question(s) saved successfully.", questions.Count));
        }

        private long GetLessonQuizId(NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            using (var cmd = new NpgsqlCommand("SELECT quiz_id FROM lessons WHERE lesson_id = @lesson AND course_id = @course", conn, transaction))
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

        private static object GetOption(List<string> options, int index)
        {
            return options != null && index < options.Count && !string.IsNullOrWhiteSpace(options[index])
                ? (object)options[index]
                : DBNull.Value;
        }

        private List<QuestionDto> ReadQuestionsFromForm()
        {
            // Request.Unvalidated avoids the "potentially dangerous Request.Form value" error
            // when a tutor types characters such as < or > (e.g. "x < y").
            // Always HtmlEncode these values when displaying them back to students.
            var form = Request.Unvalidated.Form;

            string[] ids = form.GetValues("qid") ?? new string[0];
            string[] prompts = form.GetValues("prompt") ?? new string[0];
            string[] difficulties = form.GetValues("difficulty") ?? new string[0];
            string[] points = form.GetValues("points") ?? new string[0];
            string[] optA = form.GetValues("optA") ?? new string[0];
            string[] optB = form.GetValues("optB") ?? new string[0];
            string[] optC = form.GetValues("optC") ?? new string[0];
            string[] optD = form.GetValues("optD") ?? new string[0];
            string[] explanations = form.GetValues("explanation") ?? new string[0];

            var result = new List<QuestionDto>();
            for (int i = 0; i < ids.Length; i++)
            {
                string prompt = (GetValue(prompts, i) ?? "").Trim();
                if (prompt.Length == 0) continue;           // skip empty questions

                int pts;
                if (!int.TryParse(GetValue(points, i), out pts) || pts < 0) pts = 0;

                var options = new List<string>();
                foreach (string o in new[] { GetValue(optA, i), GetValue(optB, i), GetValue(optC, i), GetValue(optD, i) })
                    options.Add((o ?? "").Trim());

                result.Add(new QuestionDto
                {
                    prompt = prompt,
                    difficulty = GetValue(difficulties, i),
                    points = pts,
                    options = options,
                    correct = form["correct_" + GetValue(ids, i)],
                    explanation = (GetValue(explanations, i) ?? "").Trim()
                });
            }
            return result;
        }

        private static string GetValue(string[] values, int index)
        {
            return values != null && index < values.Length ? values[index] : "";
        }

        // ---------------------------------------------------------------
        // Interactive Object tab
        // ---------------------------------------------------------------
        protected void BtnSaveInteractive_Click(object sender, EventArgs e)
        {
            var form = Request.Unvalidated.Form;
            string title = form["ioTitle"];
            string language = form["ioLang"];
            string starterCode = form["ioStarter"];   // may contain < and >

            // TODO (Supabase): store as a block in lessons.lesson_content, e.g.
            // { "type": "interactive", "title": ..., "language": ..., "timeoutSeconds": ..., "starterCode": ..., "allowReset": ..., "published": ... }

            ShowStatus("Interactive object configuration ready to save.");
        }

        // ---------------------------------------------------------------
        // Virtual Lab tab
        // ---------------------------------------------------------------
        protected void BtnSaveLab_Click(object sender, EventArgs e)
        {
            var form = Request.Unvalidated.Form;
            string title = form["labTitle"];
            string environment = form["labEnv"];
            string instructions = form["labInstructions"];

            // TODO (Supabase): store as a block in lessons.lesson_content, e.g.
            // { "type": "lab", "title": ..., "environment": ..., "memory": ..., "sessionMins": ..., "instructions": ..., "published": ... }

            ShowStatus("Virtual lab configuration ready to save.");
        }

        private void ShowStatus(string message)
        {
            LblStatus.Text = Server.HtmlEncode(message);
            LblStatus.Visible = true;
        }
    }
}