namespace APP.Components;

// Uma opção do bottom sheet. IsLast controla se desenha divisor a seguir
// (evitar lógica de índice no XAML via BindableLayout).
public sealed class ActionSheetOption
{
    public required string IconSource { get; init; }
    public required string Text { get; init; }
    public required string ResultKey { get; init; }
    public bool IsDestructive { get; init; }
    public bool IsLast { get; init; }
}