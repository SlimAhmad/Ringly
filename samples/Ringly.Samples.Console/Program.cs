using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Twilio.Services.Foundations.Queues;
using Ringly.Samples.Console;
using Ringly.Twilio.Brokers;
using Ringly.Twilio.Services.Foundations.CallSessions;

// Twilio backend demo — the counterpart to Ringly.Samples.WebApi's Asterisk backend, showing
// both server-side pluggable backends behind the same ICallProvider/ICallCenterProvider
// interfaces (see docs/call-provider.md, docs/call-center.md).
//
// Usage:
//   dotnet run -- <partyAPhoneNumber> <partyBPhoneNumber> [--yes]
//
// Real Twilio credentials are read from environment variables — never hardcoded, never
// committed:
//   RINGLY_TWILIO_ACCOUNT_SID
//   RINGLY_TWILIO_AUTH_TOKEN
//   RINGLY_TWILIO_DEFAULT_CALLER_ID   (a number your Twilio account owns/verifies)
//   RINGLY_TWILIO_WORKSPACE_SID       (a TaskRouter Workspace SID)

string? accountSid = Environment.GetEnvironmentVariable("RINGLY_TWILIO_ACCOUNT_SID");
string? authToken = Environment.GetEnvironmentVariable("RINGLY_TWILIO_AUTH_TOKEN");
string? defaultCallerId = Environment.GetEnvironmentVariable("RINGLY_TWILIO_DEFAULT_CALLER_ID");
string? workspaceSid = Environment.GetEnvironmentVariable("RINGLY_TWILIO_WORKSPACE_SID");

if (string.IsNullOrWhiteSpace(accountSid) ||
    string.IsNullOrWhiteSpace(authToken) ||
    string.IsNullOrWhiteSpace(defaultCallerId) ||
    string.IsNullOrWhiteSpace(workspaceSid))
{
    Console.WriteLine("Missing Twilio credentials. Set these environment variables first:");
    Console.WriteLine("  RINGLY_TWILIO_ACCOUNT_SID");
    Console.WriteLine("  RINGLY_TWILIO_AUTH_TOKEN");
    Console.WriteLine("  RINGLY_TWILIO_DEFAULT_CALLER_ID");
    Console.WriteLine("  RINGLY_TWILIO_WORKSPACE_SID");
    return 1;
}

bool confirmed = args.Contains("--yes");
string[] positionalArgs = args.Where(a => a != "--yes").ToArray();

if (positionalArgs.Length < 2)
{
    Console.WriteLine("Usage: dotnet run -- <partyAPhoneNumber> <partyBPhoneNumber> [--yes]");
    return 1;
}

string partyAExtension = positionalArgs[0];
string partyBExtension = positionalArgs[1];

var services = new ServiceCollection();

services.Configure<TwilioOptions>(options =>
{
    options.AccountSid = accountSid;
    options.AuthToken = authToken;
    options.DefaultCallerId = defaultCallerId;
    options.WorkspaceSid = workspaceSid;
});

services.AddSingleton<ILoggingBroker, ConsoleLoggingBroker>();
services.AddSingleton<ITwilioBroker, TwilioBroker>();
services.AddSingleton<InMemorySipCredentialsStore>();
services.AddSingleton<ISipCredentialsStore>(sp => sp.GetRequiredService<InMemorySipCredentialsStore>());
services.AddScoped<ICallProvider, TwilioCallProvider>();
services.AddScoped<ICallCenterProvider, TwilioCallCenterProvider>();

using ServiceProvider provider = services.BuildServiceProvider();
using IServiceScope scope = provider.CreateScope();

var callProvider = scope.ServiceProvider.GetRequiredService<ICallProvider>();
var callCenterProvider = scope.ServiceProvider.GetRequiredService<ICallCenterProvider>();

// This actually dials both numbers via Twilio — a real phone call, at the account's cost.
// Never fire it without explicit confirmation.
Console.WriteLine($"About to dial {partyAExtension} and {partyBExtension} into a Twilio conference.");
Console.WriteLine("This places a REAL call and will be billed by Twilio.");

if (!confirmed)
{
    Console.Write("Continue? [y/N] ");
    string? answer = Console.ReadLine();
    confirmed = string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase);
}

if (!confirmed)
{
    Console.WriteLine("Not confirmed — exiting without placing a call.");
    return 0;
}

HoldingBridge queue = await callCenterProvider.CreateQueueAsync(new QueueConfig { Name = "support" });
Console.WriteLine($"Created TaskRouter queue '{queue.QueueName}' ({queue.BridgeId}).");

CallSession session = await callProvider.StartCallSessionAsync(
    new CallParticipant { SipExtension = partyAExtension },
    new CallParticipant { SipExtension = partyBExtension });

Console.WriteLine($"Call session {session.CallSessionId} started — conference '{session.BridgeId}'.");
return 0;
