namespace FocusTimer.App.Services
{
    using Avalonia.Controls;

    /// <summary>Observable platform facts used to select widget material.</summary>
    public readonly record struct WidgetMaterialInputs(
        string Requested,
        WindowTransparencyLevel Actual,
        bool HighContrast,
        bool TransparencyDisabled,
        bool SupportedSession,
        bool IsWindows,
        bool CapabilityAvailable,
        bool AttachmentSucceeded,
        bool UpdateApplied);
}
