using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;
using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Samples.WebApi.Controllers;

// Cold support entry point (§3.1a) — customer taps "contact support" with no active call in
// progress. Kept as its own controller rather than folded into ClientsController/CallsController:
// RouteToQueueAsync is neither pure client-credential management nor a generic call start (it's
// keyed by customerId, not two arbitrary SIP extensions) — matches the minimal-API route it
// replaces (/support/{clientId}/route) closely enough to keep the same mental model.
[ApiController]
[Route("api/[controller]")]
public class SupportController : RESTFulController
{
    private readonly ICallProvider callProvider;
    private readonly SupportQueueBroadcastRegistry supportQueueBroadcastRegistry;

    public SupportController(ICallProvider callProvider, SupportQueueBroadcastRegistry supportQueueBroadcastRegistry)
    {
        this.callProvider = callProvider;
        this.supportQueueBroadcastRegistry = supportQueueBroadcastRegistry;
    }

    [HttpPost("{clientId:guid}/route")]
    public async ValueTask<ActionResult<CallSession>> PostRouteAsync(Guid clientId, [FromQuery] string queueName)
    {
        try
        {
            CallSession session = await this.callProvider.RouteToQueueAsync(clientId, queueName);

            // Publishes the now-waiting customer for AgentsController's broadcast stream — see
            // SupportQueueBroadcastRegistry's own comment for why this doesn't go through
            // ICallCenterProvider. A non-throwing dictionary write + Subject.OnNext, so it can't
            // introduce a new failure mode into this method's own catch ladder below.
            this.supportQueueBroadcastRegistry.PublishWaitingCustomer(
                clientId, queueName, session.CustomerChannelId, session.BridgeId);

            return this.Created(value: session);
        }
        catch (CallSessionValidationException callSessionValidationException)
            when (callSessionValidationException.InnerException is NotFoundQueueException
                or NotFoundSipCredentialsException)
        {
            // NotFoundQueueException/NotFoundSipCredentialsException are validation-category
            // exceptions at the foundation-service layer (AsteriskCallFoundationService.Exceptions.cs
            // treats "queue/customer doesn't exist" the same as a malformed request), but a REST
            // consumer expects 404 for "the thing you asked for doesn't exist", not 400 — same
            // distinction ClientsController already draws for its own not-found case.
            return this.NotFound(callSessionValidationException.InnerException);
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

    // Hands an in-progress call Ringly never originated (e.g. one Dograh's own ARI Stasis app is
    // currently handling) into a human queue — the endpoint an external AI agent's own
    // tool/function-calling webhook hits when a caller asks for a real person. A JSON body
    // (not query params like PostRouteAsync above) — confirmed live against Dograh's own tool
    // configuration UI, which always sends a POST tool call's parameters as a JSON body. Same
    // broadcast wiring as PostRouteAsync so the escalated caller shows up for agents via the
    // existing AgentsController claim flow unchanged.
    [HttpPost("escalate")]
    public async ValueTask<ActionResult<CallSession>> PostEscalateAsync([FromBody] EscalateToQueueRequest request)
    {
        try
        {
            CallSession session =
                await this.callProvider.EscalateToQueueAsync(request.ChannelId, request.QueueName);

            this.supportQueueBroadcastRegistry.PublishWaitingCustomer(
                Guid.NewGuid(), request.QueueName, session.CustomerChannelId, session.BridgeId);

            return this.Created(value: session);
        }
        catch (CallSessionValidationException callSessionValidationException)
            when (callSessionValidationException.InnerException is NotFoundQueueException)
        {
            return this.NotFound(callSessionValidationException.InnerException);
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

public record EscalateToQueueRequest(string ChannelId, string QueueName);
