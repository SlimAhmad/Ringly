using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Controllers.Models;
using Ringly.Twilio.Events;
using Ringly.Twilio.Security;

namespace Ringly.Twilio.Controllers;

// §9 "Event delivery" — Twilio POSTs inbound webhooks to this public endpoint (status
// callbacks: ringing/answered/completed/etc.) instead of Asterisk's outbound ARI WebSocket.
// Every request must have its X-Twilio-Signature validated before the payload is trusted —
// an unvalidated webhook endpoint lets anyone POST fake call events at this backend.
[ApiController]
[Route("webhooks/twilio")]
public class TwilioWebhookController : ControllerBase
{
    private const string SignatureHeaderName = "X-Twilio-Signature";

    private readonly ITwilioSignatureValidator signatureValidator;
    private readonly ITwilioCallEventStream callEventStream;
    private readonly TwilioWebhookOptions options;

    public TwilioWebhookController(
        ITwilioSignatureValidator signatureValidator,
        ITwilioCallEventStream callEventStream,
        IOptions<TwilioWebhookOptions> options)
    {
        this.signatureValidator = signatureValidator;
        this.callEventStream = callEventStream;
        this.options = options.Value;
    }

    [HttpPost("voice")]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult ReceiveVoiceStatusCallback([FromForm] TwilioVoiceWebhookRequest request)
    {
        string signatureHeader = this.Request.Headers[SignatureHeaderName].ToString();
        string url = this.BuildSignedUrl();

        var formParameters = this.Request.Form.ToDictionary(
            field => field.Key,
            field => field.Value.ToString());

        bool isValid = this.signatureValidator.IsValid(
            url, formParameters, signatureHeader, this.options.AuthToken);

        if (!isValid)
        {
            // Not this.Forbid() — ForbidResult requires the hosting app to have an
            // authentication scheme registered (it throws InvalidOperationException otherwise,
            // confirmed via row #36's acceptance test), which this controller has no business
            // requiring just to reject a bad webhook signature. A direct 403 has no such
            // dependency.
            return this.StatusCode(StatusCodes.Status403Forbidden);
        }

        this.callEventStream.Publish(new CallEvent
        {
            EventType = request.CallStatus,
            ChannelId = request.CallSid,
            OccurredDate = DateTimeOffset.UtcNow
        });

        return this.Ok();
    }

    // See TwilioWebhookOptions.PublicBaseUrlOverride's comment — reconstructing from the
    // request as ASP.NET Core sees it is wrong when TLS terminates upstream of this app.
    private string BuildSignedUrl() =>
        string.IsNullOrEmpty(this.options.PublicBaseUrlOverride)
            ? this.Request.GetEncodedUrl()
            : this.options.PublicBaseUrlOverride.TrimEnd('/') + this.Request.Path + this.Request.QueryString;
}
