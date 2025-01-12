using NetProject.Domain.DataTransferObjects;
using NetProject.Domain.Interfaces;
using NetProject.Domain.TransactionAggregates;

namespace NetProject.Usecase;

public interface IBargainUsecase
{
    public Task<IEnumerable<History>> GetHistories(string accountId, DateTime startDate, DateTime endDate);
    public Task<GeneralResponse> Deposit(string accountId, decimal amount);
    public Task<GeneralResponse> Withdraw(string accountId, decimal amount);
    public Task<List<GeneralResponse>> Transfer(string accountId, To[] tos);
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

    public async Task<GeneralResponse> Deposit(string accountId, decimal amount)
    {
        try
        {
            var res = await bargainRepository.Deposit(accountId, amount);
            var response = new GeneralResponse()
            {
                AccountId = accountId,
                Amount = res
            };
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<GeneralResponse> Withdraw(string accountId, decimal amount)
    {
        try
        {
            var res = await bargainRepository.Withdraw(accountId, amount);
            var response = new GeneralResponse()
            {
                AccountId = accountId,
                Amount = res
            };
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<GeneralResponse>> Transfer(string accountId, To[] tos)
    {
        var isOk = await bargainRepository.Transfer(accountId, tos);
        var res = await bargainRepository.GetBalance(accountId);
        // var res = new List<Balance>();
        var generalResponses = new List<GeneralResponse>();
        foreach (var to in res)
        {
            var response = new GeneralResponse()
            {
                AccountId = to.AccountId,
                CurrencyId = to.CurrencyId,
                Amount = to.Amount
            };
            generalResponses.Add(response);
        }
        return generalResponses;
    }
}