using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace WAPP_EduCore
{
    public static class Db
    {
        private static string ConnString
        {
            get
            {
                var s = ConfigurationManager.ConnectionStrings["SupabasePostgres"];
                if (s == null)
                    throw new ConfigurationErrorsException("Connection string 'SupabasePostgres' is missing from web.config.");
                return s.ConnectionString;
            }
        }

        public static DataTable Query(string sql, Dictionary<string, object> parameters = null)
        {
            DataTable table = new DataTable();

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnString))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        foreach (var kv in parameters)
                        {
                            cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);
                        }
                    }

                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd))
                    {
                        conn.Open();
                        adapter.Fill(table);
                    }
                }
            }

            return table;
        }
    }
}