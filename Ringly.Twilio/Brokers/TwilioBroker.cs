using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using RESTFulSense.Clients;

namespace Ringly.Twilio.Brokers;

public partial class TwilioBroker : ITwilioBroker
{
    private const string FormUrlEncodedMediaType = "application/x-www-form-urlencoded";

    private readonly HttpClient httpClient;
    private readonly IRESTFulApiFactoryClient apiClient;
    private readonly string taskRouterBaseUrl;
    private readonly string workspaceSid;

    public TwilioBroker(IOptions<TwilioOptions> options)
    {
        TwilioOptions twilioOptions = options.Value;
        this.taskRouterBaseUrl = twilioOptions.TaskRouterBaseUrl.TrimEnd('/');
        this.workspaceSid = twilioOptions.WorkspaceSid;

        // Trailing slash required for relative URL combination to append rather than replace
        // the last path segment — see Ringly.Asterisk.Brokers.AsteriskBroker for the same
        // lesson learned the hard way (row #21).
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{twilioOptions.BaseUrl.TrimEnd('/')}/Accounts/{twilioOptions.AccountSid}/")
        };

        // Twilio supports HTTP Basic auth with the Account SID as username and Auth Token as
        // password (API keys are Twilio's recommended production alternative, not used here to
        // keep this consistent with the simple credential-pair pattern the rest of this
        // codebase uses, e.g. AsteriskOptions/SipTrunkConfig).
        string credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{twilioOptions.AccountSid}:{twilioOptions.AuthToken}"));

        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        this.apiClient = new RESTFulApiFactoryClient(this.httpClient);
    }

    private static readonly Func<string, ValueTask<string>> PassThroughDeserialization =
        content => ValueTask.FromResult(content ?? string.Empty);

    // Twilio's documented form-param convention is lowercase "true"/"false" — C#'s default
    // bool.ToString() produces "True"/"False", confirmed (via a local capture, not a live
    // account) to be what this code would otherwise send. Twilio's own parser may well be
    // case-insensitive, but there's no reason to rely on that when the fix is this small.
    private static string FormBool(bool value) => value ? "true" : "false";

    // Twilio's REST API takes application/x-www-form-urlencoded bodies, not JSON — confirmed
    // against Twilio's own docs (the "Create a Call" curl example uses --data-urlencode, and
    // the official OpenAPI spec's request bodies are form-encoded throughout). RESTFulSense's
    // typed content helpers JSON-serialize by default regardless of the mediaType string passed,
    // so the body is built as a real form-encoded string ourselves and passed through as-is.
    private static string BuildFormBody(IEnumerable<KeyValuePair<string, string?>> fields) =>
        string.Join(
            '&',
            fields
                .Where(field => field.Value is not null)
                .Select(field => $"{Uri.EscapeDataString(field.Key)}={Uri.EscapeDataString(field.Value!)}"));

    // Request body is form-encoded (built above); the response body is still normal JSON —
    // Twilio's API is form-in/JSON-out — so response deserialization uses RESTFulSense's usual
    // default (Newtonsoft.Json under the hood), only the request-side serialization is
    // overridden to a pass-through of the pre-built form string.
    private async ValueTask<T> PostFormAsync<T>(string relativeUrl, IEnumerable<KeyValuePair<string, string?>> fields) =>
        await this.apiClient.SendHttpRequestAsync<string, T>(
            method: "POST",
            relativeUrl: relativeUrl,
            content: BuildFormBody(fields),
            mediaType: FormUrlEncodedMediaType,
            serializationFunction: body => ValueTask.FromResult(body));

    private async ValueTask PostFormAsync(string relativeUrl, IEnumerable<KeyValuePair<string, string?>> fields) =>
        await this.apiClient.SendHttpRequestAsync<string, string>(
            method: "POST",
            relativeUrl: relativeUrl,
            content: BuildFormBody(fields),
            mediaType: FormUrlEncodedMediaType,
            serializationFunction: body => ValueTask.FromResult(body),
            deserializationFunction: PassThroughDeserialization);

    private async ValueTask DeleteAsync(string relativeUrl) =>
        await this.apiClient.SendHttpRequestAsync(
            method: "DELETE",
            relativeUrl: relativeUrl,
            cancellationToken: CancellationToken.None,
            deserailizationFunction: PassThroughDeserialization);
}
