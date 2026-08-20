using Microsoft.EntityFrameworkCore;
using Ringly.Samples.WebApi.Models.Foundations.Recordings;

namespace Ringly.Samples.WebApi.Brokers.Storages;

public partial class StorageBroker
{
    public DbSet<Recording> Recordings { get; set; } = null!;

    public async ValueTask<Recording> InsertRecordingAsync(Recording recording) =>
        await this.InsertAsync(recording);

    public async ValueTask<IQueryable<Recording>> SelectAllRecordingsAsync() =>
        await this.SelectAllAsync<Recording>();

    public async ValueTask<Recording> SelectRecordingByIdAsync(Guid recordingId) =>
        await this.SelectAsync<Recording>(recordingId);

    public async ValueTask<Recording?> SelectRecordingByNameAsync(string recordingName)
    {
        IQueryable<Recording> recordings = await this.SelectAllAsync<Recording>();

        return recordings.FirstOrDefault(recording => recording.RecordingName == recordingName);
    }

    public async ValueTask<Recording> UpdateRecordingAsync(Recording recording) =>
        await this.UpdateAsync(recording);

    public async ValueTask<Recording> DeleteRecordingAsync(Recording recording) =>
        await this.DeleteAsync(recording);

    private void ConfigureRecordings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recording>(builder =>
        {
            builder.Property(recording => recording.Id).IsRequired();
            builder.Property(recording => recording.BridgeId).HasMaxLength(255).IsRequired();
            builder.Property(recording => recording.RecordingName).HasMaxLength(255).IsRequired();
            builder.Property(recording => recording.Format).HasMaxLength(50).IsRequired();
            builder.Property(recording => recording.State).HasMaxLength(50).IsRequired();
            builder.Property(recording => recording.BlobUrl).HasMaxLength(2048);

            builder.HasIndex(recording => recording.RecordingName).IsUnique();
        });
    }
}
