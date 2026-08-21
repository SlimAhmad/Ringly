using Ringly.Asterisk.Brokers;
using Ringly.Samples.WebApi.Brokers.Storages;
using Ringly.Samples.WebApi.Models.Foundations.Recordings;
using Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

namespace Ringly.Samples.WebApi.Services.Foundations.Recordings;

public partial class RecordingService : IRecordingService
{
    private readonly IStorageBroker storageBroker;
    private readonly ILoggingBroker loggingBroker;

    public RecordingService(IStorageBroker storageBroker, ILoggingBroker loggingBroker)
    {
        this.storageBroker = storageBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<Recording> AddRecordingAsync(Recording recording) =>
    TryCatch(async () =>
    {
        ValidateRecordingOnAdd(recording);

        return await this.storageBroker.InsertRecordingAsync(recording);
    });

    public ValueTask<IQueryable<Recording>> RetrieveAllRecordingsAsync() =>
    TryCatch(async () => await this.storageBroker.SelectAllRecordingsAsync());

    public ValueTask<Recording> RetrieveRecordingByIdAsync(Guid recordingId) =>
    TryCatch(async () =>
    {
        ValidateRecordingId(recordingId);

        Recording? maybeRecording = await this.storageBroker.SelectRecordingByIdAsync(recordingId);

        ValidateStorageRecordingExists(maybeRecording, recordingId);

        return maybeRecording!;
    });

    public ValueTask<Recording?> RetrieveRecordingByNameAsync(string recordingName) =>
    TryCatchNullable(async () =>
    {
        ValidateRecordingName(recordingName);

        return await this.storageBroker.SelectRecordingByNameAsync(recordingName);
    });

    public ValueTask<Recording> ModifyRecordingAsync(Recording recording) =>
    TryCatch(async () =>
    {
        ValidateRecordingOnModify(recording);

        Recording? maybeRecording = await this.storageBroker.SelectRecordingByIdAsync(recording.Id);

        ValidateStorageRecordingExists(maybeRecording, recording.Id);

        // Confirmed live as a real bug: SelectRecordingByIdAsync above already tracks an instance
        // with this Id in the DbContext's change tracker (it goes through EF's FindAsync) —
        // updating the caller-supplied `recording` parameter instead, a *different* .NET object
        // with the same key, threw "cannot be tracked because another instance with the same key
        // value... is already being tracked". Copying the caller's changes onto the
        // already-tracked instance instead avoids the conflict.
        maybeRecording!.BridgeId = recording.BridgeId;
        maybeRecording.RecordingName = recording.RecordingName;
        maybeRecording.Format = recording.Format;
        maybeRecording.State = recording.State;
        maybeRecording.BlobUrl = recording.BlobUrl;
        maybeRecording.StartedDate = recording.StartedDate;

        return await this.storageBroker.UpdateRecordingAsync(maybeRecording);
    });

    public ValueTask<Recording> RemoveRecordingByIdAsync(Guid recordingId) =>
    TryCatch(async () =>
    {
        ValidateRecordingId(recordingId);

        Recording? maybeRecording = await this.storageBroker.SelectRecordingByIdAsync(recordingId);

        ValidateStorageRecordingExists(maybeRecording, recordingId);

        return await this.storageBroker.DeleteRecordingAsync(maybeRecording!);
    });
}
