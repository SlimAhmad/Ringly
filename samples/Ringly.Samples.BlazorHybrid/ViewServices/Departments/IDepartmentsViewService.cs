using Ringly.Samples.BlazorHybrid.Models.Departments;

namespace Ringly.Samples.BlazorHybrid.ViewServices.Departments;

// The single dependency DepartmentsPanel.razor (the Core Component) integrates with — mirrors
// ISupportViewService's own role for SupportPanel. Owns registering/listing/removing queues
// ("departments") so the Support panel's queue name field has something real to show instead of
// requiring the operator to already know an exact queue name up front.
public interface IDepartmentsViewService
{
    event Action? StateChanged;

    string NewDepartmentName { get; set; }
    string StatusMessage { get; }
    string StatusMessageColorClass { get; }
    bool IsBusy { get; }
    IReadOnlyList<DepartmentInfo> Departments { get; }

    ValueTask InitializeAsync();
    ValueTask CreateDepartmentAsync();
    ValueTask RemoveDepartmentAsync(string queueName);
}
