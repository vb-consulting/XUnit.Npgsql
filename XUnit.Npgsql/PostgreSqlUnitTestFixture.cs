// Ignore Spelling: Postgre Npgsql Sql

using System;
using System.Collections.Generic;
using System.IO;
using Npgsql;

namespace XUnit.Npgsql
{

    public sealed class PostgreSqlUnitTestFixture : IDisposable
    {
        public NpgsqlConnection Connection { get; }

        public PostgreSqlUnitTestFixture()
        {
            Connection = new NpgsqlConnection(Config.ConnectionString);
            CreateTestDatabase(Connection);
            Connection.ChangeDatabase(Config.Value.TestDatabaseName);
            ApplyMigrations(Connection, Config.Value.UpScripts);
        }

        public void Dispose()
        {
            ApplyMigrations(Connection, Config.Value.DownScripts);
            Connection.Close();
            Connection.Dispose();
            using var connection = new NpgsqlConnection(Config.ConnectionString);
            DropTestDatabase(connection);
        }

        public static void Execute(NpgsqlConnection connection, string command)
        {
            using var cmd = connection.CreateCommand();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
            cmd.CommandText = command;
            cmd.ExecuteNonQuery();
        }

        public static void DropDatabase(NpgsqlConnection connection, string database)
        {
            Execute(connection, $@"
        {RevokeUsersCmd(database)}
        drop database {database};");
        }

        public static void CreateDatabase(NpgsqlConnection connection, string database, string template = null)
        {
            void DoCreate() => Execute(connection, $"create database {database}{(template == null ? "" : $" template {template}")}");
            if (template != null)
            {
                Execute(connection, RevokeUsersCmd(template));
            }
            try
            {
                DoCreate();
            }
            catch (PostgresException e)
            when (e.SqlState == "42P04") // 42P04=duplicate_database, see https://www.postgresql.org/docs/9.3/errcodes-appendix.html
            {
                DropDatabase(connection, database);
                DoCreate();
            }
        }

        private static void CreateTestDatabase(NpgsqlConnection connection)
        {
            if (Config.Value.TestDatabaseFromTemplate)
            {
                CreateDatabase(connection, Config.Value.TestDatabaseName, connection.Database);
            }
            else
            {
                CreateDatabase(connection, Config.Value.TestDatabaseName);
            }
        }

        private static void DropTestDatabase(NpgsqlConnection connection)
        {
            DropDatabase(connection, Config.Value.TestDatabaseName);
        }

        private static void ApplyMigrations(NpgsqlConnection connection, List<string> scriptPaths)
        {
            foreach (var path in scriptPaths)
            {
                Execute(connection, File.ReadAllText(path));
            }
        }
        private static string RevokeUsersCmd(string database)
        {
            return 
                $"revoke connect on database {database} from public; select pg_terminate_backend(pid) from pg_stat_activity where datname = '{database}' and pid <> pg_backend_pid();";
        }
    }
}
