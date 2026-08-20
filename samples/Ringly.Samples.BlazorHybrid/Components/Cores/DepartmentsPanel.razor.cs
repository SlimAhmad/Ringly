using Microsoft.AspNetCore.Components;
using Ringly.Samples.BlazorHybrid.ViewServices.Departments;

namespace Ringly.Samples.BlazorHybrid.Components.Cores;

public partial class DepartmentsPanel : ComponentBase, IDisposable
{
    [Inject]
    private IDepartmentsViewService ViewService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        this.ViewService.StateChanged += this.OnViewServiceStateChanged;
        await this.ViewService.InitializeAsync();
    }

    private void OnViewServiceStateChanged() => this.InvokeAsync(this.StateHasChanged);

    private Task OnCreateClickedAsync() => this.ViewService.CreateDepartmentAsync().AsTask();

    private Task OnRemoveClickedAsync(string queueName) => this.ViewService.RemoveDepartmentAsync(queueName).AsTask();

    public void Dispose() => this.ViewService.StateChanged -= this.OnViewServiceStateChanged;
}
