using FluentAssertions;
using Ringly.Twilio.Security;

namespace Ringly.Twilio.Tests.Unit.Security;

// Test vector taken verbatim from Twilio's own official test suite
// (twilio/twilio-python tests/unit/test_request_validator.py, ValidationTest.setUp) — a real,
// published ground truth, not invented. Fabricating a test vector for a signature algorithm
// would only prove the test agrees with the implementation, not that either is actually
// correct against Twilio's real behavior — this is why an external, independently-published
// vector matters here specifically.
public class TwilioSignatureValidatorTests
{
    private const string AuthToken = "12345";
    private const string Url = "https://mycompany.com/myapp.php?foo=1&bar=2";
    private const string ExpectedSignature = "RSOYDt4T1cUTdK1PDd93/VVr8B8=";

    private static readonly IReadOnlyDictionary<string, string> Parameters = new Dictionary<string, string>
    {
        ["CallSid"] = "CA1234567890ABCDE",
        ["Digits"] = "1234",
        ["From"] = "+14158675309",
        ["To"] = "+18005551212",
        ["Caller"] = "+14158675309"
    };

    private readonly TwilioSignatureValidator validator = new();

    [Fact]
    public void ShouldValidateKnownGoodSignature()
    {
        // when
        bool isValid = this.validator.IsValid(Url, Parameters, ExpectedSignature, AuthToken);

        // then
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldRejectTamperedParameter()
    {
        // given
        var tamperedParameters = new Dictionary<string, string>(Parameters)
        {
            ["To"] = "+18005559999"
        };

        // when
        bool isValid = this.validator.IsValid(Url, tamperedParameters, ExpectedSignature, AuthToken);

        // then
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ShouldRejectWrongAuthToken()
    {
        // when
        bool isValid = this.validator.IsValid(Url, Parameters, ExpectedSignature, "wrong-token");

        // then
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ShouldRejectTamperedUrl()
    {
        // when
        bool isValid = this.validator.IsValid(
            "https://mycompany.com/different-path", Parameters, ExpectedSignature, AuthToken);

        // then
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShouldRejectMissingSignatureHeader(string? missingSignature)
    {
        // when
        bool isValid = this.validator.IsValid(Url, Parameters, missingSignature!, AuthToken);

        // then
        isValid.Should().BeFalse();
    }
}
