using NetProject.Domain.Interfaces;
using NetProject.Domain.TransactionAggregates;

namespace NetProject.Usecase;

public interface IBargainUsecase
{
    public Task<IEnumerable<History>> GetHistories(string accountId, DateTime startDate, DateTime endDate);
    public Task<decimal> Deposit(string accountId, decimal amount);
    public Task<decimal> Withdraw(string accountId, decimal amount);
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

    public async Task<decimal> Deposit(string accountId, decimal amount)
    {
        try
        {
            var res = await bargainRepository.Deposit(accountId, amount);
            return res;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<decimal> Withdraw(string accountId, decimal amount)
    {
        try
        {
            var res = await bargainRepository.Withdraw(accountId, amount);
            return res;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<string> Transfer(string accountId, string[] accountIds, decimal amount)
    {
        var res = await bargainRepository.Transfer(accountId, accountIds, amount);
        return res;
    }
}