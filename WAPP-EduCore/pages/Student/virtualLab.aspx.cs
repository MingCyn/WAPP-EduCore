using System;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WAPPEduCore.Services;

namespace WAPPEduCore
{
    public partial class virtualLab : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void SwitchTab_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            string tabName = clickedButton.CommandArgument;

            btnTabPhysics.CssClass = "tab-btn";
            btnTabChemistry.CssClass = "tab-btn";
            btnTabBiology.CssClass = "tab-btn";

            if (tabName == "Physics")
            {
                mvLabPanels.ActiveViewIndex = 0;
                btnTabPhysics.CssClass = "tab-btn active-tab";
            }
            else if (tabName == "Chemistry")
            {
                mvLabPanels.ActiveViewIndex = 1;
                btnTabChemistry.CssClass = "tab-btn active-tab";
            }
            else if (tabName == "Biology")
            {
                mvLabPanels.ActiveViewIndex = 2;
                btnTabBiology.CssClass = "tab-btn active-tab";
            }
        }

        protected async void btnRunPhysics_Click(object sender, EventArgs e)
        {
            try
            {
                double velocity, angle;
                if (!TryNumber(txtPhysVelocity.Text, out velocity) || !TryNumber(txtPhysAngle.Text, out angle))
                {
                    ShowError(litPhysicsAi, "Please enter numeric values for velocity and angle.");
                    return;
                }

                var service = new VirtualLabService();
                string expName = ddlPhysicsExp.SelectedValue;
                var payload = new JObject
                {
                    ["velocity"] = txtPhysVelocity.Text.Trim(),
                    ["angle"] = txtPhysAngle.Text.Trim()
                };

                string rawApiResponse = await service.EvaluateExperimentAsync("Physics", expName, payload.ToString(Formatting.None));
                var parsedObj = ParseAiJsonResponse(rawApiResponse);

                lblPhysHeight.Text = HttpUtility.HtmlEncode(parsedObj["primaryMetric"]?.ToString() ?? "0.00 m");
                lblPhysDistance.Text = HttpUtility.HtmlEncode(parsedObj["secondaryMetric"]?.ToString() ?? "0.00 m");
                litPhysicsAi.Text = "<strong>AI Insight:</strong> " + HttpUtility.HtmlEncode(parsedObj["explanation"]?.ToString());

                TriggerGraphRender(parsedObj["coordinates"], "Horizontal Distance (m)", "Vertical Height (m)", "canvasPhysics", litPhysicsAi);
            }
            catch (Exception ex)
            {
                ShowError(litPhysicsAi, "Simulation error: " + ex.Message);
            }
        }

        protected async void btnRunChemistry_Click(object sender, EventArgs e)
        {
            try
            {
                var service = new VirtualLabService();
                string expName = ddlChemistryExp.SelectedValue;
                var payload = new JObject
                {
                    ["substance"] = ddlChemSubstance.SelectedValue,
                    ["amount"] = txtChemAmount.Text.Trim()
                };

                string rawApiResponse = await service.EvaluateExperimentAsync("Chemistry", expName, payload.ToString(Formatting.None));
                var parsedObj = ParseAiJsonResponse(rawApiResponse);

                lblChemOutput.Text = HttpUtility.HtmlEncode(parsedObj["primaryMetric"]?.ToString() ?? "Completed");
                litChemAi.Text = "<strong>AI Insight:</strong> " + HttpUtility.HtmlEncode(parsedObj["explanation"]?.ToString());

                TriggerGraphRender(parsedObj["coordinates"], "Volume / Time Added", "pH Value / Volume of Gas (cm³)", "canvasChemistry", litChemAi);
            }
            catch (Exception ex)
            {
                ShowError(litChemAi, "Simulation error: " + ex.Message);
            }
        }

        protected async void btnRunBiology_Click(object sender, EventArgs e)
        {
            try
            {
                double paramValue;
                if (!TryNumber(txtBioParam.Text, out paramValue))
                {
                    ShowError(litBioAi, "Please enter a numeric value for the independent variable.");
                    return;
                }

                var service = new VirtualLabService();
                string expName = ddlBiologyExp.SelectedValue;
                var payload = new JObject
                {
                    ["parameterValue"] = txtBioParam.Text.Trim()
                };

                string rawApiResponse = await service.EvaluateExperimentAsync("Biology", expName, payload.ToString(Formatting.None));
                var parsedObj = ParseAiJsonResponse(rawApiResponse);

                lblBioRate.Text = HttpUtility.HtmlEncode(parsedObj["primaryMetric"]?.ToString() ?? "Optimum");
                litBioAi.Text = "<strong>AI Insight:</strong> " + HttpUtility.HtmlEncode(parsedObj["explanation"]?.ToString());

                TriggerGraphRender(parsedObj["coordinates"], "Independent Variable (Temperature / Concentration)", "Reaction Rate / Activity (%)", "canvasBiology", litBioAi);
            }
            catch (Exception ex)
            {
                ShowError(litBioAi, "Simulation error: " + ex.Message);
            }
        }

        // ---------- Helpers ----------

        private static bool TryNumber(string text, out double value)
        {
            return double.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static void ShowError(Literal target, string message)
        {
            target.Text = "<span style='color: red;'>" + HttpUtility.HtmlEncode(message) + "</span>";
        }

        private static string JsString(string value)
        {
            // Safe JS string literal (quotes, backslashes and < > & are escaped)
            return JsonConvert.SerializeObject(value ?? "", Formatting.None,
                new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
        }

        private JObject ParseAiJsonResponse(string rawApiResponse)
        {
            var outerObj = JObject.Parse(rawApiResponse);
            string rawText = outerObj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

            if (string.IsNullOrWhiteSpace(rawText))
                throw new InvalidOperationException("The AI service returned an empty response.");

            string cleanJson = rawText.Replace("```json", "").Replace("```", "").Trim();

            // Keep only the JSON object in case the model added extra text around it
            int start = cleanJson.IndexOf('{');
            int end = cleanJson.LastIndexOf('}');
            if (start < 0 || end <= start)
                throw new InvalidOperationException("The AI response was not in the expected format.");

            return JObject.Parse(cleanJson.Substring(start, end - start + 1));
        }

        private void TriggerGraphRender(JToken coordinates, string xLabel, string yLabel, string canvasId, Literal messageTarget)
        {
            // Rebuild the points as clean numbers so nothing unexpected reaches the page script
            var points = new JArray();
            var source = coordinates as JArray;
            if (source != null)
            {
                foreach (var item in source)
                {
                    double x, y;
                    if (TryNumber(item["x"]?.ToString(), out x) && TryNumber(item["y"]?.ToString(), out y))
                    {
                        points.Add(new JObject { ["x"] = x, ["y"] = y });
                    }
                }
            }

            if (points.Count == 0)
            {
                messageTarget.Text += "<br /><span style='color: #b45309;'>No graph data was returned for this run.</span>";
                return;
            }

            string script = string.Format("drawSimulationGraph({0}, {1}, {2}, {3});",
                points.ToString(Formatting.None), JsString(xLabel), JsString(yLabel), JsString(canvasId));

            ClientScript.RegisterStartupScript(this.GetType(), "DrawGraph_" + canvasId, script, true);
        }
    }
}