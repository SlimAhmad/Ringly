using Microsoft.EntityFrameworkCore;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Brokers.Storages;

public partial class StorageBroker
{
    public DbSet<TelephonyCall> TelephonyCalls { get; set; } = null!;

    public async ValueTask<TelephonyCall> InsertTelephonyCallAsync(TelephonyCall telephonyCall) =>
        await this.InsertAsync(telephonyCall);

    public async ValueTask<IQueryable<TelephonyCall>> SelectAllTelephonyCallsAsync() =>
        await this.SelectAllAsync<TelephonyCall>();

    public async ValueTask<TelephonyCall> SelectTelephonyCallByIdAsync(Guid telephonyCallId) =>
        await this.SelectAsync<TelephonyCall>(telephonyCallId);

    public async ValueTask<IQueryable<TelephonyCall>> SelectTelephonyCallsByCallerIdentityIdAsync(
        Guid callerIdentityId)
    {
        IQueryable<TelephonyCall> telephonyCalls = await this.SelectAllAsync<TelephonyCall>();

        return telephonyCalls.Where(call => call.CallerIdentityId == callerIdentityId);
    }

    public async ValueTask<TelephonyCall> UpdateTelephonyCallAsync(TelephonyCall telephonyCall) =>
        await this.UpdateAsync(telephonyCall);

    public async ValueTask<TelephonyCall> DeleteTelephonyCallAsync(TelephonyCall telephonyCall) =>
        await this.DeleteAsync(telephonyCall);

    private void ConfigureTelephonyCalls(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelephonyCall>(builder =>
        {
            builder.Property(call => call.Id).IsRequired();
            builder.Property(call => call.CallerIdentityId).IsRequired();
            builder.Property(call => call.RecipientIdentityId).IsRequired();
            builder.Property(call => call.Status).IsRequired();
            builder.Property(call => call.AsteriskChannelId).HasMaxLength(255);
            builder.Property(call => call.AsteriskBridgeId).HasMaxLength(255);

            // Restrict (not Cascade) on both — two FKs to the same table (TelephonyIdentity) can't
            // both cascade in SQL Server ("may cause cycles or multiple cascade paths"), and
            // preserving call history even after an identity is removed is the right behavior for
            // a CDR-style record anyway (unlike TelephonyDevice, which reasonably disappears with
            // its owning identity).
            builder
                .HasOne<TelephonyIdentity>()
                .WithMany()
                .HasForeignKey(call => call.CallerIdentityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne<TelephonyIdentity>()
                .WithMany()
                .HasForeignKey(call => call.RecipientIdentityId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
