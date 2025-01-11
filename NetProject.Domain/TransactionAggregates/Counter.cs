using System.Text.Json.Serialization;

namespace NetProject.Domain.TransactionAggregates;

public class Counter
{
    [JsonPropertyName("szCounterId")]
    public string CounterId { get; set; }
    [JsonPropertyName("iLastNumber")]
    public long LastNumber { get; set; }
}