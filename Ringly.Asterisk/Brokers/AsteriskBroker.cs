using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker : IAsteriskBroker
{
    private readonly HttpClient ariClient;

    public AsteriskBroker(IOptions<AsteriskOptions> options)
    {
        AsteriskOptions asteriskOptions = options.Value;

        this.ariClient = new HttpClient
        {
            BaseAddress = new Uri(asteriskOptions.BaseUrl)
        };

        string credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{asteriskOptions.Username}:{asteriskOptions.Password}"));

        this.ariClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
    }

    private async ValueTask<T> PostAsync<T>(string relativeUrl)
    {
        HttpResponseMessage response = await this.ariClient.PostAsync(relativeUrl, content: null);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async ValueTask PostAsync(string relativeUrl)
    {
        HttpResponseMessage response = await this.ariClient.PostAsync(relativeUrl, content: null);
        response.EnsureSuccessStatusCode();
    }

    private async ValueTask DeleteAsync(string relativeUrl)
    {
        HttpResponseMessage response = await this.ariClient.DeleteAsync(relativeUrl);
        response.EnsureSuccessStatusCode();
    }
}
