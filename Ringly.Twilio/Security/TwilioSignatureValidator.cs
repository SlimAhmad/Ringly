using System.Security.Cryptography;
using System.Text;

namespace Ringly.Twilio.Security;

// Implements Twilio's documented X-Twilio-Signature validation algorithm exactly as specified
// (confirmed against Twilio's webhook security docs and cross-checked against the official
// twilio-csharp/twilio-python SDK sources, not guessed):
//   1. Start with the full URL Twilio called (exact string, including query string).
//   2. Append each POST parameter's key immediately followed by its value, no separators
//      between key/value or between pairs, iterating keys in ascending ordinal sort order.
//   3. HMAC-SHA1 that string using the Twilio Auth Token (raw bytes, UTF8) as the key.
//   4. Base64-encode the hash and compare it to the X-Twilio-Signature header value.
// This only covers the application/x-www-form-urlencoded case (standard Voice/Messaging
// webhooks) — Twilio's JSON-body webhooks (an opt-in feature for other products) use a
// different bodySHA256-based scheme this class does not implement.
public class TwilioSignatureValidator : ITwilioSignatureValidator
{
    public bool IsValid(
        string url,
        IReadOnlyDictionary<string, string> formParameters,
        string signatureHeader,
        string authToken)
    {
        if (string.IsNullOrEmpty(signatureHeader))
        {
            return false;
        }

        var messageBuilder = new StringBuilder(url);

        foreach (string key in formParameters.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            messageBuilder.Append(key).Append(formParameters[key]);
        }

        byte[] keyBytes = Encoding.UTF8.GetBytes(authToken);
        byte[] messageBytes = Encoding.UTF8.GetBytes(messageBuilder.ToString());
        byte[] hash = HMACSHA1.HashData(keyBytes, messageBytes);
        string computedSignature = Convert.ToBase64String(hash);

        byte[] computedBytes = Encoding.UTF8.GetBytes(computedSignature);
        byte[] receivedBytes = Encoding.UTF8.GetBytes(signatureHeader);

        return computedBytes.Length == receivedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(computedBytes, receivedBytes);
    }
}
