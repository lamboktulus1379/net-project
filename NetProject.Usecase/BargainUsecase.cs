using NetProject.Domain.Interfaces;
using NetProject.Domain.TransactionAggregates;

namespace NetProject.Usecase;

public interface IBargainUsecase
{
    public Task<IEnumerable<History>> GetHistories(string accountId, DateTime startDate, DateTime endDate);
    public Task<string> Transfer(string accountId, string[] accountIds, decimal amount);
}

public class BargainUsecase(IBargainRepository bargainRepository) : IBargainUsecase
{
    public async Task<IEnumerable<History>> GetHistories(string accountId, DateTime startDate, DateTime endDate)
    {
        try
        {
            return await bargainRepository.GetAll(accountId, startDate, endDate);
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

    public async Task<string> Transfer(string accountId, string[] accountIds, decimal amount)
    {
        var res = await bargainRepository.Transfer(accountId, accountIds, amount);
        return res;
    }
}