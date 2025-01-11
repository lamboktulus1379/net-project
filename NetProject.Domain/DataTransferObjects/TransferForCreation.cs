namespace NetProject.Domain.DataTransferObjects;

public class TransferForCreation
{
    public string From { get; set; }
    public string[] To { get; set; }
    public decimal Amount { get; set; }
}