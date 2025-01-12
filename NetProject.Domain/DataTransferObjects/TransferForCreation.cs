using NetProject.Domain.TransactionAggregates;

namespace NetProject.Domain.DataTransferObjects;

public class TransferForCreation
{
    public string From { get; set; }
    public ToCreation[] Tos { get; set; }
}

public class ToCreation
{
    public string AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}

public class To
{
    public string AccountId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
}