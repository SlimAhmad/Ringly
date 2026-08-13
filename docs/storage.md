# Recording Storage

`Ringly.Storage.Abstractions` defines where finished call recordings end up:

```csharp
public interface IRecordingStorageProvider
{
    ValueTask<Uri> UploadRecordingAsync(string localFilePath, string recordingId);
    ValueTask<Stream> DownloadRecordingAsync(string recordingId);
    ValueTask DeleteRecordingAsync(string recordingId);
    ValueTask<Uri> GenerateTemporaryAccessUrlAsync(string recordingId, TimeSpan expiry);
}
```

## Azure Blob Storage — `Ringly.Storage.AzureBlob`

```csharp
dotnet add package Ringly.Storage.AzureBlob
```

```csharp
services.Configure<AzureBlobOptions>(options =>
{
    options.ConnectionString = "..."; // Azure Storage account connection string
    options.ContainerName = "call-recordings";
});

services.AddSingleton<IAzureBlobBroker, AzureBlobBroker>();
services.AddScoped<IRecordingStorageProvider, AzureBlobRecordingStorageProvider>();
```

```csharp
public AzureBlobRecordingStorageProvider(
    IAzureBlobBroker azureBlobBroker,
    ILoggingBroker loggingBroker)
```

```csharp
Uri recordingUrl = await recordingStorageProvider.UploadRecordingAsync(
    localFilePath: "/tmp/recording.wav",
    recordingId: "call-12345");

Uri temporaryLink = await recordingStorageProvider.GenerateTemporaryAccessUrlAsync(
    "call-12345", expiry: TimeSpan.FromHours(1));
```

## Wiring it to Asterisk's recording pipeline

`Ringly.Asterisk` has `RecordingOrchestrationService`, which uploads a finished
Asterisk recording (from its local spool directory) through whichever
`IRecordingStorageProvider` you've registered:

```csharp
services.Configure<RecordingPipelineOptions>(options =>
    options.RecordingsSpoolPath = "/var/spool/asterisk/recording"); // Asterisk's default spool path

services.AddScoped<IRecordingOrchestrationService, RecordingOrchestrationService>();
```

```csharp
public RecordingOrchestrationService(
    IRecordingStorageProvider recordingStorageProvider,
    ILoggingBroker loggingBroker,      // Ringly.Asterisk.Brokers.ILoggingBroker
    IOptions<RecordingPipelineOptions> options)
```

```csharp
Uri uploadedUrl = await recordingOrchestrationService.UploadFinishedRecordingAsync(
    recordingName: "call-12345", format: "wav");
```

Wiring the automatic trigger (subscribing to Asterisk's `RecordingFinished` event
and calling this automatically) is a hosted-service concern left to your app — call
`UploadFinishedRecordingAsync` from wherever you observe that event.
