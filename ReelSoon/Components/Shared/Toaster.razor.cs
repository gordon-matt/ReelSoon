using Microsoft.AspNetCore.Components;

namespace ReelSoon.Components.Shared;

public partial class Toaster : IDisposable
{
    [Inject]
    private ToastService Toasts { get; set; } = default!;

    private readonly List<Toast> toasts = [];

    protected override void OnInitialized() => Toasts.Shown += OnShown;

    private void OnShown(Toast toast)
    {
        InvokeAsync(async () =>
        {
            toasts.Add(toast);
            StateHasChanged();
            await Task.Delay(3200);
            toasts.Remove(toast);
            StateHasChanged();
        });
    }

    public void Dispose() => Toasts.Shown -= OnShown;
}
