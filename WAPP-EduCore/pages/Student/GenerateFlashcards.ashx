<%@ WebHandler Language="C#" Class="WAPP_EduCore.pages.Student.GenerateFlashcards" %>

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace WAPP_EduCore.pages.Student
{
    public class GenerateFlashcards : IHttpHandler, System.Web.SessionState.IRequiresSessionState
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public void ProcessRequest(HttpContext context)
        {
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
            
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            try
            {
                if (context.Request.HttpMethod != "POST")
                {
                    context.Response.Write(Json.Serialize(new { success = false, message = "Method not allowed." }));
                    return;
                }

                long userId = 0;
                if (context.Session["UserID"] != null) long.TryParse(Convert.ToString(context.Session["UserID"]), out userId);
                if (userId == 0)
                {
                    context.Response.Write(Json.Serialize(new { success = false, message = "Unauthorized access." }));
                    return;
                }

                string body;
                using (var reader = new StreamReader(context.Request.InputStream)) body = reader.ReadToEnd();
                var data = Json.Deserialize<Dictionary<string, object>>(body);

                long courseId = 0;
                if (data != null && data.ContainsKey("courseId")) long.TryParse(Convert.ToString(data["courseId"]), out courseId);

                if (courseId == 0)
                {
                    context.Response.Write(Json.Serialize(new { success = false, message = "Invalid course notebook." }));
                    return;
                }

                // Fetch ALL saved notes belonging to this course notebook for the user
                DataTable notes = Db.Query(
                    "SELECT n.highlighted_text, n.ai_summary, l.lesson_title " +
                    "FROM student_notes n " +
                    "JOIN course_notebooks nb ON nb.notebook_id = n.notebook_id " +
                    "JOIN lesson l ON l.lesson_id = n.lesson_id " +
                    "WHERE nb.user_id = @uid AND nb.course_id = @cid ORDER BY n.created_at DESC",
                    new Dictionary<string, object> { { "@uid", userId }, { "@cid", courseId } });

                if (notes == null || notes.Rows.Count == 0)
                {
                    context.Response.Write(Json.Serialize(new { success = false, message = "No saved notes found in this notebook to generate flashcards from." }));
                    return;
                }

                // Compile all highlights and notes into a unified context block
                var sb = new StringBuilder();
                int index = 1;
                foreach (DataRow row in notes.Rows)
                {
                    sb.AppendLine($"### Note {index} (Lesson: {row["lesson_title"]})");
                    sb.AppendLine($"Highlighted Passage: \"{row["highlighted_text"]}\"");
                    sb.AppendLine($"Summary/Explanation: {row["ai_summary"]}");
                    sb.AppendLine();
                    index++;
                }

                string systemPrompt = 
                    "You are an expert AI Study Assistant. Based strictly on the student's saved course notes and highlights provided below, generate a high-yield study flashcard deck.\n\n" +
                    "Rules:\n" +
                    "1. Create clear, concise flashcards covering the key concepts across all the notes.\n" +
                    "2. Format each flashcard strictly using:\n" +
                    "   ### Card X\n" +
                    "   **Question:** [Question testing the concept]\n" +
                    "   **Answer:** [Precise answer or definition]\n" +
                    "3. Rely entirely on the provided student notebook content.";

                string userPrompt = "STUDENT'S NOTEBOOK HIGHLIGHTS:\n\n" + sb.ToString();

                string aiResponse = Task.Run(() => CallGeminiAsync(systemPrompt, userPrompt)).GetAwaiter().GetResult();

                context.Response.Write(Json.Serialize(new { success = true, flashcards = aiResponse }));
            }
            catch (Exception ex)
            {
                context.Response.Write(Json.Serialize(new { success = false, message = "Error: " + ex.Message }));
            }
        }

        private static async Task<string> CallGeminiAsync(string systemText, string userText)
        {
            string key = ConfigurationManager.AppSettings["GeminiApiKey"];
            string model = ConfigurationManager.AppSettings["GeminiModel"] ?? "gemini-1.5-flash";
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={key}";

            var payload = new Dictionary<string, object>
            {
                { "systemInstruction", new Dictionary<string, object> { { "parts", new object[] { new Dictionary<string, object> { { "text", systemText } } } } } },
                { "contents", new object[] { new Dictionary<string, object> { { "role", "user" }, { "parts", new object[] { new Dictionary<string, object> { { "text", userText } } } } } } },
                { "generationConfig", new Dictionary<string, object> { { "temperature", 0.3 }, { "maxOutputTokens", 4096 } } }
            };

            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                req.Content = new StringContent(Json.Serialize(payload), Encoding.UTF8, "application/json");
                using (var resp = await Http.SendAsync(req).ConfigureAwait(false))
                {
                    string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) return "Failed to generate flashcards from AI service.";
                    
                    var root = Json.Deserialize<Dictionary<string, object>>(body);
                    var cands = root["candidates"] as System.Collections.IList;
                    var cand = cands[0] as Dictionary<string, object>;
                    var content = cand["content"] as Dictionary<string, object>;
                    var parts = content["parts"] as System.Collections.IList;
                    var pd = parts[0] as Dictionary<string, object>;
                    return Convert.ToString(pd["text"]).Trim();
                }
            }
        }

        public bool IsReusable => false;
    }
}