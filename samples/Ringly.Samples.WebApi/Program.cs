using Ringly.Abstractions;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Services.Foundations.CallSessions;
using Ringly.Asterisk.Services.Foundations.SipEndpoints;
using Ringly.Asterisk.Services.Processings.Provisioning;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Asterisk.Services.Foundations.Queues;
using Ringly.Samples.WebApi;
using Ringly.Samples.WebApi.Brokers.Storages;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyIdentities;

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
builder.Services.AddSingleton<IQueueRegistry, InMemoryQueueRegistry>();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// Walks through the same flow as docs/getting-started.md, as real HTTP endpoints. Every route is
// now a controller — see Controllers/: ClientsController (credentials), QueuesController,
// CallsController, SupportController.

app.Run();
