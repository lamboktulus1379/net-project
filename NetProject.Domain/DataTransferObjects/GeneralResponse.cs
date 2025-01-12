namespace NetProject.Domain.DataTransferObjects;

public class GeneralResponse
{
    public string AccountId { get; set; }
    public string CurrencyId { get; set; }
    public decimal Amount { get; set; }
}