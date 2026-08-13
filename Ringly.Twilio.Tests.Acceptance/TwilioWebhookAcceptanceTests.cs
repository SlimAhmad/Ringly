using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Ringly.Abstractions.Models;

namespace Ringly.Twilio.Tests.Acceptance;

// Exercises the real HTTP pipeline: routing, [FromForm] binding, and the real
// TwilioSignatureValidator — not a mock — so this proves the wiring in TwilioWebhookController
// actually rejects unsigned/incorrectly-signed requests and accepts correctly-signed ones,
// against a real TestServer instance rather than calling the controller method directly (already
// covered by Ringly.Twilio.Tests.Unit).
public class TwilioWebhookAcceptanceTests : IClassFixture<TwilioWebhookAcceptanceTestFactory>
{
    private const string SignatureHeaderName = "X-Twilio-Signature";
    private readonly TwilioWebhookAcceptanceTestFactory factory;

    public TwilioWebhookAcceptanceTests(TwilioWebhookAcceptanceTestFactory factory) =>
        this.factory = factory;

    // Twilio's own documented algorithm (same one TwilioSignatureValidator implements, verified
    // in row #32 against a real published Twilio test vector): url + sorted(key+value pairs),
    // HMAC-SHA1 keyed with the Auth Token, base64-encoded.
    private static string ComputeSignature(
        string url, IReadOnlyDictionary<string, string> formParameters, string authToken)
    {
        var messageBuilder = new StringBuilder(url);

        foreach (string key in formParameters.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            messageBuilder.Append(key).Append(formParameters[key]);
        }

        byte[] keyBytes = Encoding.UTF8.GetBytes(authToken);
        byte[] messageBytes = Encoding.UTF8.GetBytes(messageBuilder.ToString());
        byte[] hash = HMACSHA1.HashData(keyBytes, messageBytes);

        return Convert.ToBase64String(hash);
    }

    [Fact]
    public async Task ShouldReturnForbiddenWhenSignatureHeaderIsMissingAsync()
    {
        // given
        using HttpClient client = this.factory.CreateClient();

        var formFields = new Dictionary<string, string>
        {
            ["CallSid"] = "CA_missing_signature",
            ["CallStatus"] = "ringing"
        };

        using var content = new FormUrlEncodedContent(formFields);

        // when
        HttpResponseMessage response = await client.PostAsync("webhooks/twilio/voice", content);

        // then
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldReturnForbiddenWhenSignatureIsIncorrectAsync()
    {
        // given
        using HttpClient client = this.factory.CreateClient();

        var formFields = new Dictionary<string, string>
        {
            ["CallSid"] = "CA_wrong_signature",
            ["CallStatus"] = "ringing"
        };

        using var content = new FormUrlEncodedContent(formFields);
        content.Headers.Add(SignatureHeaderName, "clearly-not-a-real-signature");

        // when
        HttpResponseMessage response = await client.PostAsync("webhooks/twilio/voice", content);

        // then
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldReturnOkAndPublishEventWhenSignatureIsValidAsync()
    {
        // given
        using HttpClient client = this.factory.CreateClient();
        string url = client.BaseAddress + "webhooks/twilio/voice";

        var formFields = new Dictionary<string, string>
        {
            ["CallSid"] = "CA_valid_signature",
            ["CallStatus"] = "completed"
        };

        string signature = ComputeSignature(url, formFields, TwilioWebhookAcceptanceTestFactory.AuthToken);

        var receivedEvents = new List<CallEvent>();
        using IDisposable subscription = this.factory.CallEventStream.Events.Subscribe(receivedEvents.Add);

        using var content = new FormUrlEncodedContent(formFields);
        content.Headers.Remove("Content-Type");
        content.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
        content.Headers.Add(SignatureHeaderName, signature);

        // when
        HttpResponseMessage response = await client.PostAsync("webhooks/twilio/voice", content);

        // then
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        receivedEvents.Should().ContainSingle(callEvent =>
            callEvent.ChannelId == "CA_valid_signature" && callEvent.EventType == "completed");
    }
}
