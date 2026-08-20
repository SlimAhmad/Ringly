using Ringly.Samples.WebApi.Models.Foundations.Recordings;

namespace Ringly.Samples.WebApi.Services.Foundations.Recordings;

public interface IRecordingService
{
    ValueTask<Recording> AddRecordingAsync(Recording recording);
    ValueTask<IQueryable<Recording>> RetrieveAllRecordingsAsync();
    ValueTask<Recording> RetrieveRecordingByIdAsync(Guid recordingId);
    ValueTask<Recording?> RetrieveRecordingByNameAsync(string recordingName);
    ValueTask<Recording> ModifyRecordingAsync(Recording recording);
    ValueTask<Recording> RemoveRecordingByIdAsync(Guid recordingId);
}
