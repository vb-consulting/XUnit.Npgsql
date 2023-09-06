// Ignore Spelling: Npgsql

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace XUnit.Npgsql
{
    public class Config
    {
        public string TestConnection { get; set; }
        public string ConfigPath { get; set; }
        public string TestDatabaseName { get; set; }
        public bool SkipCreateTestDatabase { get; set; } = false;
        public bool TestDatabaseFromTemplate { get; set; } = false;
        public List<string> UpScripts { get; set; } = new List<string>();
        public List<string> DownScripts { get; set; } = new List<string>();
        public bool UnitTestsUnderTransaction { get; set; } = true;
        public bool DisableConstraintCheckingForTransaction { get; set; } = true;
        public bool UnitTestsNewDatabaseFromTemplate { get; set; } = false;

        public static Config Value { get; }
        public static IConfigurationRoot ConfigurationRoot { get; }
        public static string ConnectionString { get; }

        static Config()
        {
            Value = new Config();
            ConfigurationRoot = new ConfigurationBuilder()
                .AddJsonFile("testsettings.json", optional: true, false)
                .AddJsonFile("testsettings.Development.json", true, false)
                .AddJsonFile("appsettings.json", true, false)
                .AddJsonFile("appsettings.Development.json", true, false)
                .Build();
            ConfigurationRoot.GetSection("TestSettings").Bind(Value);

            ValidateAndThrow();

            string? externalConnectionString = null;
            if (!string.IsNullOrEmpty(Value.ConfigPath) && File.Exists(Value.ConfigPath))
            {
                var external = new ConfigurationBuilder().AddJsonFile(Path.Join(Directory.GetCurrentDirectory(), Value.ConfigPath), false, false).Build();
                externalConnectionString = external.GetConnectionString(Value.TestConnection);
            }
            ConnectionString = ConfigurationRoot.GetConnectionString(Value.TestConnection) ?? externalConnectionString;

            // append random number to test database name to avoid conflicts, only if we choose to create new test database
            if (!Value.SkipCreateTestDatabase)
            {
                Value.TestDatabaseName = string.Concat(Value.TestDatabaseName, "_", Guid.NewGuid().ToString()[..8]);
            }
        }

        private static void ValidateAndThrow()
        {
            if (Value.TestDatabaseFromTemplate && Value.UnitTestsNewDatabaseFromTemplate)
            {
                throw new ArgumentException(@"Configuration settings TestDatabaseFromTemplate=true and UnitTestsNewDatabaseFromTemplate=true doesn't make any sense.
There is no point of creating a test database from a template and to do that again for each unit test.
Set one of TestDatabaseFromTemplate or UnitTestsNewDatabaseFromTemplate to false.");
            }
            if (Value.UnitTestsNewDatabaseFromTemplate && Value.UnitTestsUnderTransaction)
            {
                throw new ArgumentException(@"Configuration settings UnitTestsNewDatabaseFromTemplate=true and UnitTestsUnderTransaction=true doesn't make any sense.
There is no point of creating a new test database from a template for each test and then use transaction on a database where only one test runs.
Set one of UnitTestsNewDatabaseFromTemplate or UnitTestsUnderTransaction to false.");
            }
            if (Value.UnitTestsNewDatabaseFromTemplate && (Value.UpScripts.Any() || Value.DownScripts.Any()))
            {
                throw new ArgumentException(@"Configuration settings UnitTestsNewDatabaseFromTemplate=true and up or down scripts (UpScripts, DownScripts) doesn't make any sense.
Up or down scripts are only applied on a test database created for all tests.
Set one of UnitTestsNewDatabaseFromTemplate or clear UpScripts and DownScripts.");
            }
            if (Value.DisableConstraintCheckingForTransaction && !Value.UnitTestsUnderTransaction)
            {
                throw new ArgumentException(@"Configuration settings DisableConstraintCheckingForTransaction=true and UnitTestsUnderTransaction=false doesn't make any sense.
Disabling constraint checking works only under transaction.");
            }

            if (Value.SkipCreateTestDatabase && Value.TestDatabaseFromTemplate)
            {
                throw new ArgumentException(@"Configuration settings SkipCreateTestDatabase=true and TestDatabaseFromTemplate=true doesn't make any sense.
You can't create a template database if you choose to skip creating a test database");
            }
        }
    }
}
