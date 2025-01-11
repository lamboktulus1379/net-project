using System.Text.Json.Serialization;

namespace NetProject.Domain.TransactionAggregates;

public enum Note
{
    SETOR,
    TARIK,
    TRANSFER
}

public enum Currency
{
    IDR,
    SGD
}

public class History
{
    [JsonPropertyName("szTransactionId")]
    public string TransactionId { get; set; }
    [JsonPropertyName("szAccountId")]
    public string AccountId { get; set; }
    [JsonPropertyName("szCurrencyId")]
    public string CurrencyId { get; set; }
    [JsonPropertyName("dtmTransaction")]
    public DateTime DateTimeTransaction { get; set; }
    [JsonPropertyName("decAmount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("note")]
    public string Note { get; set; }
}