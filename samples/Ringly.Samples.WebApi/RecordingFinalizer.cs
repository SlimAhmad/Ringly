using System.Reactive.Linq;
using Microsoft.Extensions.Options;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;
using Ringly.Samples.WebApi.Models.Foundations.Recordings;
using Ringly.Samples.WebApi.Services.Foundations.Recordings;
using Ringly.Storage.Abstractions;

namespace Ringly.Samples.WebApi;

// Confirmed live as a real gap: RecordingsController.PostStopAsync only uploaded to blob storage
// when a client explicitly called the stop action — if the underlying call hung up on its own
// first (the common case in practice), Asterisk auto-finalizes the local recording file but
// nothing ever uploaded it. Subscribing to ARI's own RecordingFinished event (fired whenever a
// live recording ends, by explicit Stop/Cancel OR because its bridge was torn down) catches both
// cases uniformly, so PostStopAsync itself no longer needs its own copy of this logic.
public class RecordingFinalizer : BackgroundService
{
    private const string DoneState = "done";
    private const string StoppedState = "stopped";

    private readonly IAsteriskBroker asteriskBroker;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IRecordingStorageProvider recordingStorageProvider;
    private readonly RecordingSpoolOptions recordingSpoolOptions;
    private readonly ILogger<RecordingFinalizer> logger;

    public RecordingFinalizer(
        IAsteriskBroker asteriskBroker,
        IServiceScopeFactory serviceScopeFactory,
        IRecordingStorageProvider recordingStorageProvider,
        IOptions<RecordingSpoolOptions> recordingSpoolOptions,
        ILogger<RecordingFinalizer> logger)
    {
        this.asteriskBroker = asteriskBroker;
        this.serviceScopeFactory = serviceScopeFactory;
        this.recordingStorageProvider = recordingStorageProvider;
        this.recordingSpoolOptions = recordingSpoolOptions.Value;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IDisposable subscription = this.asteriskBroker.StreamRecordingFinishedEvents()
            .Subscribe(recordingFinishedEvent => this.OnRecordingFinished(recordingFinishedEvent));

        stoppingToken.Register(() => subscription.Dispose());

        return Task.CompletedTask;
    }

    private async void OnRecordingFinished(RecordingFinishedEvent recordingFinishedEvent)
    {
        try
        {
            await this.HandleRecordingFinishedAsync(recordingFinishedEvent);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to finalize recording {RecordingName}",
                recordingFinishedEvent.RecordingName);
        }
    }

    private async Task HandleRecordingFinishedAsync(RecordingFinishedEvent recordingFinishedEvent)
    {
        // IRecordingService is Scoped (it ultimately depends on IStorageBroker, a DbContext) —
        // this service is a Singleton hosted service, so a scope is created per event rather than
        // holding the dependency directly, the standard pattern for accessing Scoped services from
        // a Singleton (same as TelephonyCallTrackingService's own comment on this exact problem;
        // confirmed live: consuming it directly here throws at startup — "Cannot consume scoped
        // service ... from singleton").
        using IServiceScope scope = this.serviceScopeFactory.CreateScope();

        IRecordingService recordingService = scope.ServiceProvider.GetRequiredService<IRecordingService>();

        Recording? recording =
            await recordingService.RetrieveRecordingByNameAsync(recordingFinishedEvent.RecordingName);

        if (recording is null || !string.IsNullOrEmpty(recording.BlobUrl))
        {
            // Either not one of ours, or an explicit action already finalized it before this
            // event was processed — nothing left to do.
            return;
        }

        if (recordingFinishedEvent.State != DoneState)
        {
            // "failed"/"canceled" — no complete audio file to upload, just reflect the real state.
            recording.State = recordingFinishedEvent.State;
            await recordingService.ModifyRecordingAsync(recording);
            return;
        }

        string localFilePath = Path.Combine(
            this.recordingSpoolOptions.Directory, $"{recording.RecordingName}.{recording.Format}");

        Uri uploadedUri = await this.recordingStorageProvider.UploadRecordingAsync(
            localFilePath, recording.RecordingName);

        recording.State = StoppedState;
        recording.BlobUrl = uploadedUri.ToString();
        await recordingService.ModifyRecordingAsync(recording);
    }
}
