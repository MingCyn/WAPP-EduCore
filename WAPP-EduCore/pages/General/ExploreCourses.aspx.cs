using Npgsql;
using System;
using System.Configuration;
using System.Data;
using System.Web.UI;

namespace WAPP_EduCore
{
    public partial class ExploreCourses : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string categoryFilter = Request.QueryString["cat"];
                BindAllCourses(categoryFilter);
            }
        }

        private void BindAllCourses(string category = null)
        {
            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;

            string query = @"SELECT c.course_id, c.title, c.category, c.duration, c.course_img, u.full_name AS tutor_name 
                             FROM courses c 
                             LEFT JOIN users u ON c.user_id = u.user_id";

            if (!string.IsNullOrEmpty(category))
            {
                query += " WHERE c.category = @category";
            }

            using (NpgsqlConnection conn = new NpgsqlConnection(connString))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(category))
                    {
                        cmd.Parameters.AddWithValue("@category", category);
                    }

                    using (NpgsqlDataAdapter sda = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        RepeaterAllCourses.DataSource = dt;
                        RepeaterAllCourses.DataBind();
                    }
                }
            }
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Simple search filtering by title
            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;
            string query = @"SELECT c.course_id, c.title, c.category, c.duration, c.course_img, u.username AS tutor_name 
                             FROM courses c 
                             LEFT JOIN users u ON c.user_id = u.user_id 
                             WHERE c.title ILIKE @search";

            using (NpgsqlConnection conn = new NpgsqlConnection(connString))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@search", "%" + txtSearch.Text.Trim() + "%");

                    using (NpgsqlDataAdapter sda = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        RepeaterAllCourses.DataSource = dt;
                        RepeaterAllCourses.DataBind();
                    }
                }
            }
        }
    }
}