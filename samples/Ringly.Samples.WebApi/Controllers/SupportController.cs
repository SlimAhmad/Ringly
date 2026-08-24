using System.Text.Json.Serialization;
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

    // Dograh's native "Call Transfer" tool's Dynamic HTTP Resolver
    // (docs.dograh.com/voice-agent/tools/call-transfer) — replaces the raw ARI /channels/{id}/move
    // approach (PostEscalateAsync above), which turned out to make Dograh's own app hang the call
    // up defensively the instant it noticed the channel disappear from its Stasis app.
    //
    // The destination MUST be a real, registered PJSIP/SIP endpoint - confirmed live twice: a
    // static PJSIP AOR contact can't point at a Local channel (res_pjsip only accepts genuine
    // sip(s): URIs), and Dograh's own tool mis-parses a bare "Local/..." destination string
    // outright ("Unable to create PJSIP channel - endpoint 'Local' was not found" - it forces
    // tech=PJSIP regardless of what's actually given). "supportregistrar" is
    // QueueTransferRegistrarService's own real, always-registered endpoint - Dograh dials it,
    // gets answered, and (since Dograh's own app then directly bridges the caller to whatever
    // answered, bypassing Ringly's own Stasis app/holding bridge/MOH/claim system entirely) that
    // service immediately sends a real SIP BlindTransfer back into Asterisk targeting the actual
    // "support" queue, landing the caller in the same holding bridge/claim flow a native Ringly
    // customer would use.
    //
    // Only one department/queue is wired up this way for now - see QueueTransferRegistrarService
    // for what a second one (e.g. "billing") would need (its own registered identity + transfer
    // target; not yet built).
    [HttpPost("dograh-transfer-resolver")]
    public ActionResult<DograhTransferResolverResponse> PostDograhTransferResolverAsync(
        [FromBody] Dictionary<string, object> request) =>
        this.Ok(new DograhTransferResolverResponse(
            new DograhTransferContext(
                Destination: "PJSIP/supportregistrar",
                CustomMessage: "Connecting you to a support agent now.")));
}

public record EscalateToQueueRequest(string ChannelId, string QueueName);

// [JsonPropertyName] required on every field here — ASP.NET Core's default MVC JSON output is
// camelCase (transferContext/customMessage), but Dograh's own docs specify snake_case
// (transfer_context/custom_message) for the Dynamic HTTP Resolver's expected response shape.
public record DograhTransferContext(
    [property: JsonPropertyName("destination")] string Destination,
    [property: JsonPropertyName("custom_message")] string CustomMessage);

public record DograhTransferResolverResponse(
    [property: JsonPropertyName("transfer_context")] DograhTransferContext TransferContext);
