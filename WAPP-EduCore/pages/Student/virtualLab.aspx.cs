using System;
using System.Web.UI.WebControls;
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
                var service = new VirtualLabService();
                string expName = ddlPhysicsExp.SelectedValue;
                string payloadJson = $"{{\"velocity\": \"{txtPhysVelocity.Text}\", \"angle\": \"{txtPhysAngle.Text}\"}}";

                string rawApiResponse = await service.EvaluateExperimentAsync("Physics", expName, payloadJson);
                var parsedObj = ParseAiJsonResponse(rawApiResponse);

                lblPhysHeight.Text = parsedObj["primaryMetric"]?.ToString() ?? "0.00 m";
                lblPhysDistance.Text = parsedObj["secondaryMetric"]?.ToString() ?? "0.00 m";
                litPhysicsAi.Text = "<strong>AI Insight:</strong> " + parsedObj["explanation"]?.ToString();

                TriggerGraphRender(parsedObj["coordinates"], "Horizontal Distance (m)", "Vertical Height (m)");
            }
            catch (Exception ex)
            {
                litPhysicsAi.Text = "<span style='color: red;'>Simulation error: " + ex.Message + "</span>";
            }
        }

        protected async void btnRunChemistry_Click(object sender, EventArgs e)
        {
            try
            {
                var service = new VirtualLabService();
                string expName = ddlChemistryExp.SelectedValue;
                string substance = ddlChemSubstance.SelectedValue;
                string payloadJson = $"{{\"substance\": \"{substance}\", \"amount\": \"{txtChemAmount.Text}\"}}";

                string rawApiResponse = await service.EvaluateExperimentAsync("Chemistry", expName, payloadJson);
                var parsedObj = ParseAiJsonResponse(rawApiResponse);

                lblChemOutput.Text = parsedObj["primaryMetric"]?.ToString() ?? "Completed";
                litChemAi.Text = "<strong>AI Insight:</strong> " + parsedObj["explanation"]?.ToString();

                TriggerGraphRender(parsedObj["coordinates"], "Volume / Time Added", "pH Value / Volume of Gas (cm³)");
            }
            catch (Exception ex)
            {
                litChemAi.Text = "<span style='color: red;'>Simulation error: " + ex.Message + "</span>";
            }
        }

        protected async void btnRunBiology_Click(object sender, EventArgs e)
        {
            try
            {
                var service = new VirtualLabService();
                string expName = ddlBiologyExp.SelectedValue;
                string payloadJson = $"{{\"parameterValue\": \"{txtBioParam.Text}\"}}";

                string rawApiResponse = await service.EvaluateExperimentAsync("Biology", expName, payloadJson);
                var parsedObj = ParseAiJsonResponse(rawApiResponse);

                lblBioRate.Text = parsedObj["primaryMetric"]?.ToString() ?? "Optimum";
                litBioAi.Text = "<strong>AI Insight:</strong> " + parsedObj["explanation"]?.ToString();

                TriggerGraphRender(parsedObj["coordinates"], "Independent Variable (Temperature / Concentration)", "Reaction Rate / Activity (%)");
            }
            catch (Exception ex)
            {
                litBioAi.Text = "<span style='color: red;'>Simulation error: " + ex.Message + "</span>";
            }
        }

        private Newtonsoft.Json.Linq.JObject ParseAiJsonResponse(string rawApiResponse)
        {
            var outerObj = Newtonsoft.Json.Linq.JObject.Parse(rawApiResponse);
            string rawText = outerObj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
            string cleanJson = rawText.Replace("```json", "").Replace("```", "").Trim();
            return Newtonsoft.Json.Linq.JObject.Parse(cleanJson);
        }

        private void TriggerGraphRender(Newtonsoft.Json.Linq.JToken coordinates, string xLabel, string yLabel)
        {
            if (coordinates != null)
            {
                string jsCoords = coordinates.ToString(Newtonsoft.Json.Formatting.None);
                string script = $"drawSimulationGraph({jsCoords}, '{xLabel}', '{yLabel}');";
                ClientScript.RegisterStartupScript(this.GetType(), "DrawGraph", script, true);
            }
        }
    }
}