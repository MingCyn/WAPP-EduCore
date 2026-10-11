using System;
using System.Configuration;
using System.Web.UI;
using Npgsql;

namespace WAPP_EduCore
{
    public partial class testconnection : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Read connection string from Web.config
            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"]?.ConnectionString;

            if (string.IsNullOrEmpty(connString))
            {
                lblStatus.Text = "❌ Error: 'SupabasePostgres' connection string is missing or empty in Web.config!";
                lblStatus.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68); // Red
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    lblStatus.Text = "✅ Success! Successfully connected to Supabase PostgreSQL database.";
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129); // Green
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "❌ Connection Failed: " + ex.Message;
                lblStatus.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68); // Red
            }
        }
    }
}