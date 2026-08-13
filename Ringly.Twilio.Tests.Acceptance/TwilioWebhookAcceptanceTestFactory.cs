using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ringly.Twilio.Controllers;
using Ringly.Twilio.Events;
using Ringly.Twilio.Security;

namespace Ringly.Twilio.Tests.Acceptance;

// A minimal real ASP.NET Core host exercising TwilioWebhookController through the actual HTTP
// pipeline (routing, model binding, [Consumes]) with the real TwilioSignatureValidator wired in —
// not mocked, since row #36's whole point is verifying the real signature-checking integration,
// not the controller's mapping logic (already unit-tested in Ringly.Twilio.Tests.Unit). Twilio
// itself — the actual caller of this endpoint — is the external resource being emulated, per this
// package's acceptance-testing convention; no live Twilio account is needed.
//
// TestServer is used directly rather than WebApplicationFactory<T> — the latter requires T to
// come from an assembly with an entry point (Program.cs), but Ringly.Twilio is a plain class
// library with no host of its own (the consuming app's host wires the controller in, same as
// row #32's design).
public sealed class TwilioWebhookAcceptanceTestFactory : IDisposable
{
    public const string AuthToken = "acceptance-test-auth-token";

    private readonly IHost host;

    public TwilioWebhookAcceptanceTestFactory()
    {
        this.host = new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();

                    services
                        .AddControllers()
                        .AddApplicationPart(typeof(TwilioWebhookController).Assembly);

                    services.AddSingleton<ITwilioSignatureValidator, TwilioSignatureValidator>();
                    services.AddSingleton<ITwilioCallEventStream>(this.CallEventStream);

                    services.Configure<TwilioWebhookOptions>(options =>
                        options.AuthToken = AuthToken);
                })
                .Configure(app => app
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapControllers())))
            .Start();
    }

    public ITwilioCallEventStream CallEventStream { get; } = new TwilioCallEventStream();

    public HttpClient CreateClient() =>
        this.host.GetTestClient();

    public void Dispose() =>
        this.host.Dispose();
}
