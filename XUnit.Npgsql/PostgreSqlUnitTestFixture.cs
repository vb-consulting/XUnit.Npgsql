// Ignore Spelling: Postgre Npgsql Sql

using System;
using System.Collections.Generic;
using System.IO;
using Npgsql;

namespace XUnit.Npgsql
{
    public class PostgreSqlUnitTestFixture : IDisposable
    {
        public NpgsqlConnection Connection { get; }

        public PostgreSqlUnitTestFixture()
        {
            Connection = new NpgsqlConnection(Config.ConnectionString);

            if (!Config.Value.SkipCreateTestDatabase)
            {
                CreateTestDatabase(Connection);
            }
            if (Config.Value.TestDatabaseName != null && Config.Value.TestDatabaseName != Connection.Database)
            {
                Connection.ChangeDatabase(Config.Value.TestDatabaseName);
            }
            ApplyMigrations(Connection, Config.Value.UpScripts);
        }

        public virtual void Dispose()
        {
            ApplyMigrations(Connection, Config.Value.DownScripts);
            Connection.Close();
            Connection.Dispose();
            if (!Config.Value.SkipCreateTestDatabase)
            {
                using var connection = new NpgsqlConnection(Config.ConnectionString);
                DropTestDatabase(connection);
            }
        }

        public virtual void CreateTestDatabase(NpgsqlConnection connection)
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

        public virtual void DropTestDatabase(NpgsqlConnection connection)
        {
            DropDatabase(connection, Config.Value.TestDatabaseName);
        }

        public virtual void ApplyMigrations(NpgsqlConnection connection, List<string> scriptPaths)
        {
            if (scriptPaths == null)
            {
                return;
            }
            foreach (var path in scriptPaths)
            {
                Execute(connection, File.ReadAllText(path));
            }
        }
        
        internal void Execute(NpgsqlConnection connection, string command)
        {
            using var cmd = connection.CreateCommand();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
            cmd.CommandText = command;
            cmd.ExecuteNonQuery();
        }

        internal void DropDatabase(NpgsqlConnection connection, string database)
        {
            Execute(connection, $"revoke connect on database {database} from public");
            Execute(connection, $"select pg_terminate_backend(pid) from pg_stat_activity where datname = '{database}' and pid <> pg_backend_pid()");
            Execute(connection, $"drop database {database};");
        }

        internal void CreateDatabase(NpgsqlConnection connection, string database, string template = null)
        {
            void DoCreate() => Execute(connection, $"create database {database}{(template == null ? "" : $" template {template}")}");
            if (template != null)
            {
                Execute(connection, $"revoke connect on database {template} from public; select pg_terminate_backend(pid) from pg_stat_activity where datname = '{template}' and pid <> pg_backend_pid();");
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
    }
}
