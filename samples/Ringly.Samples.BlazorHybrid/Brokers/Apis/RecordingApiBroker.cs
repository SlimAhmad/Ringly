using Microsoft.Extensions.Options;
using RESTFulSense.Clients;
using Ringly.Samples.BlazorHybrid.Models.Recordings;

namespace Ringly.Samples.BlazorHybrid.Brokers.Apis;

public class RecordingApiBroker : IRecordingApiBroker
{
    private readonly IRESTFulApiFactoryClient apiClient;

    // Same base URL as SupportApiBroker/QueueApiBroker (Ringly.Samples.WebApi) — reuses
    // SupportApiOptions rather than a duplicate options type, since all target the same server.
    // Explicit SocketsHttpHandler — same Android-reliability reasoning as SupportApiBroker/
    // QueueApiBroker's own construction this session (PostStopAsync in particular can run long,
    // since the WebApi uploads the finished file to blob storage before returning).
    public RecordingApiBroker(IOptions<SupportApiOptions> options)
    {
        var httpClient = new HttpClient(new SocketsHttpHandler())
        {
            BaseAddress = new Uri(options.Value.BaseUrl)
        };

        this.apiClient = new RESTFulApiFactoryClient(httpClient);
    }

    public async ValueTask<IReadOnlyList<RecordingRow>> GetRecordingsAsync() =>
        await this.apiClient.GetContentAsync<List<RecordingRow>>(relativeUrl: "api/recordings");

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
