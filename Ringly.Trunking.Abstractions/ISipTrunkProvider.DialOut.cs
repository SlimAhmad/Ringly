using Ringly.Abstractions.Models;

namespace Ringly.Trunking.Abstractions;

public partial interface ISipTrunkProvider
{
    ValueTask<Channel> DialOutAsync(string phoneNumber, string trunkName);
}
