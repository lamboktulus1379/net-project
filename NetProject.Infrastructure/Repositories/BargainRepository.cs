using System.Data.Odbc;
using System.Text;
using NetProject.Domain.Interfaces;
using NetProject.Domain.TransactionAggregates;

namespace NetProject.Infrastructure.Repositories;

public class BargainRepository(OdbcConnection dbConnection) : IBargainRepository
{
    public async Task<IEnumerable<History>> GetAll(string accountId, DateTime startDate, DateTime endDate)
    {
        try
        {
            await dbConnection.OpenAsync();
            var command =
                new OdbcCommand(
                    @"SELECT szTransactionId, szAccountId, szCurrencyId, dtmTransaction, decAmount, szNote FROM dbo.[BOS_History]
                WHERE szAccountId = ? AND dtmTransaction BETWEEN ? AND ?",
                    dbConnection);
            command.Parameters.Add(new OdbcParameter("szAccountId", OdbcType.VarChar)).Value = accountId;
            command.Parameters.Add(new OdbcParameter("startDate", OdbcType.DateTime)).Value = startDate;
            command.Parameters.Add(new OdbcParameter("endDate", OdbcType.DateTime)).Value = endDate;
            var reader = await command.ExecuteReaderAsync();
            var histories = new List<History>();
            while (await reader.ReadAsync())
            {
                var history = new History
                {
                    TransactionId = reader.GetString(0),
                    AccountId = reader.GetString(1),
                    CurrencyId = reader.GetString(2),
                    DateTimeTransaction = reader.GetDateTime(3),
                    Amount = reader.GetDecimal(4),
                    Note = reader.GetString(5)
                };
                histories.Add(history);
            }

            await dbConnection.CloseAsync();
            return histories;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task Deposit(string accountId, string[] accountIds, decimal Amount)
    {
    }

    public void Withdraw()
    {
    }

    public async Task<string> Transfer(string accountId, string[] accountIds, decimal amount)
    {
        List<string> res = new();
        var note = Note.TRANSFER.ToString();
        await dbConnection.OpenAsync();

        // Set the transaction isolation level to snapshot
        await using (OdbcCommand setIsolationLevelCommand =
                     new OdbcCommand("SET TRANSACTION ISOLATION LEVEL SNAPSHOT", dbConnection))
        {
            await setIsolationLevelCommand.ExecuteNonQueryAsync();
        }

        // Start a transaction
        OdbcTransaction transaction = dbConnection.BeginTransaction();

        try
        {
            var numberAccounts = accountIds.Length;

            // Update the BOS_Balance table for the account that is transferring the money.
            // Read and lock the current balance
            string selectQuery =
                "SELECT decAmount FROM BOS_Balance WITH (UPDLOCK, ROWLOCK) WHERE szAccountId = ? AND szCurrencyId = ?";
            OdbcCommand selectCommand = new OdbcCommand(selectQuery, dbConnection, transaction);
            selectCommand.Parameters.AddWithValue("@szAccountId", accountId);
            selectCommand.Parameters.AddWithValue("@szCurrencyId", Currency.IDR.ToString());

            decimal currentBalance = 0;
            await using (var reader = await selectCommand.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    currentBalance = reader.GetDecimal(reader.GetOrdinal("decAmount"));
                }
                else
                {
                    currentBalance = 0;
                }
            }

            decimal totalAmountSubstracted = numberAccounts * amount;
            if (currentBalance < totalAmountSubstracted)
            {
                throw new Exception("Insufficient balance.");
            }

            // Insert two records for each account in the BOS_History table.
            StringBuilder insertQuery =
                new StringBuilder(
                    "INSERT INTO BOS_History (szTransactionId, szAccountId, szCurrencyId, dtmTransaction, decAmount, szNote) VALUES ");
            List<OdbcParameter> parameters = new List<OdbcParameter>();

            for (var i = 0; i < numberAccounts; i++)
            {
                bool receiverExists = false;
                // Update the balance for the account that is receiving the money
                selectQuery =
                    "SELECT decAmount FROM BOS_Balance WITH (UPDLOCK, ROWLOCK) WHERE szAccountId = ? AND szCurrencyId = ?";
                selectCommand = new OdbcCommand(selectQuery, dbConnection, transaction);
                selectCommand.Parameters.AddWithValue("@szAccountId", accountIds[i]);
                selectCommand.Parameters.AddWithValue("@szCurrencyId", Currency.IDR.ToString());

                decimal transferBalance = 0;
                await using (var reader = await selectCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        transferBalance = reader.GetDecimal(reader.GetOrdinal("decAmount"));
                        receiverExists = true;
                    }
                    else
                    {
                        transferBalance = 0;
                    }
                }

                if (receiverExists)
                {
                    // Update the balance for account that is transferring the money
                    decimal newBalance = currentBalance - amount;
                    string updateQuery =
                        "UPDATE BOS_Balance SET decAmount = ? WHERE szAccountId = ? AND szCurrencyId = ?";
                    OdbcCommand updateCommand = new OdbcCommand(updateQuery, dbConnection, transaction);
                    updateCommand.Parameters.AddWithValue("@decAmount", newBalance);
                    updateCommand.Parameters.AddWithValue("@szAccountId", accountId);
                    updateCommand.Parameters.AddWithValue("@szCurrencyId", Currency.IDR.ToString());
                    await updateCommand.ExecuteNonQueryAsync();
                    currentBalance = newBalance;

                    // Update the balance for the account that is receiving the money
                    transferBalance += amount;
                    string updateQueryReceiver =
                        "UPDATE BOS_Balance SET decAmount = ? WHERE szAccountId = ? AND szCurrencyId = ?";
                    OdbcCommand updateCommandReceiver = new OdbcCommand(updateQueryReceiver, dbConnection, transaction);
                    updateCommandReceiver.Parameters.AddWithValue("@decAmount", transferBalance);
                    updateCommandReceiver.Parameters.AddWithValue("@szAccountId", accountIds[i]);
                    updateCommandReceiver.Parameters.AddWithValue("@szCurrencyId", Currency.IDR.ToString());
                    await updateCommandReceiver.ExecuteNonQueryAsync();
                }

                // Retrieve and increment the last number from BOS_Counter
                string counterQuery = "SELECT iLastNumber FROM BOS_Counter WHERE szCounterId = '001-COU'";
                OdbcCommand counterCommand = new OdbcCommand(counterQuery, dbConnection, transaction);
                long iLastNumber = (long)await counterCommand.ExecuteScalarAsync();

                // Increment the last number
                iLastNumber++;

                // Update the BOS_Counter table with the new last number
                string updateCounterQuery = "UPDATE BOS_Counter SET iLastNumber = ? WHERE szCounterId = '001-COU'";
                OdbcCommand updateCounterCommand = new OdbcCommand(updateCounterQuery, dbConnection, transaction);
                updateCounterCommand.Parameters.AddWithValue("@iLastNumber", iLastNumber);
                await updateCounterCommand.ExecuteNonQueryAsync();

                // Generate the new szTransactionId
                DateTime originalDateTime = DateTime.Now;
                string szTransactionId = $"{originalDateTime:yyyyMMdd}-00000.{iLastNumber:D5}";
                res.Add(szTransactionId);

                DateTime adjustedDateTime = new DateTime(
                    originalDateTime.Year,
                    originalDateTime.Month,
                    originalDateTime.Day,
                    originalDateTime.Hour,
                    originalDateTime.Minute,
                    originalDateTime.Second
                );

                insertQuery.Append($"(?, ?, ?, ?, ?, ?)");
                insertQuery.Append(", ");
                insertQuery.Append($"(?, ?, ?, ?, ?, ?)");
                parameters.AddRange(new OdbcParameter[]
                {
                    new OdbcParameter($"@szTransactionId{i}", szTransactionId),
                    new OdbcParameter($"@szAccountId{i}", accountId),
                    new OdbcParameter($"@szCurrencyId{i}", Currency.IDR.ToString()),
                    new OdbcParameter($"@dtmTransaction{i}", OdbcType.DateTime) { Value = adjustedDateTime },
                    new OdbcParameter($"@decAmount{i}", -amount),
                    new OdbcParameter($"@szNote{i}", note)
                });
                parameters.AddRange(new OdbcParameter[]
                {
                    new OdbcParameter($"@szTransactionId{i}", szTransactionId),
                    new OdbcParameter($"@szAccountId{i}", accountIds[i]),
                    new OdbcParameter($"@szCurrencyId{i}", Currency.IDR.ToString()),
                    new OdbcParameter($"@dtmTransaction{i}", OdbcType.DateTime) { Value = adjustedDateTime },
                    new OdbcParameter($"@decAmount{i}", amount),
                    new OdbcParameter($"@szNote{i}", note)
                });
            }

            OdbcCommand insertCommand = new OdbcCommand(insertQuery.ToString(), dbConnection, transaction);
            insertCommand.Parameters.AddRange(parameters.ToArray());
            await insertCommand.ExecuteNonQueryAsync();

            // Commit the transaction
            transaction.Commit();

            return string.Join(",", res);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            transaction.Rollback();
            throw;
        }
    }
}