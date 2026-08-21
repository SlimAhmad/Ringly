using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;
using Ringly.Samples.WebApi.Services.Foundations.Recordings;
using Ringly.Storage.Abstractions;

namespace Ringly.Samples.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordingsController : RESTFulController
{
    private readonly ICallCenterProvider callCenterProvider;
    private readonly IRecordingService recordingService;
    private readonly IRecordingStorageProvider recordingStorageProvider;

    public RecordingsController(
        ICallCenterProvider callCenterProvider,
        IRecordingService recordingService,
        IRecordingStorageProvider recordingStorageProvider)
    {
        this.callCenterProvider = callCenterProvider;
        this.recordingService = recordingService;
        this.recordingStorageProvider = recordingStorageProvider;
    }

    [HttpGet]
    public async ValueTask<ActionResult<IQueryable<Models.Foundations.Recordings.Recording>>> GetRecordingsAsync() =>
        this.Ok(await this.recordingService.RetrieveAllRecordingsAsync());

    // The blobUrl already returned by GetRecordingsAsync isn't directly playable — the container
    // is private (confirmed live: a plain GET on it returns 403 AuthorizationFailure), so a real
    // caller needs a signed, time-limited URL instead. IRecordingStorageProvider already had
    // GenerateTemporaryAccessUrlAsync built for exactly this; it just had no endpoint exposing it.
    [HttpGet("{recordingName}/access-url")]
    public async ValueTask<ActionResult<Uri>> GetAccessUrlAsync(
        string recordingName, [FromQuery] int expiryMinutes = 60)
    {
        try
        {
            Uri accessUrl = await this.recordingStorageProvider.GenerateTemporaryAccessUrlAsync(
                recordingName, TimeSpan.FromMinutes(expiryMinutes));

            return this.Ok(accessUrl);
        }
        catch (Exception exception)
        {
            return this.HandleRecordingException(exception);
        }
    }

    [HttpPost]
    public async ValueTask<ActionResult<RecordingInfo>> PostRecordingAsync(InsertRecordingRequest request)
    {
        try
        {
            RecordingInfo recordingInfo = await this.callCenterProvider.InsertRecordingAsync(
                request.BridgeId, request.RecordingName, request.Format);

            // Best-effort persistence — the ARI recording itself already started successfully by
            // this point; a storage failure here shouldn't be reported as "starting the recording
            // failed" (a real, different operation that genuinely succeeded).
            await this.recordingService.AddRecordingAsync(new Models.Foundations.Recordings.Recording
            {
                Id = Guid.NewGuid(),
                BridgeId = request.BridgeId,
                RecordingName = request.RecordingName,
                Format = request.Format,
                State = recordingInfo.State,
                StartedDate = DateTimeOffset.UtcNow
            });

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
            await this.UpdateRecordingStateAsync(recordingName, "paused");
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
            await this.UpdateRecordingStateAsync(recordingName, "recording");
            return this.Ok();
        }
        catch (Exception exception)
        {
            return this.HandleRecordingException(exception);
        }
    }

    // Only tells Asterisk to stop — the actual upload-to-blob-storage + state/BlobUrl update now
    // happens in RecordingFinalizer, reacting to ARI's own RecordingFinished event instead of
    // living here. That event fires for this explicit stop AND for a call that just hangs up on
    // its own (confirmed live: the latter previously left recordings permanently un-uploaded,
    // since nothing here was reachable if the client never got a chance to call this action).
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
            await this.UpdateRecordingStateAsync(recordingName, "canceled");
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

            Models.Foundations.Recordings.Recording? recording =
                await this.recordingService.RetrieveRecordingByNameAsync(recordingName);

            if (recording is not null)
            {
                // Best-effort — the blob may not exist at all if this recording was deleted
                // before ever being stopped/uploaded.
                try
                {
                    await this.recordingStorageProvider.DeleteRecordingAsync(recordingName);
                }
                catch (Exception)
                {
                }

                await this.recordingService.RemoveRecordingByIdAsync(recording.Id);
            }

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

    private async ValueTask UpdateRecordingStateAsync(string recordingName, string state)
    {
        Models.Foundations.Recordings.Recording? recording =
            await this.recordingService.RetrieveRecordingByNameAsync(recordingName);

        if (recording is not null)
        {
            recording.State = state;
            await this.recordingService.ModifyRecordingAsync(recording);
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

        RecordingDependencyValidationException recordingDependencyValidationException
            when recordingDependencyValidationException.InnerException is NotFoundRecordingException =>
                this.NotFound(recordingDependencyValidationException.InnerException),

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
