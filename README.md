# XUnit.Npgsql

_XUnit Unit Testing for PostgreSQL Databases and .NET Standard 2.1_
 
![build-publish](https://github.com/vb-consulting/XUnit.Npgsql/workflows/build-publish/badge.svg)

## Basic Usage

1) Create a new XUnit Test Project.

2) Add a reference to the XUnit.Npgsql package. e.g. `dotnet add package XUnit.Npgsql`
 
3) Add new collection fixture to your project:

```cs
using XUnit.Npgsql;

[CollectionDefinition("PostgreSqlDatabase")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlUnitTestFixture> { }
```

- Note: The collection name must be always `PostgreSqlDatabase`.

- Note: Collection class name is arbitrary (in this example `DatabaseCollection`).

4) Add configuration file to your XUnit Test Project project root named `testsettings.json`.

- Note: see sample configuration file here: [testsettings.json sample](https://github.com/vb-consulting/XUnit.Npgsql/blob/master/testsettings-config-sample.jsonc)

- Note: Make sure that configuration file is copied to output directory on build.

5) Add new test class to your project.

Note: test class must inherit `PostgreSqlUnitTest` class.

```cs
using XUnit.Npgsql;
using Norm;

public class MyDatabaseTests : PostgreSqlUnitTest
{
    public MyDatabaseTests(PostgreSqlUnitTestFixture tests) : base(tests) { }

    // example database unit test
    [Fact]
    public void Test_Get_Company_Details()
    {
        //
        // Arrange
        //
        Guid? id = Connection
            .Read<Guid>(command: @"insert into companies (name, linkedin) values ('name', 'linkedin') returning id")
            .Single();

        //
        // Act
        //
        var result = Connection.Read(new
            {
                id = default(Guid),
                name = default(string),
                linkedin = default(string)
            },
            "select get_company_details(@id)", new { id }) // get_company_details udf
            .Single();

        //
        // Assert
        //
        result.Should().BeEquivalentTo(new
        {
            id,
            name = "name",
            linkedin = "linkedin"
        });
    }
}
```

## Advanced Usage

### Configuration file:

- Configuration file is mandatory.

- Configuration needs to be copied to output directory on build.

- Configuration file name must be either `testsettings.json`, `testsettings.Development.json`˙, `appsettings.json` or `appsettings.Development.json` in that order.

- Note: see sample configuration file here: [testsettings.json sample](https://github.com/vb-consulting/XUnit.Npgsql/blob/master/testsettings-config-sample.jsonc)

- Configuration can contain `ConnectionStrings` section

- Configuration file must contain `TestSettings` section. This sections can have these values:

  - `TestConnection` - connection string name from `ConnectionStrings` section to the test database.
  - `ConfigPath` - path to the configuration file. This is useful when you want to use different configuration file for different environments. e.g. `ConfigPath: "appsettings.Development.json"`
  - `TestDatabaseName` - name of the test database. Default connection is used for server and this name for database name. If not specified, test database will be created with random name. If it is specified name will be appended with random string to ensure that test database is unique. This database is recreated and dropped on every test session unless `SkipCreateTestDatabase` is set to true.
  - `SkipCreateTestDatabase` - skip creating and dropping test database. Default is false.
  - `TestDatabaseFromTemplate` - If set to true, the test database will be created from a [template database](https://www.postgresql.org/docs/current/manage-ag-templatedbs.html). PostgreSQL can create a new database from a template database together with data, very fast.
  - `UpScripts` - list of scripts to run before tests. Scripts are run in order. 
  - `DownScripts` - list of scripts to run after tests. Scripts are run in order.
  - `UnitTestsUnderTransaction` - if set to true, all tests will be run under transaction that is rolled-back when test ends. This is useful when you want to run tests in parallel. Default is true.
  - `DisableConstraintCheckingForTransaction` - when running tests under transaction, this will disable constraint checking. This is useful when arranging test data, all related tables don't have to be populated. Default is true. 
  - `UnitTestsNewDatabaseFromTemplate` - if set to true, all tests will run in unique database created from a template. This is useful when testing sequences. Default is false.

### Overriding configuration in test class

```cs
using XUnit.Npgsql;
using Norm;

public class MyDatabaseTests : PostgreSqlUnitTest
{
    public MyDatabaseTests(PostgreSqlUnitTestFixture tests) : base(
        tests,
        // override "create new database from template" behavior for these tests
        newDatabaseFromTemplate: false, 
        // override test database name for these tests (if set name is exact, without guid)
        testDatabaseName: "test_database_name", 
        // override "run tests under transaction" behavior for these tests
        underTransaction: true, 
        // override "disable constraint checking for transaction" behavior for these tests
        disableConstraintCheckingForTransaction: false 
    ) { }

    [Fact]
    public void Test()
    {
        // arrange
        // act
        // assert ...
    }
}
```

### Overriding configuration test fixtures class

```cs
[CollectionDefinition("PostgreSqlDatabase")]
public class TestCollection : ICollectionFixture<CustomTestFixtures> { }

public class CustomTestFixtures : PostgreSqlUnitTestFixture
{
    public MyPostgreSqlUnitTestFixture() : base()
    {
        // override default constructor before tests are run
        // this is executed once per project
    }

    public override void Dispose()
    {
        // override default dispose after tests are run
        // this is executed once per project
        base.Dispose();
    }

    public override void CreateTestDatabase(NpgsqlConnection connection)
    {
        // override creating test database for all tests or call base method (that creates database from configuration)
        // CreateTestDatabase is called from default constructor
        base.CreateTestDatabase(connection);
    }

    public override void DropTestDatabase(NpgsqlConnection connection)
    {
        // override dropping test database for all tests or call base method (that creates database from configuration)
        // DropTestDatabase is called from Dispose method
        base.DropTestDatabase(connection);
    }

    public override void ApplyMigrations(NpgsqlConnection connection, List<string> scriptPaths)
    {
        // override apply migrations method
        // this method is called twice: 
        // 1) from default constructor (after CreateTestDatabase) for up migrations
        // 2) from Dispose method (before DropTestDatabase) for down migrations
        base.ApplyMigrations(connection, scriptPaths);
    }
}
```

## Dependencies

```
Microsoft.Extensions.Configuration >= 7.0.0
Microsoft.Extensions.Configuration.Binder >= 7.0.4
Microsoft.Extensions.Configuration.Json >= 7.0.0
Npgsq >= 7.0.0
xunit >= 2.0.0
```

## Changelog

## [1.1.3](https://github.com/vb-consulting/XUnit.Npgsql/tree/1.1.3) (2023-09-06)

[Full Changelog](https://github.com/vb-consulting/XUnit.Npgsql/compare/1.1.2...1.1.3)

Expose `IConfigurationRoot` as static public readonly property of ťhe `Config`  class.

This is useful to access custom configuiration values without having to load configuiration file again (e.g. `Config.ConfigurationRoot.GetConnectionString("AdminConnection")`)

## [1.1.2](https://github.com/vb-consulting/XUnit.Npgsql/tree/1.1.2) (2023-08-27)

[Full Changelog](https://github.com/vb-consulting/XUnit.Npgsql/compare/1.1.1...1.1.2)

Add default value to config and check is string null or empty.

## [1.1.1](https://github.com/vb-consulting/XUnit.Npgsql/tree/1.1.1) (2023-08-27)

[Full Changelog](https://github.com/vb-consulting/XUnit.Npgsql/compare/1.1.0...1.1.1)

If `SkipCreateTestDatabase` is true and `TestDatabaseName` is set, change the connection database to `TestDatabaseName` instead of the default database.

## [1.1.0](https://github.com/vb-consulting/XUnit.Npgsql/tree/1.1.0) (2023-08-27)

[Full Changelog](https://github.com/vb-consulting/XUnit.Npgsql/compare/1.0.2...1.1.0)

### New setting SkipCreateTestDatabase

Set to true to skip the creation of the test database.

The database from the connection string will be used directly and no test database will be created or dropped.

This doesn't apply to `UnitTestsNewDatabaseFromTemplate` setting will create and drop new template database from this database on each unit test.
This settings cannot be combined with `TestDatabaseFromTemplate` because the you can't create a template database if you choose to skip creating a test database.

### Improvements

- Fixed strange PostgreSQL bug where test database can't be dropped because PostgreSQL thinks it's in pipeline mode (although it isn't).

- Add global counter for test database name when using `TestDatabaseFromTemplate` for each unit tests (besides timestamps guid).

## Currently supported platforms
 
- .NET Standard 2.1
 
## Support
 
This is open-source software developed and maintained freely without any compensation whatsoever.
 
## Licence
 
Copyright (c) Vedran Bilopavlović - VB Consulting and VB Software 2020
This source code is licensed under the [MIT license](https://github.com/vbilopav/XUnit.Npgsql/blob/master/LICENSE).

