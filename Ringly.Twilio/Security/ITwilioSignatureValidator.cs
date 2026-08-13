namespace Ringly.Twilio.Security;

public interface ITwilioSignatureValidator
{
    bool IsValid(string url, IReadOnlyDictionary<string, string> formParameters, string signatureHeader, string authToken);
}
