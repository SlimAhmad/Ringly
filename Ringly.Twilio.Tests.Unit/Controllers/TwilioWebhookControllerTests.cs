using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Controllers;
using Ringly.Twilio.Controllers.Models;
using Ringly.Twilio.Events;
using Ringly.Twilio.Security;

namespace Ringly.Twilio.Tests.Unit.Controllers;

public class TwilioWebhookControllerTests
{
    private readonly Mock<ITwilioSignatureValidator> signatureValidatorMock;
    private readonly Mock<ITwilioCallEventStream> callEventStreamMock;
    private readonly TwilioWebhookController controller;

    public TwilioWebhookControllerTests()
    {
        this.signatureValidatorMock = new Mock<ITwilioSignatureValidator>();
        this.callEventStreamMock = new Mock<ITwilioCallEventStream>();

        var options = Options.Create(new TwilioWebhookOptions { AuthToken = "some-auth-token" });

        this.controller = new TwilioWebhookController(
            this.signatureValidatorMock.Object,
            this.callEventStreamMock.Object,
            options)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            }
        };
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com");
        context.Request.Path = "/webhooks/twilio/voice";
        context.Request.Headers["X-Twilio-Signature"] = "some-signature";

        context.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["CallSid"] = "CA123",
            ["CallStatus"] = "ringing"
        });

        return context;
    }

    [Fact]
    public void ShouldPublishEventAndReturnOkWhenSignatureValid()
    {
        // given
        var request = new TwilioVoiceWebhookRequest { CallSid = "CA123", CallStatus = "ringing" };

        this.signatureValidatorMock.Setup(validator =>
            validator.IsValid(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                "some-signature",
                "some-auth-token"))
                    .Returns(true);

        // when
        IActionResult result = this.controller.ReceiveVoiceStatusCallback(request);

        // then
        result.Should().BeOfType<OkResult>();

        this.callEventStreamMock.Verify(stream =>
            stream.Publish(It.Is<CallEvent>(callEvent =>
                callEvent.ChannelId == "CA123" && callEvent.EventType == "ringing")),
                    Times.Once);
    }

    [Fact]
    public void ShouldReturnForbiddenStatusAndNotPublishWhenSignatureInvalid()
    {
        // given
        var request = new TwilioVoiceWebhookRequest { CallSid = "CA123", CallStatus = "ringing" };

        this.signatureValidatorMock.Setup(validator =>
            validator.IsValid(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                    .Returns(false);

        // when
        IActionResult result = this.controller.ReceiveVoiceStatusCallback(request);

        // then
        result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        this.callEventStreamMock.Verify(stream =>
            stream.Publish(It.IsAny<CallEvent>()),
                Times.Never);
    }
}
