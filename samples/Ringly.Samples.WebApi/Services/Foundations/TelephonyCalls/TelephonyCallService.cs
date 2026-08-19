using Ringly.Asterisk.Brokers;
using Ringly.Samples.WebApi.Brokers.Storages;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallService : ITelephonyCallService
{
    private readonly IStorageBroker storageBroker;
    private readonly ILoggingBroker loggingBroker;

    public TelephonyCallService(IStorageBroker storageBroker, ILoggingBroker loggingBroker)
    {
        this.storageBroker = storageBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<TelephonyCall> AddTelephonyCallAsync(TelephonyCall telephonyCall) =>
    TryCatch(async () =>
    {
        ValidateTelephonyCallOnAdd(telephonyCall);

        return await this.storageBroker.InsertTelephonyCallAsync(telephonyCall);
    });

    public ValueTask<IQueryable<TelephonyCall>> RetrieveAllTelephonyCallsAsync() =>
    TryCatch(async () => await this.storageBroker.SelectAllTelephonyCallsAsync());

    public ValueTask<TelephonyCall> RetrieveTelephonyCallByIdAsync(Guid telephonyCallId) =>
    TryCatch(async () =>
    {
        ValidateTelephonyCallId(telephonyCallId);

        TelephonyCall? maybeTelephonyCall =
            await this.storageBroker.SelectTelephonyCallByIdAsync(telephonyCallId);

        ValidateStorageTelephonyCallExists(maybeTelephonyCall, telephonyCallId);

        return maybeTelephonyCall!;
    });

    public ValueTask<IQueryable<TelephonyCall>> RetrieveTelephonyCallsByCallerIdentityIdAsync(
        Guid callerIdentityId) =>
    TryCatch(async () =>
    {
        ValidateCallerIdentityId(callerIdentityId);

        return await this.storageBroker.SelectTelephonyCallsByCallerIdentityIdAsync(callerIdentityId);
    });

    public ValueTask<TelephonyCall> ModifyTelephonyCallAsync(TelephonyCall telephonyCall) =>
    TryCatch(async () =>
    {
        ValidateTelephonyCallOnModify(telephonyCall);

        TelephonyCall? maybeTelephonyCall =
            await this.storageBroker.SelectTelephonyCallByIdAsync(telephonyCall.Id);

        ValidateStorageTelephonyCallExists(maybeTelephonyCall, telephonyCall.Id);

        return await this.storageBroker.UpdateTelephonyCallAsync(telephonyCall);
    });

    public ValueTask<TelephonyCall> RemoveTelephonyCallByIdAsync(Guid telephonyCallId) =>
    TryCatch(async () =>
    {
        ValidateTelephonyCallId(telephonyCallId);

        TelephonyCall? maybeTelephonyCall =
            await this.storageBroker.SelectTelephonyCallByIdAsync(telephonyCallId);

        ValidateStorageTelephonyCallExists(maybeTelephonyCall, telephonyCallId);

        return await this.storageBroker.DeleteTelephonyCallAsync(maybeTelephonyCall!);
    });
}
