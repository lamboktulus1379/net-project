namespace NetProject.Domain.TransactionAggregates;

public class Balance
{
    public string AccountId { get; set; }
    public string CurrencyId { get; set; }
    public decimal Amount { get; set; }
}