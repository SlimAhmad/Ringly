using Ringly.Samples.BlazorHybrid.Brokers.Apis;
using Ringly.Samples.BlazorHybrid.Models.Departments;

namespace Ringly.Samples.BlazorHybrid.ViewServices.Departments;

public sealed class DepartmentsViewService : IDepartmentsViewService
{
    private readonly IQueueApiBroker queueApiBroker;
    private List<DepartmentInfo> departments = [];

    public event Action? StateChanged;

    public string NewDepartmentName { get; set; } = string.Empty;
    public string StatusMessage { get; private set; } = string.Empty;
    public string StatusMessageColorClass { get; private set; } = string.Empty;
    public bool IsBusy { get; private set; }

    public IReadOnlyList<DepartmentInfo> Departments => this.departments;

    public DepartmentsViewService(IQueueApiBroker queueApiBroker) =>
        this.queueApiBroker = queueApiBroker;

    public async ValueTask InitializeAsync() => await this.LoadDepartmentsAsync();

    public async ValueTask CreateDepartmentAsync()
    {
        if (string.IsNullOrWhiteSpace(this.NewDepartmentName))
        {
            return;
        }

        this.IsBusy = true;
        this.OnStateChanged();

        try
        {
            await this.queueApiBroker.PostDepartmentAsync(this.NewDepartmentName);
            this.NewDepartmentName = string.Empty;
            this.StatusMessage = "Department created.";
            this.StatusMessageColorClass = "text-emerald-400";
            await this.LoadDepartmentsAsync();
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Create failed: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
        }

        this.IsBusy = false;
        this.OnStateChanged();
    }

    public async ValueTask RemoveDepartmentAsync(string queueName)
    {
        this.IsBusy = true;
        this.OnStateChanged();

        try
        {
            await this.queueApiBroker.DeleteDepartmentAsync(queueName);
            this.StatusMessage = $"Removed \"{queueName}\".";
            this.StatusMessageColorClass = "text-emerald-400";
            await this.LoadDepartmentsAsync();
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Remove failed: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
        }

        this.IsBusy = false;
        this.OnStateChanged();
    }

    private async ValueTask LoadDepartmentsAsync()
    {
        try
        {
            IReadOnlyList<DepartmentInfo> retrievedDepartments = await this.queueApiBroker.GetDepartmentsAsync();
            this.departments = [.. retrievedDepartments];
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Failed to load departments: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
        }

        this.OnStateChanged();
    }

    private void OnStateChanged() => this.StateChanged?.Invoke();
}
