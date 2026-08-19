using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyDevices;

public interface ITelephonyDeviceService
{
    ValueTask<TelephonyDevice> AddTelephonyDeviceAsync(TelephonyDevice telephonyDevice);
    ValueTask<IQueryable<TelephonyDevice>> RetrieveAllTelephonyDevicesAsync();
    ValueTask<TelephonyDevice> RetrieveTelephonyDeviceByIdAsync(Guid telephonyDeviceId);
    ValueTask<IQueryable<TelephonyDevice>> RetrieveTelephonyDevicesByIdentityIdAsync(Guid identityId);
    ValueTask<TelephonyDevice> ModifyTelephonyDeviceAsync(TelephonyDevice telephonyDevice);
    ValueTask<TelephonyDevice> RemoveTelephonyDeviceByIdAsync(Guid telephonyDeviceId);
}
