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

        // See RecordingService.ModifyRecordingAsync's own comment — SelectTelephonyCallByIdAsync
        // above already tracks an instance with this Id (EF's FindAsync); updating the
        // caller-supplied `telephonyCall` instead, a different object with the same key, throws
        // "cannot be tracked because another instance with the same key value... is already
        // being tracked." Copying onto the already-tracked instance avoids the conflict.
        maybeTelephonyCall!.CallerIdentityId = telephonyCall.CallerIdentityId;
        maybeTelephonyCall.RecipientIdentityId = telephonyCall.RecipientIdentityId;
        maybeTelephonyCall.Status = telephonyCall.Status;
        maybeTelephonyCall.AsteriskChannelId = telephonyCall.AsteriskChannelId;
        maybeTelephonyCall.AsteriskBridgeId = telephonyCall.AsteriskBridgeId;
        maybeTelephonyCall.TripId = telephonyCall.TripId;
        maybeTelephonyCall.StartedAt = telephonyCall.StartedAt;
        maybeTelephonyCall.EndedAt = telephonyCall.EndedAt;

        return await this.storageBroker.UpdateTelephonyCallAsync(maybeTelephonyCall);
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
