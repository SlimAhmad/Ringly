namespace Ringly.Trunking.Abstractions.Models;

public class TrunkCallLimitStatus
{
    public string TrunkName { get; set; } = string.Empty;
    public decimal SpendTodayUsd { get; set; }
    public int ActiveCallCount { get; set; }
    public bool IsOverLimit { get; set; }
}
