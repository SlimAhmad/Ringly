using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Services.Foundations.CallSessions;
using Ringly.Asterisk.Services.Foundations.SipEndpoints;
using Ringly.Asterisk.Services.Processings.Provisioning;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Services.Foundations.Queues;
using Ringly.Samples.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Matches docker/asterisk/config/ari.conf's dev credentials exactly — this sample works
// against `docker compose up -d` (see docker/README.md) with zero edits. Override via
// appsettings.Development.json or environment variables for a real deployment.
builder.Services.Configure<AsteriskOptions>(builder.Configuration.GetSection("Asterisk"));

builder.Services.AddSingleton<ILoggingBroker, AspNetLoggingBroker>();

// AsteriskBroker owns a persistent ARI WebSocket + AMI TCP connection — singleton, not
// scoped/transient (see docs/call-provider.md).
builder.Services.AddSingleton<IAsteriskBroker, AsteriskBroker>();

builder.Services.AddSingleton<InMemorySipCredentialsStore>();
builder.Services.AddSingleton<ISipCredentialsStore>(sp => sp.GetRequiredService<InMemorySipCredentialsStore>());
builder.Services.AddSingleton<IQueueRegistry, InMemoryQueueRegistry>();

builder.Services.AddScoped<IAsteriskSipEndpointConfigFoundationService, AsteriskSipEndpointConfigFoundationService>();
builder.Services.AddScoped<ICallProvisioningService, CallProvisioningService>();
builder.Services.AddScoped<ICallProvider, AsteriskCallFoundationService>();
builder.Services.AddScoped<ICallCenterProvider, AsteriskCallCenterFoundationService>();

// Bridges calls a client dials directly (not just server-originated ones via /calls) — see
// RideHailingCallRouter's own comment for why this is needed.
builder.Services.AddHostedService<RideHailingCallRouter>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Walks through the same flow as docs/getting-started.md, as real HTTP endpoints.

app.MapPost("/clients/{clientId:guid}/provision", async (
    Guid clientId,
    ICallProvisioningService provisioningService,
    InMemorySipCredentialsStore credentialsStore) =>
{
    SipCredentials credentials = await provisioningService.AddClientCredentialsAsync(clientId);
    await credentialsStore.AddAsync(credentials);
    return Results.Ok(credentials);
})
.WithName("ProvisionClient");

app.MapPost("/queues", async (CreateQueueRequest request, ICallCenterProvider callCenterProvider) =>
{
    HoldingBridge holdingBridge = await callCenterProvider.CreateQueueAsync(
        new QueueConfig { Name = request.Name, MusicOnHoldClass = request.MusicOnHoldClass ?? string.Empty });

    return Results.Ok(holdingBridge);
})
.WithName("CreateQueue");

app.MapPost("/calls", async (StartCallRequest request, ICallProvider callProvider) =>
{
    CallSession session = await callProvider.StartCallSessionAsync(
        new CallParticipant { SipExtension = request.PartyAExtension },
        new CallParticipant { SipExtension = request.PartyBExtension });

    return Results.Ok(session);
})
.WithName("StartCall");

app.MapPost("/support/{clientId:guid}/route", async (
    Guid clientId,
    string queueName,
    ICallProvider callProvider) =>
{
    CallSession session = await callProvider.RouteToQueueAsync(clientId, queueName);
    return Results.Ok(session);
})
.WithName("RouteToQueue");

app.Run();

internal record CreateQueueRequest(string Name, string? MusicOnHoldClass);
internal record StartCallRequest(string PartyAExtension, string PartyBExtension);
