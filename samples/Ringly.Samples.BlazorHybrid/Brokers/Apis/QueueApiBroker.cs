using Microsoft.Extensions.Options;
using RESTFulSense.Clients;
using Ringly.Samples.BlazorHybrid.Models.Departments;

namespace Ringly.Samples.BlazorHybrid.Brokers.Apis;

public class QueueApiBroker : IQueueApiBroker
{
    private readonly IRESTFulApiFactoryClient apiClient;

    // Same base URL as SupportApiBroker/AgentConsoleApiBroker (Ringly.Samples.WebApi) — reuses
    // SupportApiOptions rather than a duplicate options type, since all three brokers target the
    // same server.
    public QueueApiBroker(IOptions<SupportApiOptions> options)
    {
        var httpClient = new HttpClient(new SocketsHttpHandler())
        {
            BaseAddress = new Uri(options.Value.BaseUrl)
        };

        this.apiClient = new RESTFulApiFactoryClient(httpClient);
    }

    public async ValueTask<IReadOnlyList<DepartmentInfo>> GetDepartmentsAsync() =>
        await this.apiClient.GetContentAsync<List<DepartmentInfo>>(relativeUrl: "api/queues");

    public async ValueTask<DepartmentInfo> PostDepartmentAsync(string queueName) =>
        await this.apiClient.PostContentAsync<object, DepartmentInfo>(
            relativeUrl: "api/queues",
            content: new { name = queueName },
            mediaType: "application/json");

    public async ValueTask DeleteDepartmentAsync(string queueName) =>
        await this.apiClient.DeleteContentAsync($"api/queues/{Uri.EscapeDataString(queueName)}");
}
