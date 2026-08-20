using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;

namespace Ringly.Samples.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueuesController : RESTFulController
{
    private readonly ICallCenterProvider callCenterProvider;
    private readonly IQueueRegistry queueRegistry;

    public QueuesController(ICallCenterProvider callCenterProvider, IQueueRegistry queueRegistry)
    {
        this.callCenterProvider = callCenterProvider;
        this.queueRegistry = queueRegistry;
    }

    [HttpPost]
    public async ValueTask<ActionResult<HoldingBridge>> PostQueueAsync(CreateQueueRequest request)
    {
        try
        {
            HoldingBridge holdingBridge = await this.callCenterProvider.CreateQueueAsync(
                new QueueConfig { Name = request.Name, MusicOnHoldClass = request.MusicOnHoldClass ?? string.Empty });

            return this.Created(value: holdingBridge);
        }
        catch (QueueConfigValidationException queueConfigValidationException)
        {
            return this.BadRequest(queueConfigValidationException.InnerException);
        }
        catch (QueueConfigDependencyValidationException queueConfigDependencyValidationException)
        {
            return this.BadRequest(queueConfigDependencyValidationException.InnerException);
        }
        catch (QueueConfigDependencyException queueConfigDependencyException)
        {
            return this.InternalServerError(queueConfigDependencyException);
        }
        catch (QueueConfigServiceException queueConfigServiceException)
        {
            return this.InternalServerError(queueConfigServiceException);
        }
    }

    // Read-only, purely a UI/API convenience — no business logic to wrap, so IQueueRegistry (an
    // app-level persistence contract, see docs/call-center.md) is injected directly rather than
    // through ICallCenterProvider, the same precedent SupportQueueBroadcastRegistry established
    // for direct app-level registry access from a controller.
    [HttpGet]
    public async ValueTask<ActionResult<IReadOnlyList<HoldingBridge>>> GetQueuesAsync() =>
        this.Ok(await this.queueRegistry.RetrieveAllAsync());

    [HttpDelete("{queueName}")]
    public async ValueTask<ActionResult> DeleteQueueAsync(string queueName)
    {
        await this.queueRegistry.RemoveAsync(queueName);

        return this.Ok();
    }
}

public record CreateQueueRequest(string Name, string? MusicOnHoldClass);
