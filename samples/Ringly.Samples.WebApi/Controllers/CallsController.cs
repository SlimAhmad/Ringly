using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;
using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Samples.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CallsController : RESTFulController
{
    private readonly ICallProvider callProvider;

    public CallsController(ICallProvider callProvider) =>
        this.callProvider = callProvider;

    [HttpPost]
    public async ValueTask<ActionResult<CallSession>> PostCallAsync(StartCallRequest request)
    {
        try
        {
            CallSession session = await this.callProvider.StartCallSessionAsync(
                new CallParticipant { SipExtension = request.PartyAExtension },
                new CallParticipant { SipExtension = request.PartyBExtension });

            return this.Created(value: session);
        }
        catch (CallSessionValidationException callSessionValidationException)
        {
            return this.BadRequest(callSessionValidationException.InnerException);
        }
        catch (CallSessionDependencyValidationException callSessionDependencyValidationException)
        {
            return this.BadRequest(callSessionDependencyValidationException.InnerException);
        }
        catch (CallProviderDependencyException callProviderDependencyException)
        {
            return this.InternalServerError(callProviderDependencyException);
        }
        catch (CallProviderServiceException callProviderServiceException)
        {
            return this.InternalServerError(callProviderServiceException);
        }
    }
}

public record StartCallRequest(string PartyAExtension, string PartyBExtension);
