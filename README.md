# XUnit.Npgsql

_XUnit Unit Testing for PostgreSQL Databases and .NET Standard 2.1_
 
![build-publish](https://github.com/vb-consulting/XUnit.Npgsql/workflows/build-publish/badge.svg)

## Basic Usage

1) Create a new XUnit Test Project.

2) Add a reference to the XUnit.Npgsql package. e.g. `dotnet add package XUnit.Npgsql`
 
3) Add new collection fixture to your project:

```csharp
using XUnit.Npgsql;

[CollectionDefinition("PostgreSqlDatabase")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlUnitTestFixture> { }
```

- Note: The collection name must be always `PostgreSqlDatabase`.

- Note: Collection class name is arbitrary (in this example `DatabaseCollection`).

4) Add configuration file to your XUnit Test Project project root named `testsettings.json`.

- Note: see sample configuration file here: [testsettings.json sample](https://github.com/vb-consulting/XUnit.Npgsql/testsettings-config-sample.jsonc)

- Note: Make sure that configuration file is copied to output directory on build.

5) Add new test class to your project.

Note: test class must inherit `PostgreSqlUnitTest` class.

```csharp
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

- Note: see sample configuration file here: [testsettings.json sample](https://github.com/vb-consulting/XUnit.Npgsql/testsettings-config-sample.jsonc)

- Configuration can contain `ConnectionStrings` section

- Configuration file must contain `TestSettings` section. This sections can have these values:

  - `TestConnection` - connection string name from `ConnectionStrings` section to the test database.
  - `ConfigPath` - path to the configuration file. This is useful when you want to use different configuration file for different environments. e.g. `ConfigPath: "appsettings.Development.json"`
  - `TestDatabaseName` - name of the test database. Default connection is used for server and this name for database name. If not specified, test database will be created with random name. If it is specified name will be appended with random string to ensure that test database is unique. This database is recreated and dropped on every test session.
  - `TestDatabaseFromTemplate` - If set to true, the test database will be created from a [template database](https://www.postgresql.org/docs/current/manage-ag-templatedbs.html). PostgreSQL can create a new database from a template database together with data, very fast.
  - `UpScripts` - list of scripts to run before tests. Scripts are run in order. 
  - `DownScripts` - list of scripts to run after tests. Scripts are run in order.
  - `UnitTestsUnderTransaction` - if set to true, all tests will be run under transaction that is rolled-back when test ends. This is useful when you want to run tests in parallel. Default is true.
  - `DisableConstraintCheckingForTransaction` - when running tests under transaction, this will disable constraint checking. This is useful when arranging test data, all related tables don't have to be populated. Default is true. 
  - `UnitTestsNewDatabaseFromTemplate` - if set to true, all tests will run in unique database created from a template. This is useful when testing sequences. Default is false.

### Overriding configuration in test class

```csharp
using XUnit.Npgsql;
using Norm;

public class MyDatabaseTests : PostgreSqlUnitTest
{
    public MyDatabaseTests(PostgreSqlUnitTestFixture tests) : base(
        tests,
        newDatabaseFromTemplate: false,
        testDatabaseName: "test_database_name",
        underTransaction: true,
        disableConstraintCheckingForTransaction: false) { }


    [Fact]
    public void Test()
    {
        // arrange, act, assert ...
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

## Currently supported platforms
 
- .NET Standard 2.1
 
## Support
 
This is open-source software developed and maintained freely without any compensation whatsoever.
 
## Licence
 
Copyright (c) Vedran Bilopavlović - VB Consulting and VB Software 2020
This source code is licensed under the [MIT license](https://github.com/vbilopav/XUnit.Npgsql/blob/master/LICENSE).

