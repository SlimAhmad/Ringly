using Microsoft.EntityFrameworkCore;
using Ringly.Abstractions;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Services.Foundations.CallSessions;
using Ringly.Asterisk.Services.Foundations.SipEndpoints;
using Ringly.Asterisk.Services.Processings.Provisioning;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Asterisk.Services.Foundations.Queues;
using Ringly.Client.SipSorcery;
using Ringly.Samples.WebApi;
using Ringly.Samples.WebApi.Brokers.Storages;
using Ringly.Samples.WebApi.Services.Foundations.Recordings;
using Ringly.Samples.WebApi.Services.Foundations.SupportQueues;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyIdentities;
using Ringly.Storage.Abstractions;
using Ringly.Storage.AzureBlob.Services.Foundations.RecordingStorage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Matches docker/asterisk/config/ari.conf's dev credentials exactly — this sample works
// against `docker compose up -d` (see docker/README.md) with zero edits. Override via
// appsettings.Development.json or environment variables for a real deployment.
builder.Services.Configure<AsteriskOptions>(builder.Configuration.GetSection("Asterisk"));

builder.Services.AddSingleton<ILoggingBroker, AspNetLoggingBroker>();

// AsteriskBroker owns a persistent ARI WebSocket + AMI TCP connection — singleton, not
// scoped/transient (see docs/call-provider.md).
builder.Services.AddSingleton<IAsteriskBroker, AsteriskBroker>();

// SqlSipCredentialsStore depends on ITelephonyIdentityService, which depends on the Scoped
// IStorageBroker (a DbContext) — so this must be Scoped too, not Singleton like the
// InMemorySipCredentialsStore it replaces (a Singleton can't depend on a Scoped service).
builder.Services.AddScoped<ISipCredentialsStore, SqlSipCredentialsStore>();

// SqlQueueRegistry depends on ISupportQueueService, which depends on the Scoped IStorageBroker —
// so this must be Scoped too, not Singleton like the InMemoryQueueRegistry it replaces (same
// reasoning as ISipCredentialsStore's own registration above). Queues created via the UI/API now
// survive a WebApi restart instead of vanishing with the in-memory registry.
builder.Services.AddScoped<IQueueRegistry, SqlQueueRegistry>();
builder.Services.AddScoped<ISupportQueueService, SupportQueueService>();
builder.Services.AddScoped<IRecordingService, RecordingService>();

// Ringly.Storage.AzureBlob (see its own README/comments) — real Azure.Storage.Blobs-backed
// upload/download/delete/SAS-URL for a finished recording file. Points at Azurite (docker-compose)
// by default for local dev, see appsettings.json's AzureBlob section's own comment. Fully
// qualified below (not `using`'d) since Ringly.Storage.AzureBlob.Brokers.ILoggingBroker would
// otherwise collide with Ringly.Asterisk.Brokers.ILoggingBroker, already in scope from the
// `using Ringly.Asterisk.Brokers;` above.
builder.Services.Configure<Ringly.Storage.AzureBlob.Brokers.AzureBlobOptions>(
    builder.Configuration.GetSection("AzureBlob"));

builder.Services.Configure<RecordingSpoolOptions>(builder.Configuration.GetSection("RecordingSpool"));

builder.Services.AddSingleton<
    Ringly.Storage.AzureBlob.Brokers.ILoggingBroker, Ringly.Storage.AzureBlob.Brokers.LoggingBroker>();

builder.Services.AddSingleton<
    Ringly.Storage.AzureBlob.Brokers.IAzureBlobBroker, Ringly.Storage.AzureBlob.Brokers.AzureBlobBroker>();

builder.Services.AddSingleton<IRecordingStorageProvider, AzureBlobRecordingStorageProvider>();

// Ties SupportController's real customer-routing flow to AgentsController's broadcast/claim
// endpoints — see SupportQueueBroadcastRegistry's own comment for the full rationale.
builder.Services.AddSingleton<SupportQueueBroadcastRegistry>();

builder.Services.AddScoped<IAsteriskSipEndpointConfigFoundationService, AsteriskSipEndpointConfigFoundationService>();

// StorageBroker owns a DbContext — Scoped, not Singleton (DbContext isn't thread-safe for
// concurrent requests) and not Transient (a fresh EF change-tracker per request is standard,
// but per-injection-site would be wasteful/inconsistent within one request).
builder.Services.AddScoped<IStorageBroker, StorageBroker>();
builder.Services.AddScoped<ITelephonyIdentityService, TelephonyIdentityService>();
builder.Services.AddScoped<ITelephonyDeviceService, TelephonyDeviceService>();
builder.Services.AddScoped<ITelephonyCallService, TelephonyCallService>();
builder.Services.AddScoped<ICallProvisioningService, CallProvisioningService>();
builder.Services.AddScoped<ICallProvider, AsteriskCallFoundationService>();
builder.Services.AddScoped<ICallCenterProvider, AsteriskCallCenterFoundationService>();
builder.Services.AddScoped<ClientCredentialsService>();

// Bridges calls a client dials directly (not just server-originated ones via /calls) — see
// RideHailingCallRouter's own comment for why this is needed. Registered as a Singleton (not via
// the AddHostedService<T>() shorthand) so TelephonyCallTrackingService can also resolve it by its
// ICallLifecycleEventSource interface — AddHostedService<T>() alone only exposes T as IHostedService.
builder.Services.AddSingleton<RideHailingCallRouter>();
builder.Services.AddSingleton<ICallLifecycleEventSource>(sp => sp.GetRequiredService<RideHailingCallRouter>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<RideHailingCallRouter>());

// Writes/updates TelephonyCall rows as real calls progress — see TelephonyCallTrackingService's
// own comment for why this is a separate service rather than folded into RideHailingCallRouter.
builder.Services.AddHostedService<TelephonyCallTrackingService>();

// Uploads a recording to blob storage once it actually finishes, whether by explicit Stop or
// because the call just hung up on its own — see RecordingFinalizer's own comment for why this
// replaced the upload logic that used to live inline in RecordingsController.PostStopAsync.
builder.Services.AddHostedService<RecordingFinalizer>();

// See QueueTransferRegistrarService's own comment for why this exists — Dograh's native Call
// Transfer tool can only dial a real, registered SIP endpoint, so this WebApi process itself
// registers one. "localhost" — the WebApi runs on the same Windows host Asterisk's ports are
// published to, same as samples/Ringly.Samples.Maui's own Windows-platform RegistrarHost.
builder.Services.Configure<SipSorceryCallOptions>(options =>
{
    options.RegistrarHost = "localhost:5060";
    options.RegistrationExpirySeconds = 120;
});

builder.Services.AddHostedService<QueueTransferRegistrarService>();

var app = builder.Build();

// Moved out of StorageBroker's own constructor — a DbContext constructor calling Database.Migrate()
// blocks EF Core's design-time tooling outright: `dotnet ef migrations add` instantiates the
// context via its real constructor to read the current model, and once that model has ANY pending
// change (exactly the situation right before adding a new migration for it), Migrate() throws
// PendingModelChangesWarning as an error before the new migration can even be scaffolded — a
// chicken-and-egg problem confirmed live while adding the SupportQueues migration. Calling it once
// here instead, against a real DI scope, keeps the "auto-create schema on startup" behavior without
// ever running as a side effect of the design-time tooling itself.
using (IServiceScope migrationScope = app.Services.CreateScope())
{
    var storageBroker = (Ringly.Samples.WebApi.Brokers.Storages.StorageBroker)
        migrationScope.ServiceProvider.GetRequiredService<IStorageBroker>();

    storageBroker.Database.Migrate();
}

// The blob container isn't created automatically the first time a recording is uploaded — an
// upload against a missing container just 404s. Created once here at startup instead.
using (IServiceScope blobScope = app.Services.CreateScope())
{
    var azureBlobBroker = blobScope.ServiceProvider
        .GetRequiredService<Ringly.Storage.AzureBlob.Brokers.IAzureBlobBroker>();

    await azureBlobBroker.EnsureContainerExistsAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// Walks through the same flow as docs/getting-started.md, as real HTTP endpoints. Every route is
// now a controller — see Controllers/: ClientsController (credentials), QueuesController,
// CallsController, SupportController.

app.Run();
