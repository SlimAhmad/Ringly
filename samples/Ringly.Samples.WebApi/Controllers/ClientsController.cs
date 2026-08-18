using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;
using Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

namespace Ringly.Samples.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : RESTFulController
{
    private readonly ClientCredentialsService clientCredentialsService;

    public ClientsController(ClientCredentialsService clientCredentialsService) =>
        this.clientCredentialsService = clientCredentialsService;

    [HttpPost("{clientId:guid}/credentials")]
    public async ValueTask<ActionResult<SipCredentials>> PostCredentialsAsync(Guid clientId)
    {
        try
        {
            SipCredentials addedCredentials = await this.clientCredentialsService.AddAsync(clientId);

            return this.Created(value: addedCredentials);
        }
        catch (SipCredentialsValidationException sipCredentialsValidationException)
        {
            return this.BadRequest(sipCredentialsValidationException.InnerException);
        }
        catch (SipCredentialsDependencyValidationException sipCredentialsDependencyValidationException)
        {
            return this.BadRequest(sipCredentialsDependencyValidationException.InnerException);
        }
        catch (SipCredentialsDependencyException sipCredentialsDependencyException)
        {
            return this.InternalServerError(sipCredentialsDependencyException);
        }
        catch (SipCredentialsServiceException sipCredentialsServiceException)
        {
            return this.InternalServerError(sipCredentialsServiceException);
        }
    }

    [HttpGet("{clientId:guid}/credentials")]
    public async ValueTask<ActionResult<SipCredentials>> GetCredentialsAsync(Guid clientId)
    {
        SipCredentials? credentials = await this.clientCredentialsService.RetrieveByClientIdAsync(clientId);

        return credentials is null ? this.NotFound() : this.Ok(value: credentials);
    }

    [HttpDelete("{clientId:guid}/credentials")]
    public async ValueTask<ActionResult> DeleteCredentialsAsync(Guid clientId)
    {
        try
        {
            bool removed = await this.clientCredentialsService.RemoveByClientIdAsync(clientId);

            return removed ? this.Ok() : this.NotFound();
        }
        catch (SipCredentialsDependencyValidationException sipCredentialsDependencyValidationException)
            when (sipCredentialsDependencyValidationException.InnerException?.InnerException is ExtensionNotFoundException)
        {
            // Three levels deep: SipCredentialsDependencyValidationException (processing layer)
            // wraps SipEndpointConfigDependencyValidationException (foundation layer), which
            // wraps the real ExtensionNotFoundException — one level deeper than a foundation
            // service's own outer exceptions, since CallProvisioningService wraps an
            // already-categorized foundation-layer exception rather than a raw native one.
            return this.NotFound(sipCredentialsDependencyValidationException.InnerException.InnerException);
        }
        catch (SipCredentialsValidationException sipCredentialsValidationException)
        {
            return this.BadRequest(sipCredentialsValidationException.InnerException);
        }
        catch (SipCredentialsDependencyValidationException sipCredentialsDependencyValidationException)
        {
            return this.BadRequest(sipCredentialsDependencyValidationException.InnerException);
        }
        catch (SipCredentialsDependencyException sipCredentialsDependencyException)
        {
            return this.InternalServerError(sipCredentialsDependencyException);
        }
        catch (SipCredentialsServiceException sipCredentialsServiceException)
        {
            return this.InternalServerError(sipCredentialsServiceException);
        }
    }
}
