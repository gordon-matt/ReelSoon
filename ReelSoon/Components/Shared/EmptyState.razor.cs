using Microsoft.AspNetCore.Components;

namespace ReelSoon.Components.Shared;

public partial class EmptyState
{
    [Parameter, EditorRequired]
    public string IconName { get; set; } = "alert";

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
