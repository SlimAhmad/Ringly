using Ringly.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Services.Foundations.Trunks;

public interface ISipTrunkFoundationService
{
    ValueTask<Channel> DialOutAsync(string phoneNumber, string trunkName);
}
