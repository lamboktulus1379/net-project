using NetProject.Domain.DataTransferObjects;
using NetProject.Domain.TransactionAggregates;

namespace NetProject.Domain.Interfaces;

public interface IBargainRepository
{
    public Task<IEnumerable<History>> GetAll(string accountId, DateTime startDate, DateTime endDate);
    public Task<decimal> Deposit(string accountId, decimal amount);
    public Task<decimal> Withdraw(string accountId, decimal amount);
    public Task<bool> Transfer(string accountId, To[] tos);
    public Task<List<Balance>> GetBalance(string accountId);
}