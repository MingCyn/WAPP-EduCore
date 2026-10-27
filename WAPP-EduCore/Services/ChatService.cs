using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WAPP_EduCore;
using WAPP_EduCore.Models;

namespace WAPP_EduCore.Services
{
    public static class ChatService
    {
        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(25)
        };

        private static string _cachedKey;

        static ChatService()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        // Key lookup: local secrets.config / host settings first, then the shared database.
        private static string GetApiKey()
        {
            if (!string.IsNullOrEmpty(_cachedKey)) return _cachedKey;

            // 1) local override (secrets.config or server app settings)
            string key = ConfigurationManager.AppSettings["GeminiApiKey"];

            // 2) otherwise load it from the shared database
            if (string.IsNullOrWhiteSpace(key))
            {
                try
                {
                    DataTable dt = Db.Query(
                        "SELECT setting_value FROM app_settings WHERE setting_key = @k",
                        new Dictionary<string, object> { { "@k", "GeminiApiKey" } });
                    if (dt.Rows.Count > 0) key = Convert.ToString(dt.Rows[0]["setting_value"]);
                }
                catch { }
            }

            _cachedKey = (key ?? "").Trim();
            return _cachedKey;
        }

        public static async Task<string> AskAsync(string systemPrompt, IList<ChatTurn> history, string userMessage)
        {
            string key = GetApiKey();
            string model = ConfigurationManager.AppSettings["GeminiModel"];

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(model))
                return "The assistant is not configured yet. Please contact the administrator.";

            var contents = new JArray();
            foreach (var t in history)
                contents.Add(MakeTurn(t.Role, t.Text));
            contents.Add(MakeTurn("user", userMessage));

            var body = new JObject
            {
                ["system_instruction"] = new JObject
                {
                    ["parts"] = new JArray(new JObject { ["text"] = systemPrompt })
                },
                ["contents"] = contents
            };

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post,
                    "https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent");
                req.Headers.Add("x-goog-api-key", key);
                req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

                var res = await Http.SendAsync(req);
                string json = await res.Content.ReadAsStringAsync();

                if (res.StatusCode == (HttpStatusCode)429)
                    return "I'm getting too many requests right now. Please try again in a minute.";
                if (!res.IsSuccessStatusCode)
                    return "Error " + (int)res.StatusCode + ": " + json; //debug chatbot

                return (string)JObject.Parse(json)["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]
                       ?? "Sorry, I couldn't come up with an answer. Try rephrasing your question.";
            }
            catch (TaskCanceledException)
            {
                return "The assistant took too long to respond. Please try again.";
            }
            catch (Exception)
            {
                return "Sorry, something went wrong. Please try again.";
            }
        }

        private static JObject MakeTurn(string role, string text)
        {
            return new JObject
            {
                ["role"] = role,
                ["parts"] = new JArray(new JObject { ["text"] = text })
            };
        }
    }
}