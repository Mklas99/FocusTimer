namespace FocusTimer.App.Services
{
    using Avalonia.Controls;
    using FocusTimer.Core.Models;

    /// <summary>Pure widget material policy, separate from native attachment and stored appearance.</summary>
    public static class WidgetMaterialPolicy
    {
        /// <summary>Selects a material without modifying the requested appearance.</summary>
        /// <param name="inputs">Current theme, preferences, and backend facts.</param>
        /// <returns>Effective material and an explanation for fallback.</returns>
        public static WidgetMaterialResult Resolve(in WidgetMaterialInputs inputs)
        {
            if (inputs.HighContrast)
            {
                return new(WidgetMaterialState.SolidFallback, "High Contrast");
            }

            if (inputs.Requested == WidgetBlurModes.Solid)
            {
                return new(WidgetMaterialState.Solid);
            }

            if (inputs.TransparencyDisabled)
            {
                return new(WidgetMaterialState.SolidFallback, "Transparency effects disabled");
            }

            if (inputs.Requested != WidgetBlurModes.Frosted)
            {
                return inputs.Actual == WindowTransparencyLevel.Transparent
                    ? new(WidgetMaterialState.Clear)
                    : new(WidgetMaterialState.SolidFallback, "Clear transparency unavailable");
            }

            if (!inputs.IsWindows)
            {
                return new(WidgetMaterialState.SolidFallback, "Frosted is unsupported on this platform");
            }

            if (!inputs.SupportedSession)
            {
                return new(WidgetMaterialState.SolidFallback, "Composition or session unavailable");
            }

            if (!inputs.CapabilityAvailable)
            {
                return new(WidgetMaterialState.SolidFallback, "Qualified backend extension unavailable");
            }

            if (!inputs.AttachmentSucceeded)
            {
                return new(WidgetMaterialState.SolidFallback, "Native backdrop attachment failed");
            }

            if (inputs.Actual != WindowTransparencyLevel.AcrylicBlur)
            {
                return new(WidgetMaterialState.SolidFallback, "Qualified acrylic material unavailable");
            }

            return inputs.UpdateApplied
                ? new(WidgetMaterialState.Frosted)
                : new(WidgetMaterialState.SolidFallback, "Backdrop update pending or failed");
        }
    }
}
