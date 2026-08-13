using Ringly.Abstractions.Models;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Brokers;

namespace Ringly.Trunking.Asterisk.Services.Foundations.Trunks;

public partial class SipTrunkFoundationService : ISipTrunkFoundationService
{
    private readonly ISipTrunkBroker sipTrunkBroker;
    private readonly ILoggingBroker loggingBroker;

    public SipTrunkFoundationService(
        ISipTrunkBroker sipTrunkBroker,
        ILoggingBroker loggingBroker)
    {
        this.sipTrunkBroker = sipTrunkBroker;
        this.loggingBroker = loggingBroker;
    }

    // §8.4 — provider-side controls are the primary, non-negotiable defense; this is the
    // app-side layer, checked before ever attempting the dial.
    public ValueTask<Channel> DialOutAsync(string phoneNumber, string trunkName) =>
    TryCatch(async () =>
    {
        ValidateDialOutRequest(phoneNumber, trunkName);

        SipTrunkConfig config = await this.sipTrunkBroker.RetrieveTrunkConfigAsync(trunkName);
        ValidateDestinationAllowed(phoneNumber, config);

        TrunkCallLimitStatus status = await this.sipTrunkBroker.RetrieveSpendStatusAsync(trunkName);
        ValidateWithinLimits(status, config, trunkName);

        return await this.sipTrunkBroker.DialOutAsync(phoneNumber, trunkName);
    });
}
