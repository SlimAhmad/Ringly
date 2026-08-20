using RESTFulSense.Clients;
using Ringly.Samples.BlazorServer.Models.Departments;

namespace Ringly.Samples.BlazorServer.Brokers.Apis;

public class QueueApiBroker : IQueueApiBroker
{
    private readonly IRESTFulApiFactoryClient apiClient;

    public QueueApiBroker(IConfiguration configuration)
    {
        string baseUrl = configuration.GetValue<string>("WebApiClient:BaseUrl") ?? "http://localhost:5000";

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
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
