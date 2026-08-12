using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string BridgesRelativeUrl = "bridges";

    public async ValueTask<Bridge> InsertBridgeAsync(string bridgeType) =>
        await this.PostAsync<Bridge>($"{BridgesRelativeUrl}?type={Uri.EscapeDataString(bridgeType)}");
}
