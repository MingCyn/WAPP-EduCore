using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WAPPEduCore.Services
{
    public class VirtualLabService
    {
        private readonly string _apiKey;

        public VirtualLabService()
        {
            _apiKey = ConfigurationManager.AppSettings["VirtualLabApiKey"];

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("VirtualLabApiKey is missing from Web.config.");
            }
        }

        public async Task<string> EvaluateExperimentAsync(string subject, string experimentName, string parametersJson)
        {
            using (var client = new HttpClient())
            {
                var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash:generateContent?key=" + _apiKey;

                // Using standard string concatenation to avoid any interpolation brace clashes
                string systemInstruction = "You are an expert AI science curriculum engine for EduCore aligned with Malaysian SPM and Cambridge IGCSE guidelines. " +
                           "Subject: " + subject + ", Experiment: " + experimentName + ". " +
                           "You MUST return ONLY a valid raw JSON object with accurate graph-plotting coordinates for this specific subject. " +
                           "Structure the JSON precisely like this:\n" +
                           "{\n" +
                           "  \"primaryMetric\": \"[calculated result with unit]\",\n" +
                           "  \"secondaryMetric\": \"[secondary calculated result with unit]\",\n" +
                           "  \"explanation\": \"[1-2 sentences of educational feedback based on user parameters]\",\n" +
                           "  \"coordinates\": [\n" +
                           "    {\"x\": 0, \"y\": 0},\n" +
                           "    {\"x\": 5, \"y\": 3.5},\n" +
                           "    {\"x\": 10, \"y\": 7.0}\n" +
                           "  ]\n" +
                           "}";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = systemInstruction + "\n\nUser Input Parameters: " + parametersJson } }
                        }
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    throw new Exception("API Error (" + (int)response.StatusCode + "): " + errorDetails);
                }

                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}