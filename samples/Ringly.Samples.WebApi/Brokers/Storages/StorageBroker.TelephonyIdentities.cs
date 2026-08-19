using Microsoft.EntityFrameworkCore;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Brokers.Storages;

public partial class StorageBroker
{
    public DbSet<TelephonyIdentity> TelephonyIdentities { get; set; } = null!;

    public async ValueTask<TelephonyIdentity> InsertTelephonyIdentityAsync(TelephonyIdentity telephonyIdentity) =>
        await this.InsertAsync(telephonyIdentity);

    public async ValueTask<IQueryable<TelephonyIdentity>> SelectAllTelephonyIdentitiesAsync() =>
        await this.SelectAllAsync<TelephonyIdentity>();

    public async ValueTask<TelephonyIdentity> SelectTelephonyIdentityByIdAsync(Guid telephonyIdentityId) =>
        await this.SelectAsync<TelephonyIdentity>(telephonyIdentityId);

    public async ValueTask<TelephonyIdentity?> SelectTelephonyIdentityByUserIdAsync(Guid userId)
    {
        IQueryable<TelephonyIdentity> telephonyIdentities = await this.SelectAllAsync<TelephonyIdentity>();

        return telephonyIdentities.FirstOrDefault(identity => identity.UserId == userId);
    }

    public async ValueTask<TelephonyIdentity?> SelectTelephonyIdentityBySipUsernameAsync(string sipUsername)
    {
        IQueryable<TelephonyIdentity> telephonyIdentities = await this.SelectAllAsync<TelephonyIdentity>();

        return telephonyIdentities.FirstOrDefault(identity => identity.SipUsername == sipUsername);
    }

    public async ValueTask<TelephonyIdentity> UpdateTelephonyIdentityAsync(TelephonyIdentity telephonyIdentity) =>
        await this.UpdateAsync(telephonyIdentity);

    public async ValueTask<TelephonyIdentity> DeleteTelephonyIdentityAsync(TelephonyIdentity telephonyIdentity) =>
        await this.DeleteAsync(telephonyIdentity);

    private void ConfigureTelephonyIdentities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelephonyIdentity>(builder =>
        {
            builder.Property(identity => identity.Id).IsRequired();
            builder.Property(identity => identity.UserId).IsRequired();
            builder.Property(identity => identity.SipUsername).HasMaxLength(255).IsRequired();
            builder.Property(identity => identity.SipCredential).HasMaxLength(255).IsRequired();
            builder.Property(identity => identity.Type).IsRequired();
            builder.Property(identity => identity.Status).IsRequired();

            // UserId is a bare Guid, not a foreign key — this sample deliberately has no User
            // table (full user management is out of scope), so UserId is just an opaque
            // identifier a real app's own user system would supply. Still indexed/unique since a
            // given app user should only ever have one telephony identity of a given type.
            builder.HasIndex(identity => new { identity.UserId, identity.Type }).IsUnique();
        });
    }
}
