using Microsoft.AspNetCore.Components;

namespace ReelSoon.Components.Shared;

public partial class ConfirmDialog
{
    private bool visible;
    private string title = string.Empty;
    private string message = string.Empty;
    private TaskCompletionSource<bool>? pending;

    public Task<bool> ShowAsync(string dialogTitle, string dialogMessage)
    {
        title = dialogTitle;
        message = dialogMessage;
        visible = true;
        pending = new TaskCompletionSource<bool>();
        StateHasChanged();
        return pending.Task;
    }

    private void Close(bool confirmed)
    {
        visible = false;
        pending?.TrySetResult(confirmed);
    }
}
