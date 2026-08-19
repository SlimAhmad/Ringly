using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyCalls;

namespace Ringly.Samples.WebApi.Controllers;

// Read-only call history/CDR — nothing in this controller writes a TelephonyCall row; that's the
// job of the call-event orchestration service (tracked separately) reacting to real call state
// changes, not something a client should be able to fabricate over HTTP.
[ApiController]
[Route("api/[controller]")]
public class TelephonyCallsController : RESTFulController
{
    private readonly ITelephonyCallService telephonyCallService;

    public TelephonyCallsController(ITelephonyCallService telephonyCallService) =>
        this.telephonyCallService = telephonyCallService;

    // GET api/telephonycalls?callerIdentityId=... — query-param-based queryable read, per the
    // Standard's OData/query-param guidance; omitting callerIdentityId returns the full history.
    [HttpGet]
    public async ValueTask<ActionResult<IQueryable<TelephonyCall>>> GetTelephonyCallsAsync(
        [FromQuery] Guid? callerIdentityId)
    {
        try
        {
            IQueryable<TelephonyCall> telephonyCalls = callerIdentityId is Guid identityId
                ? await this.telephonyCallService.RetrieveTelephonyCallsByCallerIdentityIdAsync(identityId)
                : await this.telephonyCallService.RetrieveAllTelephonyCallsAsync();

            return this.Ok(telephonyCalls);
        }
        catch (TelephonyCallValidationException telephonyCallValidationException)
        {
            return this.BadRequest(telephonyCallValidationException.InnerException);
        }
        catch (TelephonyCallDependencyException telephonyCallDependencyException)
        {
            return this.InternalServerError(telephonyCallDependencyException);
        }
        catch (TelephonyCallServiceException telephonyCallServiceException)
        {
            return this.InternalServerError(telephonyCallServiceException);
        }
    }

    [HttpGet("{telephonyCallId:guid}")]
    public async ValueTask<ActionResult<TelephonyCall>> GetTelephonyCallByIdAsync(Guid telephonyCallId)
    {
        try
        {
            TelephonyCall telephonyCall =
                await this.telephonyCallService.RetrieveTelephonyCallByIdAsync(telephonyCallId);

            return this.Ok(telephonyCall);
        }
        catch (TelephonyCallValidationException telephonyCallValidationException)
            when (telephonyCallValidationException.InnerException is NotFoundTelephonyCallException)
        {
            return this.NotFound(telephonyCallValidationException.InnerException);
        }
        catch (TelephonyCallValidationException telephonyCallValidationException)
        {
            return this.BadRequest(telephonyCallValidationException.InnerException);
        }
        catch (TelephonyCallDependencyException telephonyCallDependencyException)
        {
            return this.InternalServerError(telephonyCallDependencyException);
        }
        catch (TelephonyCallServiceException telephonyCallServiceException)
        {
            return this.InternalServerError(telephonyCallServiceException);
        }
    }
}
