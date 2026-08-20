using EFxceptions;
using Microsoft.EntityFrameworkCore;

namespace Ringly.Samples.WebApi.Brokers.Storages;

// This sample's own application data (TelephonyIdentity/TelephonyDevice/TelephonyCall) — a
// separate SQL Server database from Postgres, which keeps backing Asterisk's realtime PJSIP
// config exactly as before (see Ringly.Asterisk/Brokers/AsteriskBroker.Credentials.cs). No
// DbSet<> properties or entity-specific members live in this base partial — each entity gets its
// own partial file (StorageBroker.TelephonyIdentities.cs, etc.), matching this repo's existing
// partial-broker convention (e.g. AsteriskBroker.Credentials.cs/.Channels.cs/.Stasis.cs).
public partial class StorageBroker : EFxceptionsContext, IStorageBroker
{
    private readonly IConfiguration configuration;

    public StorageBroker(IConfiguration configuration) =>
        this.configuration = configuration;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connectionString = this.configuration.GetConnectionString("DefaultConnection")!;
        optionsBuilder.UseSqlServer(connectionString);
    }

    // Calls into each entity partial's own Configure{Entity} method (e.g.
    // StorageBroker.TelephonyIdentities.cs) — kept here, not split per-entity, since this is the
    // one place EF Core requires a single ModelBuilder pass across all entities.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        this.ConfigureTelephonyIdentities(modelBuilder);
        this.ConfigureTelephonyDevices(modelBuilder);
        this.ConfigureTelephonyCalls(modelBuilder);
        this.ConfigureSupportQueues(modelBuilder);
    }

    private async ValueTask<T> InsertAsync<T>(T entity) where T : class
    {
        this.Entry(entity).State = EntityState.Added;
        await this.SaveChangesAsync();

        return entity;
    }

    private ValueTask<IQueryable<T>> SelectAllAsync<T>() where T : class =>
        ValueTask.FromResult<IQueryable<T>>(this.Set<T>().AsNoTracking());

    private async ValueTask<T> SelectAsync<T>(params object[] entityIds) where T : class =>
        (await this.FindAsync<T>(entityIds))!;

    private async ValueTask<T> UpdateAsync<T>(T entity) where T : class
    {
        this.Entry(entity).State = EntityState.Modified;
        await this.SaveChangesAsync();

        return entity;
    }

    private async ValueTask<T> DeleteAsync<T>(T entity) where T : class
    {
        this.Entry(entity).State = EntityState.Deleted;
        await this.SaveChangesAsync();

        return entity;
    }
}
