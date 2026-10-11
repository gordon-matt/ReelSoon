using Microsoft.AspNetCore.Components;

namespace ReelSoon.Components.Shared;

public partial class Icon
{
    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    [Parameter]
    public string? Class { get; set; }
}
