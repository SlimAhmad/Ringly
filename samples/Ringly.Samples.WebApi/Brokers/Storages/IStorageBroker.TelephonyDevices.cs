using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;

namespace Ringly.Samples.WebApi.Brokers.Storages;

public partial interface IStorageBroker
{
    ValueTask<TelephonyDevice> InsertTelephonyDeviceAsync(TelephonyDevice telephonyDevice);
    ValueTask<IQueryable<TelephonyDevice>> SelectAllTelephonyDevicesAsync();
    ValueTask<TelephonyDevice> SelectTelephonyDeviceByIdAsync(Guid telephonyDeviceId);
    ValueTask<IQueryable<TelephonyDevice>> SelectTelephonyDevicesByIdentityIdAsync(Guid identityId);
    ValueTask<TelephonyDevice> UpdateTelephonyDeviceAsync(TelephonyDevice telephonyDevice);
    ValueTask<TelephonyDevice> DeleteTelephonyDeviceAsync(TelephonyDevice telephonyDevice);
}
