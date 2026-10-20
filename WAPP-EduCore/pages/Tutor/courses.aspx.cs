using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using Npgsql;

namespace WAPP_EduCore.pages.Tutor
{
    public partial class courses : Page
    {
        protected Literal LitCount;
        protected Label LblError;
        protected Panel PnlEmpty;
        protected Repeater RptCourses;

        private const string ConnectionName = "SupabasePostgres";
        private const string SqlCourses =
            "SELECT c.course_id, c.course_title, c.topic, c.duration, c.created_at, " +
            "(SELECT COUNT(*) FROM lessons l WHERE l.course_id = c.course_id) AS lesson_count " +
            "FROM courses c ORDER BY c.created_at DESC, c.course_id DESC";

        public class CourseRow
        {
            public int CourseId { get; set; }
            public string Title { get; set; }
            public string Topic { get; set; }
            public string Duration { get; set; }
            public string LessonsText { get; set; }
            public string CreatedText { get; set; }
            public string SearchText { get; set; }
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

            if (IsPostBack) return;

            try
            {
                List<CourseRow> rows = LoadCourses();
                RptCourses.DataSource = rows;
                RptCourses.DataBind();
                PnlEmpty.Visible = rows.Count == 0;
                LitCount.Text = rows.Count == 1 ? "1 course" : rows.Count + " courses";
            }
            catch (NpgsqlException)
            {
                LblError.Text = "We couldn't load your courses right now. Please try again.";
                LblError.Visible = true;
                PnlEmpty.Visible = false;
                LitCount.Text = "";
            }
        }

        private List<CourseRow> LoadCourses()
        {
            var rows = new List<CourseRow>();
            var configuredConnection = ConfigurationManager.ConnectionStrings[ConnectionName];
            if (configuredConnection == null || string.IsNullOrWhiteSpace(configuredConnection.ConnectionString))
                throw new ConfigurationErrorsException("The SupabasePostgres connection string is missing from Web.config.");

            var connectionString = configuredConnection.ConnectionString;

            using (var connection = new NpgsqlConnection(connectionString))
            using (var command = new NpgsqlCommand(SqlCourses, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string title = reader.IsDBNull(1) ? "(Untitled course)" : Convert.ToString(reader[1]);
                        string topic = reader.IsDBNull(2) ? "" : Convert.ToString(reader[2]);
                        string duration = reader.IsDBNull(3) ? "" : FormatDuration(Convert.ToString(reader[3]));
                        int lessons = Convert.ToInt32(reader[5]);

                        rows.Add(new CourseRow
                        {
                            CourseId = Convert.ToInt32(reader[0]),
                            Title = title,
                            Topic = topic,
                            Duration = duration,
                            LessonsText = lessons == 1 ? "1 lesson" : lessons + " lessons",
                            CreatedText = reader.IsDBNull(4) ? "" :
                                "Created " + reader.GetDateTime(4).ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture),
                            SearchText = (title + " " + topic).ToLowerInvariant()
                        });
                    }
                }
            }

            return rows;
        }

        private static string FormatDuration(string value)
        {
            int weeks;
            value = (value ?? "").Trim();
            return int.TryParse(value, out weeks)
                ? (weeks == 1 ? "1 week" : weeks + " weeks")
                : value;
        }
    }
}
