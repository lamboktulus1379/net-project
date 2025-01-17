using System.Data.Odbc;
using System.Text;
using NetProject.Domain.DataTransferObjects;
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

    public async Task<decimal> Deposit(string accountId, decimal Amount)
    {
        await dbConnection.OpenAsync();
        // Set the transaction isolation level to snapshot
        await using (OdbcCommand setIsolationLevelCommand =
                     new OdbcCommand("SET TRANSACTION ISOLATION LEVEL SNAPSHOT", dbConnection))
        {
            await setIsolationLevelCommand.ExecuteNonQueryAsync();
        }

        OdbcTransaction transaction = dbConnection.BeginTransaction();
        try
        {
            var selectQuery = "SELECT decAmount FROM BOS_Balance WITH (UPDLOCK) WHERE szAccountId = ?";
            OdbcCommand selectCommand = new OdbcCommand(selectQuery, dbConnection, transaction);
            selectCommand.Parameters.Add(new OdbcParameter("@szAccountId", accountId));
            decimal currentBalance = (decimal)(await selectCommand.ExecuteScalarAsync());

            decimal newBalance = currentBalance + Amount;

            string updateQuery = "UPDATE BOS_Balance SET decAmount = ? WHERE szAccountId = ?";
            using (OdbcCommand updateCommand = new OdbcCommand(updateQuery, dbConnection, transaction))
            {
                updateCommand.Parameters.Add(new OdbcParameter("@decAmount", newBalance));
                updateCommand.Parameters.Add(new OdbcParameter("@szAccountId", accountId));
                await updateCommand.ExecuteNonQueryAsync();
            }

            // Retrieve and increment the last number from BOS_Counter
            string counterQuery =
                "SELECT iLastNumber FROM BOS_Counter WITH (UPDLOCK) WHERE szCounterId = '001-COU'";
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
            DateTime adjustedDateTime = new DateTime(
                originalDateTime.Year,
                originalDateTime.Month,
                originalDateTime.Day,
                originalDateTime.Hour,
                originalDateTime.Minute,
                originalDateTime.Second
            );


            // Insert into BOS_history
            StringBuilder insertQuery =
                new StringBuilder(
                    "INSERT INTO BOS_History (szTransactionId, szAccountId, szCurrencyId, dtmTransaction, decAmount, szNote) VALUES ");
            List<OdbcParameter> parameters = new List<OdbcParameter>();

            insertQuery.Append($"(?, ?, ?, ?, ?, ?)");
            parameters.AddRange(new OdbcParameter[]
            {
                new OdbcParameter("@szTransactionId", szTransactionId),
                new OdbcParameter("@szAccountId", accountId),
                new OdbcParameter("@szCurrencyId", Currency.IDR.ToString()),
                new OdbcParameter("@dtmTransaction", OdbcType.DateTime) { Value = adjustedDateTime },
                new OdbcParameter("@decAmount", Amount),
                new OdbcParameter("@szNote", Note.SETOR.ToString())
            });

            OdbcCommand insertCommand = new OdbcCommand(insertQuery.ToString(), dbConnection, transaction);
            insertCommand.Parameters.AddRange(parameters.ToArray());
            await insertCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            await dbConnection.CloseAsync();
            return newBalance;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine("Error: " + ex.Message);
            throw;
        }
    }

    public async Task<decimal> Withdraw(string accountId, decimal amount)
    {
        await dbConnection.OpenAsync();
        // Set the transaction isolation level to snapshot
        await using (OdbcCommand setIsolationLevelCommand =
                     new OdbcCommand("SET TRANSACTION ISOLATION LEVEL SNAPSHOT", dbConnection))
        {
            await setIsolationLevelCommand.ExecuteNonQueryAsync();
        }

        OdbcTransaction transaction = dbConnection.BeginTransaction();
        try
        {
            string selectQuery = "SELECT decAmount FROM BOS_Balance WITH (UPDLOCK) WHERE szAccountId = ?";
            OdbcCommand selectCommand = new OdbcCommand(selectQuery, dbConnection, transaction);
            selectCommand.Parameters.Add(new OdbcParameter("@szAccountId", accountId));
            decimal currentBalance = (decimal)(await selectCommand.ExecuteScalarAsync());

            if (currentBalance < amount)
            {
                throw new Exception("Insufficient balance.");
            }

            decimal newBalance = currentBalance - amount;

            string updateQuery = "UPDATE BOS_Balance SET decAmount = ? WHERE szAccountId = ?";
            using (OdbcCommand updateCommand = new OdbcCommand(updateQuery, dbConnection, transaction))
            {
                updateCommand.Parameters.Add(new OdbcParameter("@decAmount", newBalance));
                updateCommand.Parameters.Add(new OdbcParameter("@szAccountId", accountId));
                await updateCommand.ExecuteNonQueryAsync();
            }

            // Retrieve and increment the last number from BOS_Counter
            string counterQuery =
                "SELECT iLastNumber FROM BOS_Counter WITH (UPDLOCK) WHERE szCounterId = '001-COU'";
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
            DateTime adjustedDateTime = new DateTime(
                originalDateTime.Year,
                originalDateTime.Month,
                originalDateTime.Day,
                originalDateTime.Hour,
                originalDateTime.Minute,
                originalDateTime.Second
            );


            // Insert into BOS_history
            StringBuilder insertQuery =
                new StringBuilder(
                    "INSERT INTO BOS_History (szTransactionId, szAccountId, szCurrencyId, dtmTransaction, decAmount, szNote) VALUES ");
            List<OdbcParameter> parameters = new List<OdbcParameter>();

            insertQuery.Append($"(?, ?, ?, ?, ?, ?)");
            parameters.AddRange(new OdbcParameter[]
            {
                new OdbcParameter("@szTransactionId", szTransactionId),
                new OdbcParameter("@szAccountId", accountId),
                new OdbcParameter("@szCurrencyId", Currency.IDR.ToString()),
                new OdbcParameter("@dtmTransaction", OdbcType.DateTime) { Value = adjustedDateTime },
                new OdbcParameter("@decAmount", -amount),
                new OdbcParameter("@szNote", Note.TARIK.ToString())
            });

            OdbcCommand insertCommand = new OdbcCommand(insertQuery.ToString(), dbConnection, transaction);
            insertCommand.Parameters.AddRange(parameters.ToArray());
            await insertCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            await dbConnection.CloseAsync();
            return newBalance;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine("Error: " + ex.Message);
            throw;
        }
    }

    public async Task<bool> Transfer(string accountId, To[] tos)
    {
        return await ExecuteWithRetryAsync(async dbConnection =>
        {
            var note = Note.TRANSFER.ToString();

            // Set the transaction isolation level to snapshot
            await using (OdbcCommand setIsolationLevelCommand =
                         new OdbcCommand("SET TRANSACTION ISOLATION LEVEL SNAPSHOT", dbConnection))
            {
                await setIsolationLevelCommand.ExecuteNonQueryAsync();
            }

            // Start a transaction
            await using (var transaction = dbConnection.BeginTransaction())
            {
                try
                {
                    var numberAccounts = tos.Length;

                    // Update the BOS_Balance table for the account that is transferring the money.
                    // Read and lock the current balance
                    var selectQuery =
                        "SELECT decAmount, szCurrencyId FROM BOS_Balance WHERE szAccountId = ?";
                    await using var selectCommand = new OdbcCommand(selectQuery, dbConnection, transaction);
                    selectCommand.Parameters.AddWithValue("@szAccountId", accountId);

                    decimal currentBalanceSGD = 0;
                    decimal currentBalanceIDR = 0;
                    decimal currentBalanceUSD = 0;
                    await using (var reader = await selectCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            if (reader.GetString(reader.GetOrdinal("szCurrencyId")) == Currency.SGD.ToString())
                            {
                                currentBalanceSGD = reader.GetDecimal(reader.GetOrdinal("decAmount"));
                            }
                            else if (reader.GetString(reader.GetOrdinal("szCurrencyId")) == Currency.IDR.ToString())
                            {
                                currentBalanceIDR = reader.GetDecimal(reader.GetOrdinal("decAmount"));
                            }
                            else
                            {
                                currentBalanceUSD = reader.GetDecimal(reader.GetOrdinal("decAmount"));
                            }
                        }
                    }

                    decimal totalAmountTransferedIDR = 0;
                    decimal totalAmountTransferedSGD = 0;
                    decimal totalAmountTransferedUSD = 0;
                    foreach (var to in tos)
                    {
                        if (to.Currency == Currency.IDR)
                        {
                            totalAmountTransferedIDR += to.Amount;
                        }
                        else if (to.Currency == Currency.USD)
                        {
                            totalAmountTransferedUSD += to.Amount;
                        }
                        else
                        {
                            totalAmountTransferedSGD += to.Amount;
                        }
                    }

                    if (currentBalanceIDR < totalAmountTransferedIDR || currentBalanceSGD < totalAmountTransferedSGD ||
                        currentBalanceUSD < totalAmountTransferedUSD)
                    {
                        throw new Exception("Insufficient balance.");
                    }

                    // Insert two records for each account in the BOS_History table.
                    var insertQuery =
                        new StringBuilder(
                            "INSERT INTO BOS_History (szTransactionId, szAccountId, szCurrencyId, dtmTransaction, decAmount, szNote) VALUES ");
                    var parameters = new List<OdbcParameter>();

                    for (var i = 0; i < numberAccounts; i++)
                    {
                        var receiverExists = await CheckIfRowExistsAsync(tos[i].AccountId, tos[i].Currency.ToString());

                        if (receiverExists)
                        {
                            // Update the balance for account that is transferring the money
                            var updateQuery =
                                "UPDATE BOS_Balance WITH (UPDLOCK) SET decAmount = decAmount - ? WHERE szAccountId = ? AND szCurrencyId = ?";
                            await using (var updateCommand =
                                         new OdbcCommand(updateQuery, dbConnection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@incrementAmount", tos[i].Amount);
                                updateCommand.Parameters.AddWithValue("@szAccountId", accountId);
                                updateCommand.Parameters.AddWithValue("@szCurrencyId", tos[i].Currency.ToString());
                                await updateCommand.ExecuteNonQueryAsync();
                            }

                            // Update the balance for the account that is receiving the money
                            var updateQueryReceiver =
                                "UPDATE BOS_Balance WITH (UPDLOCK) SET decAmount = decAmount + ? WHERE szAccountId = ? AND szCurrencyId = ?";
                            await using (var updateCommandReceiver =
                                         new OdbcCommand(updateQueryReceiver, dbConnection, transaction))
                            {
                                updateCommandReceiver.Parameters.AddWithValue("@incrementAmount", tos[i].Amount);
                                updateCommandReceiver.Parameters.AddWithValue("@szAccountId", tos[i].AccountId);
                                updateCommandReceiver.Parameters.AddWithValue("@szCurrencyId", tos[i].Currency.ToString());
                                await updateCommandReceiver.ExecuteNonQueryAsync();
                            }
                        }

                        // Retrieve and increment the last number from BOS_Counter
                        var counterQuery =
                            "SELECT iLastNumber FROM BOS_Counter WHERE szCounterId = '001-COU'";
                        await using (var counterCommand =
                                     new OdbcCommand(counterQuery, dbConnection, transaction))
                        {
                            long iLastNumber = (long)await counterCommand.ExecuteScalarAsync();

                            // Increment the last number
                            iLastNumber++;

                            // Update the BOS_Counter table with the new last number
                            var updateCounterQuery =
                                "UPDATE BOS_Counter SET iLastNumber = ? WHERE szCounterId = '001-COU'";
                            await using (var updateCounterCommand =
                                         new OdbcCommand(updateCounterQuery, dbConnection, transaction))
                            {
                                updateCounterCommand.Parameters.AddWithValue("@iLastNumber", iLastNumber);
                                await updateCounterCommand.ExecuteNonQueryAsync();
                            }

                            // Generate the new szTransactionId
                            DateTime originalDateTime = DateTime.Now;
                            string szTransactionId = $"{originalDateTime:yyyyMMdd}-00000.{iLastNumber:D5}";

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
                            parameters.AddRange(new[]
                            {
                                new OdbcParameter($"@szTransactionId{i}", szTransactionId),
                                new OdbcParameter($"@szAccountId{i}", accountId),
                                new OdbcParameter($"@szCurrencyId{i}", tos[i].Currency.ToString()),
                                new OdbcParameter($"@dtmTransaction{i}", OdbcType.DateTime)
                                    { Value = adjustedDateTime },
                                new OdbcParameter($"@decAmount{i}", -tos[i].Amount),
                                new OdbcParameter($"@szNote{i}", note)
                            });
                            parameters.AddRange(new[]
                            {
                                new OdbcParameter($"@szTransactionId{i}", szTransactionId),
                                new OdbcParameter($"@szAccountId{i}", tos[i].AccountId),
                                new OdbcParameter($"@szCurrencyId{i}", tos[i].Currency.ToString()),
                                new OdbcParameter($"@dtmTransaction{i}", OdbcType.DateTime)
                                    { Value = adjustedDateTime },
                                new OdbcParameter($"@decAmount{i}", tos[i].Amount),
                                new OdbcParameter($"@szNote{i}", note)
                            });
                        }
                    }

                    await using (var insertCommand =
                                 new OdbcCommand(insertQuery.ToString(), dbConnection, transaction))
                    {
                        insertCommand.Parameters.AddRange(parameters.ToArray());
                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    // Commit the transaction
                    transaction.Commit();
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    transaction.Rollback();
                    throw;
                }
            }

        }, dbConnection.ConnectionString);
    }

    public static async Task<T> ExecuteWithRetryAsync<T>(Func<OdbcConnection, Task<T>> operation,
        string connectionString, int maxRetryCount = 3, int delayMilliseconds = 1000)
    {
        int retryCount = 0;
        while (true)
        {
            await using var dbConnection = new OdbcConnection(connectionString);
            try
            {
                await dbConnection.OpenAsync();
                return await operation(dbConnection);
            }
            catch (OdbcException ex) when (ex.Errors.Cast<OdbcError>().Any(e => e.SQLState == "42000"))
            {
                if (++retryCount >= maxRetryCount)
                    throw;
                await Task.Delay(delayMilliseconds); // Wait before retrying
            }
            finally
            {
                await dbConnection.CloseAsync();
            }
        }
    }
    
    public async Task<List<Balance>> GetBalance(string accountId)
    {
        const string selectQuery = "SELECT decAmount, szAccountId, szCurrencyId FROM BOS_Balance WHERE szAccountId = ?";

        await dbConnection.OpenAsync();
        await using var selectCommand = new OdbcCommand(selectQuery, dbConnection);
        selectCommand.Parameters.AddWithValue("@szAccountId", accountId);

        await using var reader = await selectCommand.ExecuteReaderAsync();
        List<Balance> balances = new List<Balance>();
        while (await reader.ReadAsync())
        {
            Balance balance = new Balance
            {
                Amount = reader.GetDecimal(0),
                CurrencyId = reader.GetString(2),
                AccountId = reader.GetString(1)
            };
            balances.Add(balance);
        }

        await dbConnection.CloseAsync();

        return balances;
    }

    public async Task<bool> CheckIfRowExistsAsync(string accountId, string currency)
    {
        const string selectQuery = "SELECT 1 FROM BOS_Balance WHERE szAccountId = ? AND szCurrencyId = ?";

        await dbConnection.OpenAsync();
        await using var selectCommand = new OdbcCommand(selectQuery, dbConnection);
        selectCommand.Parameters.AddWithValue("@szAccountId", accountId);
        selectCommand.Parameters.AddWithValue("@szCurrencyId", currency);

        await using var reader = await selectCommand.ExecuteReaderAsync();
        var res = await reader.ReadAsync();
        await dbConnection.CloseAsync();
        return res;
    }
}