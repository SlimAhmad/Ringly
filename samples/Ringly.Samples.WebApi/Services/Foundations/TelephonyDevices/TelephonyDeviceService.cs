using Ringly.Asterisk.Brokers;
using Ringly.Samples.WebApi.Brokers.Storages;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceService : ITelephonyDeviceService
{
    private readonly IStorageBroker storageBroker;
    private readonly ILoggingBroker loggingBroker;

    public TelephonyDeviceService(IStorageBroker storageBroker, ILoggingBroker loggingBroker)
    {
        this.storageBroker = storageBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<TelephonyDevice> AddTelephonyDeviceAsync(TelephonyDevice telephonyDevice) =>
    TryCatch(async () =>
    {
        ValidateTelephonyDeviceOnAdd(telephonyDevice);

        return await this.storageBroker.InsertTelephonyDeviceAsync(telephonyDevice);
    });

    public ValueTask<IQueryable<TelephonyDevice>> RetrieveAllTelephonyDevicesAsync() =>
    TryCatch(async () => await this.storageBroker.SelectAllTelephonyDevicesAsync());

    public ValueTask<TelephonyDevice> RetrieveTelephonyDeviceByIdAsync(Guid telephonyDeviceId) =>
    TryCatch(async () =>
    {
        ValidateTelephonyDeviceId(telephonyDeviceId);

        TelephonyDevice? maybeTelephonyDevice =
            await this.storageBroker.SelectTelephonyDeviceByIdAsync(telephonyDeviceId);

        ValidateStorageTelephonyDeviceExists(maybeTelephonyDevice, telephonyDeviceId);

        return maybeTelephonyDevice!;
    });

    public ValueTask<IQueryable<TelephonyDevice>> RetrieveTelephonyDevicesByIdentityIdAsync(Guid identityId) =>
    TryCatch(async () =>
    {
        ValidateIdentityId(identityId);

        return await this.storageBroker.SelectTelephonyDevicesByIdentityIdAsync(identityId);
    });

    public ValueTask<TelephonyDevice> ModifyTelephonyDeviceAsync(TelephonyDevice telephonyDevice) =>
    TryCatch(async () =>
    {
        ValidateTelephonyDeviceOnModify(telephonyDevice);

        TelephonyDevice? maybeTelephonyDevice =
            await this.storageBroker.SelectTelephonyDeviceByIdAsync(telephonyDevice.Id);

        ValidateStorageTelephonyDeviceExists(maybeTelephonyDevice, telephonyDevice.Id);

        return await this.storageBroker.UpdateTelephonyDeviceAsync(telephonyDevice);
    });

    public ValueTask<TelephonyDevice> RemoveTelephonyDeviceByIdAsync(Guid telephonyDeviceId) =>
    TryCatch(async () =>
    {
        ValidateTelephonyDeviceId(telephonyDeviceId);

        TelephonyDevice? maybeTelephonyDevice =
            await this.storageBroker.SelectTelephonyDeviceByIdAsync(telephonyDeviceId);

        ValidateStorageTelephonyDeviceExists(maybeTelephonyDevice, telephonyDeviceId);

        return await this.storageBroker.DeleteTelephonyDeviceAsync(maybeTelephonyDevice!);
    });
}
