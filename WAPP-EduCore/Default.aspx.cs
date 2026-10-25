using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WAPP_EduCore
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindCoursesFromSupabase();
            }
        }

private void BindCoursesFromSupabase()
        {
            string connString = ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;

            string query = @"SELECT c.course_id, c.title, c.category, c.duration, c.course_img, u.full_name AS tutor_name 
                 FROM courses c 
                 LEFT JOIN users u ON c.user_id = u.user_id 
                 WHERE TRIM(lower(c.status)) = 'published'
                 LIMIT 4";

            using (NpgsqlConnection conn = new NpgsqlConnection(connString))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataAdapter sda = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        // Normalise the course_img path for browser rendering
                        foreach (DataRow row in dt.Rows)
                        {
                            string img = row["course_img"] as string ?? "";
                            if (!string.IsNullOrEmpty(img))
                            {
                                if (img.StartsWith("~/"))
                                {
                                    // Convert ~/ to root-relative path (e.g. /uploads/...)
                                    img = VirtualPathUtility.ToAbsolute(img);
                                }
                                else if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !img.StartsWith("/"))
                                {
                                    // If it's just "uploads/courses/...", prepend a slash
                                    img = "/" + img.TrimStart('/');
                                }
                                row["course_img"] = img;
                            }
                        }

                        RepeaterCourses.DataSource = dt;
                        RepeaterCourses.DataBind();
                    }
                }
            }
        }
    }
}