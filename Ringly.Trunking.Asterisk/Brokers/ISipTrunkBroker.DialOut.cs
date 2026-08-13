using Ringly.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Brokers;

public partial interface ISipTrunkBroker
{
    ValueTask<Channel> DialOutAsync(string phoneNumber, string trunkName);
}
