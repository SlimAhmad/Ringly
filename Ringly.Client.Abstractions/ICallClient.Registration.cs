using Ringly.Client.Abstractions.Models;

namespace Ringly.Client.Abstractions;

public partial interface ICallClient
{
    ValueTask RegisterAsync(SipCredentials credentials);
}
