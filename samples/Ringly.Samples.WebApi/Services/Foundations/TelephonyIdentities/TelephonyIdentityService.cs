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

        return await this.storageBroker.UpdateTelephonyIdentityAsync(telephonyIdentity);
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
