using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;

namespace WAPP_EduCore
{
    public static class Db
    {
        // For SELECT queries: returns the rows as a DataTable
        public static DataTable Query(
            string sql,
            Dictionary<string, object> parameters = null)
        {
            DataTable table = new DataTable();

            using (NpgsqlConnection conn =
                WAPP_EduCore.Data.DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                if (parameters != null)
                {
                    foreach (var kv in parameters)
                    {
                        cmd.Parameters.AddWithValue(
                            kv.Key,
                            kv.Value ?? DBNull.Value);
                    }
                }

                using (NpgsqlDataAdapter adapter =
                    new NpgsqlDataAdapter(cmd))
                {
                    adapter.Fill(table);
                }
            }

            return table;
        }

        // For INSERT / UPDATE / DELETE: returns the number of rows affected
        public static int Execute(
            string sql,
            Dictionary<string, object> parameters = null)
        {
            using (NpgsqlConnection conn =
                WAPP_EduCore.Data.DatabaseHelper.GetConnection())
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                if (parameters != null)
                {
                    foreach (var kv in parameters)
                    {
                        cmd.Parameters.AddWithValue(
                            kv.Key,
                            kv.Value ?? DBNull.Value);
                    }
                }

                return cmd.ExecuteNonQuery();
            }
        }
    }
}