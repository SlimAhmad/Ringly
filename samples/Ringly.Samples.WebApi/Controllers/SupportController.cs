using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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

    private const string DefaultDepartment = "support";

    // Only lowercase letters — matches extensions.conf's own _[a-z]. dialplan pattern intent, and
    // guards against an external caller (Dograh's own backend, not directly user-controlled, but
    // still a network boundary) injecting "@"/"/" or other characters into a raw Asterisk dial
    // string built from this value.
    private static readonly Regex DepartmentPattern = new("^[a-z]+$", RegexOptions.Compiled);

    // Dograh's native "Call Transfer" tool's Dynamic HTTP Resolver
    // (docs.dograh.com/voice-agent/tools/call-transfer) — replaces the raw ARI /channels/{id}/move
    // approach (PostEscalateAsync above), which turned out to make Dograh's own app hang the call
    // up defensively the instant it noticed the channel disappear from its Stasis app. Dograh
    // POSTs a flat JSON object of whatever LLM/preset parameters its tool config sends — configure
    // an LLM parameter named "department" (e.g. extracted from the conversation as "support" or
    // "billing") to route to that queue by name; falls back to "support" if missing or if the
    // value doesn't look like a real queue name, rather than handing Asterisk an unvalidated dial
    // string. Expects back transfer_context.destination as a real SIP endpoint string; Dograh
    // itself then dials that destination and manages the transfer, so this is pure mapping with no
    // service call needed — RideHailingCallRouter is what actually validates the department
    // against IQueueRegistry once the dialed channel reaches it.
    // "Local/{department}@ride_hailing" is a plain Asterisk dial string (no PJSIP endpoint/AOR
    // needed — confirmed live that a static AOR contact pointing at a Local channel is rejected
    // outright by res_pjsip, which only accepts genuine sip(s): URIs there) that lands directly in
    // extensions.conf's own [ride_hailing] dialplan. Unconfirmed whether Dograh's own tool
    // validation accepts a "Local/..." destination at all (its docs only give PJSIP/SIP examples)
    // — needs a live test.
    [HttpPost("dograh-transfer-resolver")]
    public ActionResult<DograhTransferResolverResponse> PostDograhTransferResolverAsync(
        [FromBody] Dictionary<string, object> request)
    {
        string department = request.TryGetValue("department", out object? value)
            && value?.ToString() is string requestedDepartment
            && DepartmentPattern.IsMatch(requestedDepartment)
                ? requestedDepartment
                : DefaultDepartment;

        return this.Ok(new DograhTransferResolverResponse(
            new DograhTransferContext(
                Destination: $"Local/{department}@ride_hailing",
                CustomMessage: "Connecting you to a support agent now.")));
    }
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
