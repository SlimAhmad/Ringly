using Microsoft.EntityFrameworkCore;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Brokers.Storages;

public partial class StorageBroker
{
    public DbSet<TelephonyDevice> TelephonyDevices { get; set; } = null!;

    public async ValueTask<TelephonyDevice> InsertTelephonyDeviceAsync(TelephonyDevice telephonyDevice) =>
        await this.InsertAsync(telephonyDevice);

    public async ValueTask<IQueryable<TelephonyDevice>> SelectAllTelephonyDevicesAsync() =>
        await this.SelectAllAsync<TelephonyDevice>();

    public async ValueTask<TelephonyDevice> SelectTelephonyDeviceByIdAsync(Guid telephonyDeviceId) =>
        await this.SelectAsync<TelephonyDevice>(telephonyDeviceId);

    public async ValueTask<IQueryable<TelephonyDevice>> SelectTelephonyDevicesByIdentityIdAsync(Guid identityId)
    {
        IQueryable<TelephonyDevice> telephonyDevices = await this.SelectAllAsync<TelephonyDevice>();

        return telephonyDevices.Where(device => device.IdentityId == identityId);
    }

    public async ValueTask<TelephonyDevice> UpdateTelephonyDeviceAsync(TelephonyDevice telephonyDevice) =>
        await this.UpdateAsync(telephonyDevice);

    public async ValueTask<TelephonyDevice> DeleteTelephonyDeviceAsync(TelephonyDevice telephonyDevice) =>
        await this.DeleteAsync(telephonyDevice);

    private void ConfigureTelephonyDevices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelephonyDevice>(builder =>
        {
            builder.Property(device => device.Id).IsRequired();
            builder.Property(device => device.IdentityId).IsRequired();
            builder.Property(device => device.Platform).HasMaxLength(50).IsRequired();
            builder.Property(device => device.IsOnline).IsRequired();

            // Real foreign key, unlike TelephonyIdentity.UserId — IdentityId must reference a
            // real TelephonyIdentity row (a device without an owning identity makes no sense),
            // whereas UserId deliberately has no local table to reference (see TelephonyIdentity's
            // own configuration comment).
            builder
                .HasOne<TelephonyIdentity>()
                .WithMany()
                .HasForeignKey(device => device.IdentityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
