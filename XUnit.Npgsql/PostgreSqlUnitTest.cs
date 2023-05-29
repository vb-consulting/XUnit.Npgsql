// Ignore Spelling: Postgre Npgsql Sql

using System;
using Npgsql;
using Xunit;

namespace XUnit.Npgsql
{
    /// <summary>
    /// PostgreSQL Unit Test Fixture using configuration settings from testsettings.json
    /// </summary>
    [Collection("PostgreSqlDatabase")]
    public abstract class PostgreSqlUnitTest : IDisposable
    {
        private readonly PostgreSqlUnitTestFixture tests;
        private readonly bool newDatabaseFromTemplate;
        private readonly bool underTransaction;

        public NpgsqlConnection Connection { get; private set; }

        protected PostgreSqlUnitTest(
            PostgreSqlUnitTestFixture tests,
            bool? newDatabaseFromTemplate = null,
            string testDatabaseName = null,
            bool? underTransaction = null,
            bool? disableConstraintCheckingForTransaction = null)
        {
            this.tests = tests;
            this.newDatabaseFromTemplate = newDatabaseFromTemplate ?? Config.Value.UnitTestsNewDatabaseFromTemplate;
            this.underTransaction = underTransaction ?? Config.Value.UnitTestsUnderTransaction;

            if (this.newDatabaseFromTemplate)
            {
                var dbName = testDatabaseName ?? string.Concat(Config.Value.TestDatabaseName, "_", Guid.NewGuid().ToString()[..8]);
                using var connection = new NpgsqlConnection(Config.ConnectionString);
                tests.CreateDatabase(connection, dbName, connection.Database);
                Connection = tests.Connection.CloneWith(tests.Connection.ConnectionString);
                Connection.Open();
                Connection.ChangeDatabase(dbName);
            }
            else
            {
                Connection = tests.Connection.CloneWith(tests.Connection.ConnectionString);
                Connection.Open();
            }
            if (this.underTransaction)
            {
                bool disableConstraints = disableConstraintCheckingForTransaction ?? Config.Value.DisableConstraintCheckingForTransaction;
                if (disableConstraints)
                {
                    tests.Execute(Connection, "begin; set constraints all deferred;");
                }
                else
                {
                    tests.Execute(Connection, "begin");
                }
            }
        }

        public virtual void Dispose()
        {
            if (this.underTransaction)
            {
                tests.Execute(Connection, "rollback");
            }
            Connection.Close();
            Connection.Dispose();
            if (this.newDatabaseFromTemplate)
            {
                using var connection = new NpgsqlConnection(Config.ConnectionString);
                tests.DropDatabase(connection, Connection.Database);
            }
        }
    }
}
