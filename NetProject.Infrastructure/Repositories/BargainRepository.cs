using System.Data.Odbc;
using NetProject.Domain.Interfaces;
using NetProject.Domain.TransactionAggregates;

namespace NetProject.Infrastructure.Repositories;

public class BargainRepository (OdbcConnection dbConnection) : IBargainRepository
{
    public async Task<IEnumerable<History>> GetAll(string accountId, DateTime startDate, DateTime endDate)
    {
        try
        {
            await dbConnection.OpenAsync();
            var command =
                new OdbcCommand(@"SELECT szTransactionId, szAccountId, szCurrencyId, dtmTransaction, decAmount, szNote FROM dbo.[BOS_History]
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
    
    public void Deposit()
    {
    }
    
    public void Withdraw()
    {
    }
    
    public void Transfer()
    {
    }
}