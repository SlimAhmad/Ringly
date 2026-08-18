using System.Security.Cryptography;
using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Services.Foundations.SipEndpoints;

namespace Ringly.Asterisk.Services.Processings.Provisioning;

public partial class CallProvisioningService : ICallProvisioningService
{
    private const string PasswordChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int PasswordLength = 24;
    private const int ExtensionMinValue = 100000;
    private const int ExtensionMaxValue = 999999;

    private readonly IAsteriskSipEndpointConfigFoundationService sipEndpointConfigFoundationService;
    private readonly ILoggingBroker loggingBroker;

    public CallProvisioningService(
        IAsteriskSipEndpointConfigFoundationService sipEndpointConfigFoundationService,
        ILoggingBroker loggingBroker)
    {
        this.sipEndpointConfigFoundationService = sipEndpointConfigFoundationService;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<SipCredentials> AddClientCredentialsAsync(Guid clientId) =>
    TryCatch(async () =>
    {
        ValidateClientId(clientId);

        string extension = GenerateExtension();
        string password = GeneratePassword();

        await this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(
            new SipEndpointConfig { Extension = extension, Password = password });

        return new SipCredentials
        {
            ClientId = clientId,
            Extension = extension,
            Password = password
        };
    });

    public ValueTask RemoveClientCredentialsAsync(string extension) =>
    TryCatch(async () =>
    {
        ValidateExtension(extension);

        await this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(extension);
    });

    private static string GenerateExtension() =>
        RandomNumberGenerator.GetInt32(ExtensionMinValue, ExtensionMaxValue + 1).ToString();

    private static string GeneratePassword() =>
        RandomNumberGenerator.GetString(PasswordChars, PasswordLength);
}
