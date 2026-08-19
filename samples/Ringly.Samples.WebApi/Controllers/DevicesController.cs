using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyDevices;

namespace Ringly.Samples.WebApi.Controllers;

// Nested under identities (api/identities/{identityId}/devices), not its own api/[controller] root
// — a device only ever exists in relation to the identity that owns it (see
// StorageBroker.TelephonyDevices.cs's real foreign key), matching SupportController's precedent
// for nested-resource routes.
[ApiController]
[Route("api/identities/{identityId:guid}/devices")]
public class DevicesController : RESTFulController
{
    private readonly ITelephonyDeviceService telephonyDeviceService;

    public DevicesController(ITelephonyDeviceService telephonyDeviceService) =>
        this.telephonyDeviceService = telephonyDeviceService;

    [HttpPost]
    public async ValueTask<ActionResult<TelephonyDevice>> PostDeviceAsync(
        Guid identityId, RegisterDeviceRequest request)
    {
        try
        {
            var telephonyDevice = new TelephonyDevice
            {
                Id = Guid.NewGuid(),
                IdentityId = identityId,
                Platform = request.Platform,
                IsOnline = true,
                LastRegisteredAt = DateTimeOffset.UtcNow
            };

            TelephonyDevice addedDevice = await this.telephonyDeviceService.AddTelephonyDeviceAsync(telephonyDevice);

            return this.Created(value: addedDevice);
        }
        catch (TelephonyDeviceValidationException telephonyDeviceValidationException)
        {
            return this.BadRequest(telephonyDeviceValidationException.InnerException);
        }
        catch (TelephonyDeviceDependencyValidationException telephonyDeviceDependencyValidationException)
            when (telephonyDeviceDependencyValidationException.InnerException is InvalidReferenceTelephonyDeviceException)
        {
            // IdentityId doesn't reference a real TelephonyIdentity — a 404 on the parent resource
            // reads more correctly to a REST consumer than a generic 400, same distinction
            // ClientsController/SupportController already draw for their own not-found cases.
            return this.NotFound(telephonyDeviceDependencyValidationException.InnerException);
        }
        catch (TelephonyDeviceDependencyValidationException telephonyDeviceDependencyValidationException)
        {
            return this.BadRequest(telephonyDeviceDependencyValidationException.InnerException);
        }
        catch (TelephonyDeviceDependencyException telephonyDeviceDependencyException)
        {
            return this.InternalServerError(telephonyDeviceDependencyException);
        }
        catch (TelephonyDeviceServiceException telephonyDeviceServiceException)
        {
            return this.InternalServerError(telephonyDeviceServiceException);
        }
    }

    [HttpGet]
    public async ValueTask<ActionResult<IQueryable<TelephonyDevice>>> GetDevicesAsync(Guid identityId)
    {
        try
        {
            IQueryable<TelephonyDevice> devices =
                await this.telephonyDeviceService.RetrieveTelephonyDevicesByIdentityIdAsync(identityId);

            return this.Ok(devices);
        }
        catch (TelephonyDeviceValidationException telephonyDeviceValidationException)
        {
            return this.BadRequest(telephonyDeviceValidationException.InnerException);
        }
        catch (TelephonyDeviceDependencyException telephonyDeviceDependencyException)
        {
            return this.InternalServerError(telephonyDeviceDependencyException);
        }
        catch (TelephonyDeviceServiceException telephonyDeviceServiceException)
        {
            return this.InternalServerError(telephonyDeviceServiceException);
        }
    }

    [HttpDelete("{deviceId:guid}")]
    public async ValueTask<ActionResult> DeleteDeviceAsync(Guid identityId, Guid deviceId)
    {
        try
        {
            await this.telephonyDeviceService.RemoveTelephonyDeviceByIdAsync(deviceId);

            return this.Ok();
        }
        catch (TelephonyDeviceValidationException telephonyDeviceValidationException)
            when (telephonyDeviceValidationException.InnerException is NotFoundTelephonyDeviceException)
        {
            return this.NotFound(telephonyDeviceValidationException.InnerException);
        }
        catch (TelephonyDeviceValidationException telephonyDeviceValidationException)
        {
            return this.BadRequest(telephonyDeviceValidationException.InnerException);
        }
        catch (TelephonyDeviceDependencyValidationException telephonyDeviceDependencyValidationException)
        {
            return this.BadRequest(telephonyDeviceDependencyValidationException.InnerException);
        }
        catch (TelephonyDeviceDependencyException telephonyDeviceDependencyException)
        {
            return this.InternalServerError(telephonyDeviceDependencyException);
        }
        catch (TelephonyDeviceServiceException telephonyDeviceServiceException)
        {
            return this.InternalServerError(telephonyDeviceServiceException);
        }
    }
}

public record RegisterDeviceRequest(string Platform);
