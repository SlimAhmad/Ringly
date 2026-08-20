using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

namespace Ringly.Samples.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordingsController : RESTFulController
{
    private readonly ICallCenterProvider callCenterProvider;

    public RecordingsController(ICallCenterProvider callCenterProvider) =>
        this.callCenterProvider = callCenterProvider;

    [HttpPost]
    public async ValueTask<ActionResult<RecordingInfo>> PostRecordingAsync(InsertRecordingRequest request)
    {
        try
        {
            RecordingInfo recordingInfo = await this.callCenterProvider.InsertRecordingAsync(
                request.BridgeId, request.RecordingName, request.Format);

            return this.Created(value: recordingInfo);
        }
        catch (Exception exception)
        {
            return this.HandleRecordingException(exception);
        }
    }

    [HttpPost("{recordingName}/pause")]
    public async ValueTask<ActionResult> PostPauseAsync(string recordingName)
    {
        try
        {
            await this.callCenterProvider.PauseRecordingAsync(recordingName);
            return this.Ok();
        }
        catch (Exception exception)
        {
            return this.HandleRecordingException(exception);
        }
    }

    [HttpPost("{recordingName}/unpause")]
    public async ValueTask<ActionResult> PostUnpauseAsync(string recordingName)
    {
        try
        {
            await this.callCenterProvider.UnpauseRecordingAsync(recordingName);
            return this.Ok();
        }
        catch (Exception exception)
        {
            return this.HandleRecordingException(exception);
        }
    }

    [HttpPost("{recordingName}/stop")]
    public async ValueTask<ActionResult> PostStopAsync(string recordingName)
    {
        try
        {
            await this.callCenterProvider.StopRecordingAsync(recordingName);
            return this.Ok();
        }
        catch (Exception exception)
        {
            return this.HandleRecordingException(exception);
        }
    }

    [HttpPost("{recordingName}/cancel")]
    public async ValueTask<ActionResult> PostCancelAsync(string recordingName)
    {
        try
        {
            await this.callCenterProvider.CancelRecordingAsync(recordingName);
            return this.Ok();
        }
        catch (Exception exception)
        {
            return this.HandleRecordingException(exception);
        }
    }

    [HttpDelete("{recordingName}")]
    public async ValueTask<ActionResult> DeleteRecordingAsync(string recordingName)
    {
        try
        {
            await this.callCenterProvider.DeleteStoredRecordingAsync(recordingName);
            return this.Ok();
        }
        catch (Exception exception)
        {
            return this.HandleRecordingException(exception);
        }
    }

    [HttpPost("{recordingName}/copy")]
    public async ValueTask<ActionResult> PostCopyAsync(string recordingName, [FromQuery] string destinationName)
    {
        try
        {
            await this.callCenterProvider.CopyStoredRecordingAsync(recordingName, destinationName);
            return this.Ok();
        }
        catch (Exception exception)
        {
            return this.HandleRecordingException(exception);
        }
    }

    // Shared across all seven actions — same four-category exception mapping repeated verbatim
    // per action would be pure duplication (RecordingsController's dependency is the same service
    // interface each time), so it's centralized here instead. Rethrows anything not one of these
    // four so an actual bug still surfaces as a 500 with a real stack trace, not a swallowed 500.
    private ActionResult HandleRecordingException(Exception exception) => exception switch
    {
        RecordingValidationException recordingValidationException =>
            this.BadRequest(recordingValidationException.InnerException),

        RecordingDependencyValidationException recordingDependencyValidationException =>
            this.BadRequest(recordingDependencyValidationException.InnerException),

        RecordingDependencyException recordingDependencyException =>
            this.InternalServerError(recordingDependencyException),

        RecordingServiceException recordingServiceException =>
            this.InternalServerError(recordingServiceException),

        _ => throw exception
    };
}

public record InsertRecordingRequest(string BridgeId, string RecordingName, string Format);
