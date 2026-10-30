<%@ Page Language="C#" %>
<%@ Import Namespace="System.Net.Http" %>
<%@ Import Namespace="System.Threading.Tasks" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Web.Script.Serialization" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Gemini Connection Test</title>
    <style>
        body { font-family: Arial; padding: 20px; color: #333; }
        .box { padding: 15px; border-radius: 6px; margin-top: 15px; background: #f1f5f9; white-space: pre-wrap; }
        .success { color: #15803d; font-weight: bold; }
        .error { color: #b91c1c; font-weight: bold; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <h2>🔌 Gemini API Connection Tester</h2>
        <asp:Button ID="btnTest" runat="server" Text="Run Connection Test" OnClick="btnTest_Click" CssClass="btn-solid" />
        <div runat="server" id="resultBox" class="box" visible="false">
            <asp:Literal ID="litResult" runat="server"></asp:Literal>
        </div>
    </form>
</body>
</html>

<script runat="server">
    private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

protected void btnTest_Click(object sender, EventArgs e)    {
        resultBox.Visible = true;
        string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
        string model = ConfigurationManager.AppSettings["GeminiModel"] ?? "gemini-1.5-flash";

        if (string.IsNullOrEmpty(apiKey))
        {
            litResult.Text = "<span class='error'>❌ Error: GeminiApiKey is missing in Web.config or secrets.config.</span>";
            return;
        }

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var payload = new Dictionary<string, object>
        {
            { "contents", new object[] {
                new Dictionary<string, object> {
                    { "parts", new object[] {
                        new Dictionary<string, object> { { "text", "Say 'Connection successful!' if you can read this." } }
                    } }
                }
            } }
        };

        try
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
            var jsonPayload = new JavaScriptSerializer().Serialize(payload);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
var response = client.PostAsync(url, content).Result;
string responseString = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                litResult.Text = "<span class='success'>✅ Connection Successful!</span>\n\nAPI Response:\n" + Server.HtmlEncode(responseString);
            }
            else
            {
                litResult.Text = $"<span class='error'>❌ API Error (HTTP {(int)response.StatusCode}):</span>\n" + Server.HtmlEncode(responseString);
            }
        }
        catch (Exception ex)
        {
            litResult.Text = "<span class='error'>❌ Exception Occurred:</span>\n" + Server.HtmlEncode(ex.Message);
        }
    }
</script>