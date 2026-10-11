namespace ReelSoon.Infrastructure;

public sealed record Toast(string Message, string Tone);

public sealed class ToastService
{
    public event Action<Toast>? Shown;

    public void Show(string message, string tone = "info") =>
        Shown?.Invoke(new Toast(message, tone));

    public void Success(string message) => Show(message, "success");

    public void Error(string message) => Show(message, "danger");
}
