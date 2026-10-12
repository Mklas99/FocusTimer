namespace FocusTimer.App.Tests;

using Avalonia.Controls;
using Avalonia.Media;
using FocusTimer.App.Services;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

public class ThemeManagerFailureTests
{
    [Theory]
    [InlineData(nameof(Theme.WindowBackground), "widget shell")]
    [InlineData(nameof(Theme.AccentPrimary), "Fluent accent")]
    [InlineData(nameof(Theme.InputText), "InputTextColor")]
    public void InvalidColor_PreservesThePreviousBrushAndUpdatesOtherValidColors(string property, string errorContext)
    {
        var logger = new RecordingLogger();
        var manager = new ThemeManager(logger);
        var resources = new ResourceDictionary();
        var theme = new Theme();
        manager.ApplyTheme(theme, resources);
        string brushKey = property switch
        {
            nameof(Theme.WindowBackground) => "WindowBackgroundBrush",
            nameof(Theme.AccentPrimary) => "AccentPrimaryBrush",
            _ => "InputTextBrush",
        };
        object? previousBrush = resources[brushKey];
        typeof(Theme).GetProperty(property)!.SetValue(theme, "invalid color");
        theme.PrimaryText = "#123456";

        manager.ApplyTheme(theme, resources);

        Assert.Same(previousBrush, resources[brushKey]);
        Assert.Equal(Color.Parse("#123456"), Assert.IsType<SolidColorBrush>(resources["PrimaryTextBrush"]).Color);
        Assert.Contains(logger.Errors, error => error.Contains(errorContext));
    }

    [Fact]
    public void TransparencyReport_OnlyPublishesChangesAndWorksBeforeAThemeIsApplied()
    {
        var manager = new ThemeManager();
        var reported = new List<WindowTransparencyLevel>();
        manager.ActualWidgetTransparencyChanged += reported.Add;

        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.None);
        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.Transparent);
        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.Transparent);
        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.None);

        Assert.Equal(new[] { WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None }, reported);
        Assert.False(manager.IsWidgetShellFallbackActive);
    }

    [Fact]
    public void LostPlatformTransparency_UsesOpaqueFallbackAndRecoversTheOriginalTint()
    {
        var manager = new ThemeManager();
        var resources = new ResourceDictionary();
        manager.ApplyTheme(new Theme { BackgroundOpacity = 0.3 }, resources);

        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.Transparent);
        object? tint = resources["WidgetShellActiveBrush"];
        Assert.Equal(0.3, Assert.IsType<SolidColorBrush>(tint).Opacity);

        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.None);
        Assert.True(manager.IsWidgetShellFallbackActive);
        Assert.Same(resources["WidgetShellFallbackBrush"], resources["WidgetShellActiveBrush"]);
        Assert.Equal(1.0, Assert.IsType<SolidColorBrush>(resources["WidgetShellActiveBrush"]).Opacity);

        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.Transparent);
        Assert.False(manager.IsWidgetShellFallbackActive);
        Assert.Same(tint, resources["WidgetShellActiveBrush"]);
    }

    [Fact]
    public void FrostedPolicyTransitions_KeepOneShellAndNeverRewriteTheSnapshot()
    {
        var manager = new ThemeManager();
        var resources = new ResourceDictionary();
        var theme = new Theme { WidgetBlurMode = WidgetBlurModes.Frosted, BackgroundOpacity = 0.25 };
        manager.ApplyTheme(theme, resources);
        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.AcrylicBlur);
        var available = new WidgetMaterialInputs(
            WidgetBlurModes.Frosted, WindowTransparencyLevel.AcrylicBlur, false, false, true, true, true, true, true);
        WidgetMaterialInputs[] transitions =
        [
            available with { UpdateApplied = false }, available,
            available with { TransparencyDisabled = true }, available,
            available with { HighContrast = true }, available,
            available with { SupportedSession = false }, available,
            available with { AttachmentSucceeded = false }, available,
        ];
        foreach (var inputs in transitions)
        {
            manager.ReportWidgetMaterial(WidgetMaterialPolicy.Resolve(inputs));
            Assert.Same(resources[inputs == available ? "WidgetShellTintBrush" : "WidgetShellFallbackBrush"], resources["WidgetShellActiveBrush"]);
            Assert.Equal(WidgetBlurModes.Frosted, manager.ActiveTheme!.WidgetBlurMode);
            Assert.Equal(0.25, manager.ActiveTheme.BackgroundOpacity);
            Assert.Equal(WidgetBlurModes.Frosted, theme.WidgetBlurMode);
        }
    }

    [Fact]
    public void ApplyingTheme_RepairsUnexpectedResourceTypes()
    {
        var manager = new ThemeManager();
        var resources = new ResourceDictionary { ["ButtonNormalBrush"] = "invalid resource" };
        var theme = new Theme { ButtonNormal = "#123456" };

        manager.ApplyTheme(theme, resources);

        Assert.Equal(Color.Parse("#123456"), Assert.IsType<SolidColorBrush>(resources["ButtonNormalBrush"]).Color);
        Assert.Same(resources["ButtonNormalBrush"], resources["ActionPrimaryBrush"]);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<string> Errors { get; } = new();
        public void LogError(string message, Exception? ex = null) => this.Errors.Add(message);
        public void LogCritical(string message, Exception? ex = null) { }
        public void LogDebug(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
