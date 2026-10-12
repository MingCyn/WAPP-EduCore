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
    public partial class Default : Page{
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

            // Query joining courses and users to fetch the tutor's username
            string query = @"SELECT c.title, c.category, c.duration, c.course_img, u.full_name AS tutor_name 
                         FROM courses c 
                         LEFT JOIN users u ON c.user_id = u.user_id 
                         LIMIT 4";

            using (NpgsqlConnection conn = new NpgsqlConnection(connString))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataAdapter sda = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        // Bind the database rows to the Repeater control
                        RepeaterCourses.DataSource = dt;
                        RepeaterCourses.DataBind();
                    }
                }
            }
        }
    }
}