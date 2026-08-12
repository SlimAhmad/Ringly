using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string BridgesRecordRelativeUrlFormat = "bridges/{0}/record";

    public async ValueTask<LiveRecording> InsertRecordingAsync(
        string bridgeId,
        string recordingName,
        string format)
    {
        string relativeUrl = string.Format(BridgesRecordRelativeUrlFormat, bridgeId);

        return await this.PostAsync<LiveRecording>(
            $"{relativeUrl}?name={Uri.EscapeDataString(recordingName)}&format={Uri.EscapeDataString(format)}");
    }
}
