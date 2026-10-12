<%@ Page Language="C#" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="Npgsql" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <meta charset="utf-8" />
    <title>Database Connection Test</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background-color: #f4f7f6; color: #333; }
        .card { background: #ffffff; padding: 25px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); max-width: 600px; margin: auto; }
        h2 { margin-top: 0; color: #1a1a1a; }
        .success { color: #2e7d32; background: #e8f5e9; padding: 12px; border-radius: 4px; }
        .error { color: #c62828; background: #ffebee; padding: 12px; border-radius: 4px; word-break: break-all; }
        .info { margin-top: 15px; font-size: 14px; color: #555; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="card">
            <h2>Supabase PostgreSQL Test</h2>
            <hr style="border: 0; border-top: 1px solid #eee; margin: 15px 0;" />
            
            <% 
                string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"]?.ConnectionString;

                if (string.IsNullOrEmpty(connString))
                {
                    Response.Write("<div class='error'><strong>Error:</strong> Connection string named 'SupabasePostgres' was not found in your Web.config file.</div>");
                }
                else
                {
                    try
                    {
                        using (var conn = new NpgsqlConnection(connString))
                        {
                            conn.Open();
                            Response.Write("<div class='success'><strong>Success!</strong> Connection to Supabase PostgreSQL established successfully.</div>");

                            using (var cmd = new NpgsqlCommand("SELECT version();", conn))
                            {
                                var version = cmd.ExecuteScalar();
                                Response.Write("<div class='info'><strong>Server Version:</strong><br/>" + System.Web.HttpUtility.HtmlEncode(version?.ToString()) + "</div>");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Response.Write("<div class='error'><strong>Connection Failed:</strong><br/>" + System.Web.HttpUtility.HtmlEncode(ex.Message) + "</div>");
                    }
                }
            %>
        </div>
    </form>
</body>
</html>