namespace FocusTimer.App.Services
{
    /// <summary>Material selection and its diagnostic reason; never a saved preference.</summary>
    public readonly record struct WidgetMaterialResult(WidgetMaterialState State, string? Reason = null);
}
