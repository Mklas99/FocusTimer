namespace FocusTimer.App.Tests;

using Avalonia.Controls;
using FocusTimer.App.Services;
using FocusTimer.Core.Models;

public class WidgetMaterialPolicyTests
{
    private static WidgetMaterialInputs Supported => new(
        "Frosted", WindowTransparencyLevel.AcrylicBlur, false, false, true, true, true, true, true);

    [Fact]
    public void Frost_RequiresEverySupportConditionAndReportsWhy()
    {
        Assert.Equal(new WidgetMaterialResult(WidgetMaterialState.Frosted), WidgetMaterialPolicy.Resolve(Supported));
        WidgetMaterialInputs[] failures =
        [
            Supported with { HighContrast = true },
            Supported with { TransparencyDisabled = true },
            Supported with { SupportedSession = false },
            Supported with { IsWindows = false },
            Supported with { CapabilityAvailable = false },
            Supported with { AttachmentSucceeded = false },
            Supported with { UpdateApplied = false },
            Supported with { Actual = WindowTransparencyLevel.Transparent },
            Supported with { Actual = WindowTransparencyLevel.Blur },
        ];

        foreach (var inputs in failures)
        {
            var result = WidgetMaterialPolicy.Resolve(inputs);
            Assert.Equal(WidgetMaterialState.SolidFallback, result.State);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
            Assert.Equal("Frosted", inputs.Requested);
        }
    }

    [Fact]
    public void OffAndSolid_DoNotRequireNativeFrostCapability()
    {
        var clear = Supported with
        {
            Requested = WidgetBlurModes.Off,
            Actual = WindowTransparencyLevel.Transparent,
            CapabilityAvailable = false,
            AttachmentSucceeded = false,
            IsWindows = false,
        };
        Assert.Equal(WidgetMaterialState.Clear, WidgetMaterialPolicy.Resolve(clear).State);
        Assert.Equal(WidgetMaterialState.Solid, WidgetMaterialPolicy.Resolve(clear with { Requested = WidgetBlurModes.Solid }).State);
        Assert.Equal(WidgetMaterialState.SolidFallback, WidgetMaterialPolicy.Resolve(clear with { Actual = WindowTransparencyLevel.None }).State);
    }

    [Fact]
    public void PreferenceRecovery_ResumesTheOriginalRequest()
    {
        Assert.Equal(WidgetMaterialState.SolidFallback, WidgetMaterialPolicy.Resolve(Supported with { TransparencyDisabled = true }).State);
        Assert.Equal(WidgetMaterialState.Frosted, WidgetMaterialPolicy.Resolve(Supported).State);
    }
}
