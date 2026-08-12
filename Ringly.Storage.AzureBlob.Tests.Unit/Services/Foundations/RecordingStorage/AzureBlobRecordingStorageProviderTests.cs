using System.Linq.Expressions;
using Moq;
using Ringly.Storage.AzureBlob.Brokers;
using Ringly.Storage.AzureBlob.Services.Foundations.RecordingStorage;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Storage.AzureBlob.Tests.Unit.Services.Foundations.RecordingStorage;

public partial class AzureBlobRecordingStorageProviderTests
{
    private readonly Mock<IAzureBlobBroker> azureBlobBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly AzureBlobRecordingStorageProvider recordingStorageProvider;

    public AzureBlobRecordingStorageProviderTests()
    {
        this.azureBlobBrokerMock = new Mock<IAzureBlobBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.recordingStorageProvider = new AzureBlobRecordingStorageProvider(
            azureBlobBroker: this.azureBlobBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static string GetRandomString() =>
        new MnemonicString(wordCount: 3).GetValue();

    private static Uri GetRandomUri() =>
        new($"https://example.blob.core.windows.net/{Guid.NewGuid()}");

    private static TimeSpan GetRandomTimeSpan() =>
        TimeSpan.FromMinutes(new IntRange(min: 1, max: 60).GetValue());
}
