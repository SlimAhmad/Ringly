using Ringly.Samples.BlazorHybrid.Models.Departments;

namespace Ringly.Samples.BlazorHybrid.Brokers.Apis;

// Liaison between this app and Ringly.Samples.WebApi's QueuesController — per
// the-standard-architecture's broker rules, no business logic here, just the HTTP calls
// themselves. Separate from ISupportApiBroker/IAgentConsoleApiBroker: queues ("departments") are
// their own resource, not owned by either the support-routing or agent-console flows.
public interface IQueueApiBroker
{
    ValueTask<IReadOnlyList<DepartmentInfo>> GetDepartmentsAsync();
    ValueTask<DepartmentInfo> PostDepartmentAsync(string queueName);
    ValueTask DeleteDepartmentAsync(string queueName);
}
