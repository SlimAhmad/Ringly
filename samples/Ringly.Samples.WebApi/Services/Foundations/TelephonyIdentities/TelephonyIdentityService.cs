using Ringly.Asterisk.Brokers;
using Ringly.Samples.WebApi.Brokers.Storages;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityService : ITelephonyIdentityService
{
    private readonly IStorageBroker storageBroker;
    private readonly ILoggingBroker loggingBroker;

    public TelephonyIdentityService(IStorageBroker storageBroker, ILoggingBroker loggingBroker)
    {
        this.storageBroker = storageBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<TelephonyIdentity> AddTelephonyIdentityAsync(TelephonyIdentity telephonyIdentity) =>
    TryCatch(async () =>
    {
        ValidateTelephonyIdentityOnAdd(telephonyIdentity);

        return await this.storageBroker.InsertTelephonyIdentityAsync(telephonyIdentity);
    });

    public ValueTask<IQueryable<TelephonyIdentity>> RetrieveAllTelephonyIdentitiesAsync() =>
    TryCatch(async () => await this.storageBroker.SelectAllTelephonyIdentitiesAsync());

    public ValueTask<TelephonyIdentity> RetrieveTelephonyIdentityByIdAsync(Guid telephonyIdentityId) =>
    TryCatch(async () =>
    {
        ValidateTelephonyIdentityId(telephonyIdentityId);

        TelephonyIdentity? maybeTelephonyIdentity =
            await this.storageBroker.SelectTelephonyIdentityByIdAsync(telephonyIdentityId);

        ValidateStorageTelephonyIdentityExists(maybeTelephonyIdentity, telephonyIdentityId);

        return maybeTelephonyIdentity!;
    });

    public ValueTask<TelephonyIdentity?> RetrieveTelephonyIdentityByUserIdAsync(Guid userId) =>
    TryCatchNullable(async () =>
    {
        ValidateUserId(userId);

        return await this.storageBroker.SelectTelephonyIdentityByUserIdAsync(userId);
    });

    public ValueTask<TelephonyIdentity?> RetrieveTelephonyIdentityBySipUsernameAsync(string sipUsername) =>
    TryCatchNullable(async () =>
    {
        ValidateSipUsername(sipUsername);

        return await this.storageBroker.SelectTelephonyIdentityBySipUsernameAsync(sipUsername);
    });

    public ValueTask<TelephonyIdentity> ModifyTelephonyIdentityAsync(TelephonyIdentity telephonyIdentity) =>
    TryCatch(async () =>
    {
        ValidateTelephonyIdentityOnModify(telephonyIdentity);

        TelephonyIdentity? maybeTelephonyIdentity =
            await this.storageBroker.SelectTelephonyIdentityByIdAsync(telephonyIdentity.Id);

        ValidateStorageTelephonyIdentityExists(maybeTelephonyIdentity, telephonyIdentity.Id);

        // See RecordingService.ModifyRecordingAsync's own comment —
        // SelectTelephonyIdentityByIdAsync above already tracks an instance with this Id (EF's
        // FindAsync); updating the caller-supplied `telephonyIdentity` instead, a different
        // object with the same key, throws "cannot be tracked because another instance with the
        // same key value... is already being tracked." Copying onto the already-tracked instance
        // avoids the conflict.
        maybeTelephonyIdentity!.UserId = telephonyIdentity.UserId;
        maybeTelephonyIdentity.SipUsername = telephonyIdentity.SipUsername;
        maybeTelephonyIdentity.SipCredential = telephonyIdentity.SipCredential;
        maybeTelephonyIdentity.Type = telephonyIdentity.Type;
        maybeTelephonyIdentity.Status = telephonyIdentity.Status;

        return await this.storageBroker.UpdateTelephonyIdentityAsync(maybeTelephonyIdentity);
    });

    public ValueTask<TelephonyIdentity> RemoveTelephonyIdentityByIdAsync(Guid telephonyIdentityId) =>
    TryCatch(async () =>
    {
        ValidateTelephonyIdentityId(telephonyIdentityId);

        TelephonyIdentity? maybeTelephonyIdentity =
            await this.storageBroker.SelectTelephonyIdentityByIdAsync(telephonyIdentityId);

        ValidateStorageTelephonyIdentityExists(maybeTelephonyIdentity, telephonyIdentityId);

        return await this.storageBroker.DeleteTelephonyIdentityAsync(maybeTelephonyIdentity!);
    });
}
