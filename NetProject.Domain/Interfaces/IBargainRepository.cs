using NetProject.Domain.TransactionAggregates;

namespace NetProject.Domain.Interfaces;

public interface IBargainRepository
{
    public Task<IEnumerable<History>> GetAll(string accountId, DateTime startDate, DateTime endDate);
}