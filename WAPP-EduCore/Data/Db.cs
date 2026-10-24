using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Mail;

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

    // Account rules shared by the settings pages (everything comes from the users table).
    // A rule, for example password hashing later, is then changed in ONE place.
    public static class AccountService
    {
        public class Result
        {
            public bool Ok { get; private set; }
            public string Message { get; private set; }

            public static Result Success(string message) { return new Result { Ok = true, Message = message }; }
            public static Result Fail(string message) { return new Result { Ok = false, Message = message }; }
        }

        public class UserInfo
        {
            public long UserId { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public DateTime? Dob { get; set; }
            public DateTime? CreatedAt { get; set; }
        }

        public static UserInfo GetUser(long userId)
        {
            DataTable t = Db.Query(
                "SELECT user_id, full_name, email, role, dob, created_at FROM users WHERE user_id = @uid",
                new Dictionary<string, object> { { "@uid", userId } });

            if (t.Rows.Count == 0) return null;

            DataRow r = t.Rows[0];
            return new UserInfo
            {
                UserId = Convert.ToInt64(r["user_id"]),
                FullName = Convert.ToString(r["full_name"]),
                Email = Convert.ToString(r["email"]),
                Role = Convert.ToString(r["role"]),
                Dob = r["dob"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["dob"]).Date,
                CreatedAt = r["created_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["created_at"])
            };
        }

        // Checks the details without saving anything.
        public static Result ValidateDetails(long userId, string fullName, string email, DateTime? dob)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return Result.Fail("Please enter your full name.");
            if (!IsValidEmail(email))
                return Result.Fail("Please enter a valid email address.");
            if (dob.HasValue && dob.Value.Date > DateTime.Today)
                return Result.Fail("Date of birth cannot be in the future.");

            DataTable dup = Db.Query(
                "SELECT 1 FROM users WHERE lower(email) = lower(@e) AND user_id <> @uid",
                new Dictionary<string, object> { { "@e", email.Trim() }, { "@uid", userId } });
            if (dup.Rows.Count > 0)
                return Result.Fail("That email address is already used by another account.");

            return Result.Success("");
        }

        public static Result UpdateDetails(long userId, string fullName, string email, DateTime? dob)
        {
            Result check = ValidateDetails(userId, fullName, email, dob);
            if (!check.Ok) return check;

            try
            {
                Db.Execute(
                    "UPDATE users SET full_name = @n, email = @e, dob = CAST(@dob AS date) WHERE user_id = @uid",
                    new Dictionary<string, object>
                    {
                        { "@n", fullName.Trim() },
                        { "@e", email.Trim() },
                        { "@dob", dob.HasValue ? (object)dob.Value.Date : DBNull.Value },
                        { "@uid", userId }
                    });
            }
            catch (Exception ex)
            {
                return Result.Fail("Could not save your changes: " + ex.Message);
            }

            return Result.Success("Your changes have been saved.");
        }

        public static Result ChangePassword(long userId, string current, string newPassword, string confirm)
        {
            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirm))
                return Result.Fail("Please fill in all three password fields.");

            try
            {
                DataTable t = Db.Query(
                    "SELECT password_hash FROM users WHERE user_id = @uid",
                    new Dictionary<string, object> { { "@uid", userId } });

                // login.aspx.cs compares the typed password with this column directly (plain text for now).
                // When hashing is added, this method and the login page are the places to change.
                if (t.Rows.Count == 0 || Convert.ToString(t.Rows[0]["password_hash"]) != current)
                    return Result.Fail("Your current password is incorrect.");
                if (newPassword.Length < 8)
                    return Result.Fail("The new password must be at least 8 characters.");
                if (newPassword != confirm)
                    return Result.Fail("The new passwords do not match.");
                if (newPassword == current)
                    return Result.Fail("The new password must be different from the current one.");

                Db.Execute(
                    "UPDATE users SET password_hash = @p WHERE user_id = @uid",
                    new Dictionary<string, object> { { "@p", newPassword }, { "@uid", userId } });
            }
            catch (Exception ex)
            {
                return Result.Fail("Could not change the password: " + ex.Message);
            }

            return Result.Success("Your password has been updated.");
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                MailAddress a = new MailAddress(email.Trim());
                return a.Address == email.Trim();
            }
            catch { return false; }
        }

        // "Priya Sharma" -> "PS"
        public static string InitialsOf(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            string[] parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string initials = parts[0].Substring(0, 1);
            if (parts.Length > 1) initials += parts[parts.Length - 1].Substring(0, 1);
            return initials.ToUpperInvariant();
        }
    }
}