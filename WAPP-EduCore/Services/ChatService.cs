using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WAPP_EduCore.Models;

namespace WAPP_EduCore.Services
{
    public static class ChatService
    {
        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(25)
        };

        static ChatService()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public static async Task<string> AskAsync(string systemPrompt, IList<ChatTurn> history, string userMessage)
        {
            string key = ConfigurationManager.AppSettings["GeminiApiKey"];
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
                    return "Error " + (int)res.StatusCode + ": " + json;

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