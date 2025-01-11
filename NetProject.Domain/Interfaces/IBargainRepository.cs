using NetProject.Domain.TransactionAggregates;

namespace NetProject.Domain.Interfaces;

public interface IBargainRepository
{
    public Task<IEnumerable<History>> GetAll(string accountId, DateTime startDate, DateTime endDate);
    public Task<decimal> Deposit(string accountId, decimal amount);
    public Task<string> Transfer(string accountId, string[] accountIds, decimal amount);
}