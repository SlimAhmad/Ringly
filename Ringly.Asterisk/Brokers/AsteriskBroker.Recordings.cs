using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string BridgesRecordRelativeUrlFormat = "bridges/{0}/record";
    private const string LiveRecordingsRelativeUrlFormat = "recordings/live/{0}";

    public async ValueTask<LiveRecording> InsertRecordingAsync(
        string bridgeId,
        string recordingName,
        string format)
    {
        string relativeUrl = string.Format(BridgesRecordRelativeUrlFormat, bridgeId);

        return await this.PostAsync<LiveRecording>(
            $"{relativeUrl}?name={Uri.EscapeDataString(recordingName)}&format={Uri.EscapeDataString(format)}");
    }

    public async ValueTask PauseRecordingAsync(string recordingName) =>
        await this.PostAsync($"{LiveRecordingRelativeUrl(recordingName)}/pause");

    public async ValueTask UnpauseRecordingAsync(string recordingName) =>
        await this.PostAsync($"{LiveRecordingRelativeUrl(recordingName)}/unpause");

    public async ValueTask StopRecordingAsync(string recordingName) =>
        await this.PostAsync($"{LiveRecordingRelativeUrl(recordingName)}/stop");

    public async ValueTask CancelRecordingAsync(string recordingName) =>
        await this.PostAsync($"{LiveRecordingRelativeUrl(recordingName)}/cancel");

    private static string LiveRecordingRelativeUrl(string recordingName) =>
        string.Format(LiveRecordingsRelativeUrlFormat, Uri.EscapeDataString(recordingName));
}
