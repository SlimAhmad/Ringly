using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace Ringly.Storage.AzureBlob.Brokers;

public class AzureBlobBroker : IAzureBlobBroker
{
    private readonly BlobContainerClient containerClient;

    public AzureBlobBroker(IOptions<AzureBlobOptions> options)
    {
        AzureBlobOptions azureBlobOptions = options.Value;

        this.containerClient = new BlobContainerClient(
            azureBlobOptions.ConnectionString,
            azureBlobOptions.ContainerName);
    }

    public async ValueTask<Uri> UploadAsync(string recordingId, string localFilePath)
    {
        BlobClient blobClient = this.containerClient.GetBlobClient(recordingId);
        await using FileStream fileStream = File.OpenRead(localFilePath);
        await blobClient.UploadAsync(fileStream, overwrite: true);

        return blobClient.Uri;
    }

    public async ValueTask<Stream> DownloadAsync(string recordingId)
    {
        BlobClient blobClient = this.containerClient.GetBlobClient(recordingId);
        Response<BlobDownloadStreamingResult> download = await blobClient.DownloadStreamingAsync();

        return download.Value.Content;
    }

    public async ValueTask DeleteAsync(string recordingId)
    {
        BlobClient blobClient = this.containerClient.GetBlobClient(recordingId);
        await blobClient.DeleteAsync();
    }

    public ValueTask<Uri> GenerateSasUriAsync(string recordingId, TimeSpan expiry)
    {
        BlobClient blobClient = this.containerClient.GetBlobClient(recordingId);

        Uri sasUri = blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(expiry));

        return ValueTask.FromResult(sasUri);
    }

    public async ValueTask EnsureContainerExistsAsync() =>
        await this.containerClient.CreateIfNotExistsAsync();
}
