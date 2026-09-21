using System;
using System.Configuration;
using Npgsql;

namespace WAPP_EduCore.Data
{
    public static class DatabaseHelper
    {
        private static readonly string connectionString =
            ConfigurationManager.ConnectionStrings["SupabasePostgres"].ConnectionString;

        public static NpgsqlConnection GetConnection()
        {
            NpgsqlConnection conn = new NpgsqlConnection(connectionString);
            conn.Open();
            return conn;
        }
    }
}