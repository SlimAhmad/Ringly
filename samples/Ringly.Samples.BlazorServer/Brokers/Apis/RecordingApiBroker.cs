using RESTFulSense.Clients;
using Ringly.Samples.BlazorServer.Models.Recordings;

namespace Ringly.Samples.BlazorServer.Brokers.Apis;

public class RecordingApiBroker : IRecordingApiBroker
{
    private readonly IRESTFulApiFactoryClient apiClient;

    public RecordingApiBroker(IConfiguration configuration)
    {
        string baseUrl = configuration.GetValue<string>("WebApiClient:BaseUrl") ?? "http://localhost:5000";

        var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        this.apiClient = new RESTFulApiFactoryClient(httpClient);
    }

    public async ValueTask<IReadOnlyList<RecordingRow>> GetRecordingsAsync() =>
        await this.apiClient.GetContentAsync<List<RecordingRow>>(relativeUrl: "api/recordings");

    // The blobUrl already on each RecordingRow isn't directly playable — the container is
    // private (confirmed live: a plain GET on it returns 403 AuthorizationFailure) — this asks
    // RecordingsController for a real, time-limited signed download URL instead.
    public async ValueTask<Uri> GetAccessUrlAsync(string recordingName) =>
        await this.apiClient.GetContentAsync<Uri>(
            relativeUrl: $"api/recordings/{Uri.EscapeDataString(recordingName)}/access-url");

    public async ValueTask PostRecordingAsync(string bridgeId, string recordingName, string format) =>
        await this.apiClient.PostContentAsync<object, object>(
            relativeUrl: "api/recordings",
            content: new { bridgeId, recordingName, format },
            mediaType: "application/json");

    public async ValueTask PostPauseAsync(string recordingName) =>
        await this.PostActionAsync(recordingName, "pause");

    public async ValueTask PostUnpauseAsync(string recordingName) =>
        await this.PostActionAsync(recordingName, "unpause");

    public async ValueTask PostStopAsync(string recordingName) =>
        await this.PostActionAsync(recordingName, "stop");

    public async ValueTask PostCancelAsync(string recordingName) =>
        await this.PostActionAsync(recordingName, "cancel");

    public async ValueTask DeleteRecordingAsync(string recordingName) =>
        await this.apiClient.DeleteContentAsync($"api/recordings/{Uri.EscapeDataString(recordingName)}");

    private async ValueTask PostActionAsync(string recordingName, string action) =>
        await this.apiClient.PostContentAsync<object, object>(
            relativeUrl: $"api/recordings/{Uri.EscapeDataString(recordingName)}/{action}",
            content: new { },
            mediaType: "application/json");
}
