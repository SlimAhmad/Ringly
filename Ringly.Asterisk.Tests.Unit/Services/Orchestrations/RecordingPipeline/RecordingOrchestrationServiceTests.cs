using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Moq;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Services.Orchestrations.RecordingPipeline;
using Ringly.Storage.Abstractions;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Orchestrations.RecordingPipeline;

public partial class RecordingOrchestrationServiceTests
{
    private readonly Mock<IRecordingStorageProvider> recordingStorageProviderMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly RecordingPipelineOptions recordingPipelineOptions;
    private readonly RecordingOrchestrationService recordingOrchestrationService;

    public RecordingOrchestrationServiceTests()
    {
        this.recordingStorageProviderMock = new Mock<IRecordingStorageProvider>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();
        this.recordingPipelineOptions = new RecordingPipelineOptions();

        this.recordingOrchestrationService = new RecordingOrchestrationService(
            recordingStorageProvider: this.recordingStorageProviderMock.Object,
            loggingBroker: this.loggingBrokerMock.Object,
            options: Options.Create(this.recordingPipelineOptions));
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static string GetRandomString() =>
        new MnemonicString(wordCount: 3).GetValue();

    private static Uri GetRandomUri() =>
        new($"https://example.blob.core.windows.net/{Guid.NewGuid()}");
}
