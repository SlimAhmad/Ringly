using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;
using RESTFulSense.Exceptions;
using Xeptions;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService
{
    private delegate ValueTask<RecordingInfo> ReturningRecordingInfoFunction();
    private delegate ValueTask RecordingReturningValueTaskFunction();

    private async ValueTask<RecordingInfo> TryCatchRecordingInfo(
        ReturningRecordingInfoFunction returningRecordingInfoFunction)
    {
        try
        {
            return await returningRecordingInfoFunction();
        }
        catch (InvalidRecordingRequestException invalidRecordingRequestException)
        {
            throw await CreateAndLogRecordingValidationException(invalidRecordingRequestException);
        }
        catch (HttpResponseBadRequestException)
        {
            var invalidRecordingRequestException = new InvalidRecordingRequestException();
            throw await CreateAndLogRecordingDependencyValidationException(invalidRecordingRequestException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogRecordingCriticalDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogRecordingCriticalDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseNotFoundException);

            throw await CreateAndLogRecordingCriticalDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpRequestException);

            throw await CreateAndLogRecordingCriticalDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogRecordingDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogRecordingDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseException);

            throw await CreateAndLogRecordingDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (Exception exception)
        {
            var failedRecordingServiceException = new FailedRecordingServiceException(exception);
            throw await CreateAndLogRecordingServiceException(failedRecordingServiceException);
        }
    }

    private async ValueTask TryCatchRecording(RecordingReturningValueTaskFunction returningValueTaskFunction)
    {
        try
        {
            await returningValueTaskFunction();
        }
        catch (InvalidRecordingRequestException invalidRecordingRequestException)
        {
            throw await CreateAndLogRecordingValidationException(invalidRecordingRequestException);
        }
        catch (HttpResponseBadRequestException)
        {
            var invalidRecordingRequestException = new InvalidRecordingRequestException();
            throw await CreateAndLogRecordingDependencyValidationException(invalidRecordingRequestException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogRecordingCriticalDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogRecordingCriticalDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseNotFoundException);

            throw await CreateAndLogRecordingCriticalDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpRequestException);

            throw await CreateAndLogRecordingCriticalDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogRecordingDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogRecordingDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedAsteriskRecordingDependencyException =
                new FailedAsteriskRecordingDependencyException(httpResponseException);

            throw await CreateAndLogRecordingDependencyException(failedAsteriskRecordingDependencyException);
        }
        catch (Exception exception)
        {
            var failedRecordingServiceException = new FailedRecordingServiceException(exception);
            throw await CreateAndLogRecordingServiceException(failedRecordingServiceException);
        }
    }

    private async ValueTask<RecordingValidationException> CreateAndLogRecordingValidationException(
        Xeption exception)
    {
        var recordingValidationException = new RecordingValidationException(exception);
        await this.loggingBroker.LogErrorAsync(recordingValidationException);

        return recordingValidationException;
    }

    private async ValueTask<RecordingDependencyValidationException> CreateAndLogRecordingDependencyValidationException(
        Xeption exception)
    {
        var recordingDependencyValidationException = new RecordingDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(recordingDependencyValidationException);

        return recordingDependencyValidationException;
    }

    private async ValueTask<RecordingDependencyException> CreateAndLogRecordingCriticalDependencyException(
        Xeption exception)
    {
        var recordingDependencyException = new RecordingDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(recordingDependencyException);

        return recordingDependencyException;
    }

    private async ValueTask<RecordingDependencyException> CreateAndLogRecordingDependencyException(
        Xeption exception)
    {
        var recordingDependencyException = new RecordingDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(recordingDependencyException);

        return recordingDependencyException;
    }

    private async ValueTask<RecordingServiceException> CreateAndLogRecordingServiceException(Xeption exception)
    {
        var recordingServiceException = new RecordingServiceException(exception);
        await this.loggingBroker.LogErrorAsync(recordingServiceException);

        return recordingServiceException;
    }
}
