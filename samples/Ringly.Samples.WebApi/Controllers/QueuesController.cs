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

    public QueuesController(ICallCenterProvider callCenterProvider) =>
        this.callCenterProvider = callCenterProvider;

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
}

public record CreateQueueRequest(string Name, string? MusicOnHoldClass);
