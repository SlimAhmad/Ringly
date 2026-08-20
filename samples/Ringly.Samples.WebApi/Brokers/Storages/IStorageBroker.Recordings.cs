using Ringly.Samples.WebApi.Models.Foundations.Recordings;

namespace Ringly.Samples.WebApi.Brokers.Storages;

public partial interface IStorageBroker
{
    ValueTask<Recording> InsertRecordingAsync(Recording recording);
    ValueTask<IQueryable<Recording>> SelectAllRecordingsAsync();
    ValueTask<Recording> SelectRecordingByIdAsync(Guid recordingId);
    ValueTask<Recording?> SelectRecordingByNameAsync(string recordingName);
    ValueTask<Recording> UpdateRecordingAsync(Recording recording);
    ValueTask<Recording> DeleteRecordingAsync(Recording recording);
}
