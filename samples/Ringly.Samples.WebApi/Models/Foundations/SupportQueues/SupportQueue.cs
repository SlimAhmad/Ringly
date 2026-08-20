namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues;

public class SupportQueue
{
    public Guid Id { get; set; }
    public string QueueName { get; set; } = string.Empty;
    public string BridgeId { get; set; } = string.Empty;
    public string MusicOnHoldClass { get; set; } = string.Empty;
}
