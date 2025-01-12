using System.Data.Odbc;
using NetProject.Domain.DataTransferObjects;
using NetProject.Domain.TransactionAggregates;
using NetProject.Infrastructure.Repositories;

namespace NetProject.Test.UnitTests;

[TestClass]
public class BargainRepositoryTest
{
    private readonly OdbcConnection _connection;
    private readonly string _connectionString;
    private string[] _tableNames;
    private BargainRepository _repository;

    public BargainRepositoryTest(string[] tableNames, BargainRepository repository)
    {
        _tableNames = tableNames;
        _repository = repository;
        var connectionStringTemplate =
            "Driver={ODBC driver 18 for SQL Server};Server={SRV};Database={DB};Uid={USR};Pwd={PWD};TrustServerCertificate=yes;";
        var server = Environment.GetEnvironmentVariable("SRV");
        var database = Environment.GetEnvironmentVariable("DB");
        var user = Environment.GetEnvironmentVariable("USR");
        var password = Environment.GetEnvironmentVariable("PWD");
        _connectionString = connectionStringTemplate.Replace("{SRV}", server)
            .Replace("{DB}", database)
            .Replace("{USR}", user).Replace("{PWD}", password);
        _connection = new OdbcConnection(_connectionString);
    }

    [TestInitialize]
    public async Task TestInitialize()
    {
        _tableNames = ["BOS_Counter", "BOS_Balance", "BOS_History"];
        _repository = new BargainRepository(_connection);

        // Reset the database state
        await ResetDatabaseState();
    }

    private async Task ResetDatabaseState()
    {
        await TearDown(_tableNames);
        await TearUp();
    }

    [TestMethod]
    [DataRow("000108757484", new[] { "000109999999" }, 1.0)]
    public async Task Transfer_Input1_ReturnBalancePlus1(string accountId, string[] accountIds, double amount)
    {
        // Arrange
        decimal amountDecimal = Convert.ToDecimal(amount);

        // Act
        var tos = new List<To>()
        {
            new To()
            {
                AccountId = accountIds[0],
                Currency = Currency.IDR,
                Amount = amountDecimal
            }
        };
        var result = await _repository.Transfer(accountId, tos.ToArray());

        // Assert
        Assert.AreEqual(true, result);
    }

    private async Task TearUp()
    {
        await using var dbConnection = new OdbcConnection(_connectionString);
        await dbConnection.OpenAsync();
        const string sql = """
                           
                                                   ---------------
                           -- STRUCTURE --
                           ---------------

                           CREATE TABLE BOS_Counter
                           (
                           	szCounterId NVARCHAR(50) NOT NULL,
                           	iLastNumber BIGINT NOT NULL,
                           	CONSTRAINT PK_BOS_Counter PRIMARY KEY CLUSTERED
                           	(
                           		szCounterId ASC
                           	)
                           );

                           CREATE TABLE BOS_Balance
                           (
                           	szAccountId NVARCHAR(50) NOT NULL,
                           	szCurrencyId NVARCHAR(50) NOT NULL,
                           	decAmount DECIMAL (30, 8) NOT NULL,
                           	CONSTRAINT PK_BOS_Balance PRIMARY KEY CLUSTERED
                           	(
                           		szAccountId ASC, szCurrencyId ASC
                           	)
                           );

                           CREATE TABLE BOS_History
                           (
                           	szTransactionId NVARCHAR(50) NOT NULL,
                           	szAccountId NVARCHAR(50) NOT NULL,
                           	szCurrencyId NVARCHAR(50) NOT NULL,
                           	dtmTransaction DATETIME NOT NULL,
                           	decAmount DECIMAL (30, 8) NOT NULL,
                           	szNote NVARCHAR(255) NOT NULL,
                           	CONSTRAINT PK_BOS_History PRIMARY KEY CLUSTERED
                           	(
                           		szTransactionId ASC, szAccountId ASC, szCurrencyId ASC
                           	)
                           );

                           --------------------
                           -- INITIALIZATION --
                           --------------------

                           INSERT INTO BOS_Counter VALUES
                           ('001-COU', 4);

                           INSERT INTO BOS_Balance VALUES 
                           ('000108757484', 'IDR', 34500000.00),
                           ('000108757484', 'USD', 125.8750),
                           ('000109999999', 'IDR', 1250.00),
                           ('000109999999', 'SGD', 2.25),
                           ('000108888888', 'SGD', 125.75);

                           INSERT INTO BOS_History VALUES
                           ('20201231-00000.00001', '000108757484', 'IDR', GETDATE(), 34500000.00, 'SETOR'),
                           ('20201231-00000.00001', '000108757484', 'SGD', GETDATE(), 125.8750, 'SETOR'),
                           ('20201231-00000.00002', '000109999999', 'IDR', GETDATE(), 1250.00, 'SETOR'),
                           ('20201231-00000.00003', '000109999999', 'SGD', GETDATE(), 128.00, 'SETOR'),
                           ('20201231-00000.00004', '000109999999', 'SGD', GETDATE(), -125.75, 'TRANSFER'),
                           ('20201231-00000.00004', '000108888888', 'SGD', GETDATE(), 125.75, 'TRANSFER');
                                               
                           """;

        await using OdbcCommand command = new OdbcCommand(sql, dbConnection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task TearDown(string[] tableNames)
    {
        await using var dbConnection = _connection;
        await dbConnection.OpenAsync();

        foreach (var tableName in tableNames)
        {
            var dropTableQuery = $@"
                IF OBJECT_ID(N'{tableName}', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE {tableName};
                END";

            await using var dropTableCommand = new OdbcCommand(dropTableQuery, dbConnection);
            await dropTableCommand.ExecuteNonQueryAsync();
        }
    }
}