namespace FocusTimer.App.Tests;

using System;
using FocusTimer.App.Services;

public class WindowsWidgetCompositionTests
{
    [Fact]
    public void IsSupported_WhenWindowsVersionBelowRequirement_ReturnsFalse()
    {
        var originalVersion = WindowsWidgetComposition.WindowsVersionProbe;
        try
        {
            WindowsWidgetComposition.WindowsVersionProbe = () => false;
            Assert.False(WindowsWidgetComposition.IsSupported());
        }
        finally
        {
            WindowsWidgetComposition.WindowsVersionProbe = originalVersion;
        }
    }

    [Fact]
    public void IsSupported_WhenRemoteSession_ReturnsFalse()
    {
        var originalVersion = WindowsWidgetComposition.WindowsVersionProbe;
        var originalMetrics = WindowsWidgetComposition.SystemMetricsProbe;
        try
        {
            WindowsWidgetComposition.WindowsVersionProbe = () => true;
            WindowsWidgetComposition.SystemMetricsProbe = _ => 1; // 1 indicates remote session
            Assert.False(WindowsWidgetComposition.IsSupported());
        }
        finally
        {
            WindowsWidgetComposition.WindowsVersionProbe = originalVersion;
            WindowsWidgetComposition.SystemMetricsProbe = originalMetrics;
        }
    }

    [Fact]
    public void IsSupported_WhenDwmCallFails_ReturnsFalse()
    {
        var originalVersion = WindowsWidgetComposition.WindowsVersionProbe;
        var originalMetrics = WindowsWidgetComposition.SystemMetricsProbe;
        var originalDwm = WindowsWidgetComposition.DwmCompositionProbeHook;
        try
        {
            WindowsWidgetComposition.WindowsVersionProbe = () => true;
            WindowsWidgetComposition.SystemMetricsProbe = _ => 0; // Local console session
            WindowsWidgetComposition.DwmCompositionProbeHook = (out int enabled) =>
            {
                enabled = 1;
                return -1; // Non-zero HRESULT indicates failure
            };

            Assert.False(WindowsWidgetComposition.IsSupported());
        }
        finally
        {
            WindowsWidgetComposition.WindowsVersionProbe = originalVersion;
            WindowsWidgetComposition.SystemMetricsProbe = originalMetrics;
            WindowsWidgetComposition.DwmCompositionProbeHook = originalDwm;
        }
    }

    [Fact]
    public void IsSupported_WhenDwmCompositionDisabled_ReturnsFalse()
    {
        var originalVersion = WindowsWidgetComposition.WindowsVersionProbe;
        var originalMetrics = WindowsWidgetComposition.SystemMetricsProbe;
        var originalDwm = WindowsWidgetComposition.DwmCompositionProbeHook;
        try
        {
            WindowsWidgetComposition.WindowsVersionProbe = () => true;
            WindowsWidgetComposition.SystemMetricsProbe = _ => 0;
            WindowsWidgetComposition.DwmCompositionProbeHook = (out int enabled) =>
            {
                enabled = 0; // Composition is not enabled
                return 0; // S_OK
            };

            Assert.False(WindowsWidgetComposition.IsSupported());
        }
        finally
        {
            WindowsWidgetComposition.WindowsVersionProbe = originalVersion;
            WindowsWidgetComposition.SystemMetricsProbe = originalMetrics;
            WindowsWidgetComposition.DwmCompositionProbeHook = originalDwm;
        }
    }

    [Fact]
    public void IsSupported_WhenAllConditionsMet_ReturnsTrue()
    {
        var originalVersion = WindowsWidgetComposition.WindowsVersionProbe;
        var originalMetrics = WindowsWidgetComposition.SystemMetricsProbe;
        var originalDwm = WindowsWidgetComposition.DwmCompositionProbeHook;
        try
        {
            WindowsWidgetComposition.WindowsVersionProbe = () => true;
            WindowsWidgetComposition.SystemMetricsProbe = _ => 0;
            WindowsWidgetComposition.DwmCompositionProbeHook = (out int enabled) =>
            {
                enabled = 1;
                return 0; // S_OK
            };

            Assert.True(WindowsWidgetComposition.IsSupported());
        }
        finally
        {
            WindowsWidgetComposition.WindowsVersionProbe = originalVersion;
            WindowsWidgetComposition.SystemMetricsProbe = originalMetrics;
            WindowsWidgetComposition.DwmCompositionProbeHook = originalDwm;
        }
    }

    [Fact]
    public void IsSupported_WithNativeProbes_EvaluatesWithoutException()
    {
        // Executes the actual default delegates on the host OS
        bool result = WindowsWidgetComposition.IsSupported();
        // Result is boolean true or false depending on current host OS, but should not throw.
        Assert.True(result || !result);
    }
}
