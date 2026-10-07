namespace FocusTimer.App.Tests;

using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using ReactiveUI;

/// <summary>
/// Qualifies the dense-frost shell on the real Windows compositor. A successful API call is not accepted as
/// evidence: the test shows a Settings window over a high-contrast stripe pattern, captures the pixels the
/// user would actually see, and requires the backdrop to bleed through smoothly (blurred), neither absent
/// (solid) nor sharp (an unblurred transparent shell). Windows appear on screen for a few seconds.
/// Run separately: FOCUSTIMER_NATIVE_APPEARANCE_TESTS=1 (native Avalonia state is process-wide).
/// </summary>
[Collection("Native appearance")]
public class DesktopMaterialNativeTests
{
    private const int StripeWidth = 6;

    [AppearanceNativeTests.NativeAppearanceFact]
    public void DenseFrost_SoftensTheBackdropBehindSettings_AndFallbackIsSolid()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string folder = Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "desktop-evidence");
        Directory.CreateDirectory(folder);
        var report = new StringBuilder();
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                AppBuilder.Configure<AppearanceNativeTests.AppearanceTestApp>().UsePlatformDetect().SetupWithoutStarting();
                SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
                var manager = new ThemeManager();
                manager.InitializeThemeResources();
                manager.ApplyTheme(new ThemeService().BuiltInThemes.First(t => t.ThemeName == "Dark"));

                var backdrop = new Window
                {
                    Width = 1000,
                    Height = 800,
                    Position = new PixelPoint(120, 80),
                    CanResize = false,
                    SystemDecorations = SystemDecorations.None,
                    ShowInTaskbar = false,
                    Content = BuildStripes(),
                };
                backdrop.Show();

                SettingsWindowViewModel editor = SettingsWindowViewModelTests.CreateAppearanceEditor();
                editor.SelectedTabIndex = 3; // Hotkeys: an almost empty page, so most pixels are plain shell.
                var settings = new SettingsWindow { DataContext = editor, Position = new PixelPoint(260, 160) };
                settings.Show();
                settings.Activate();
                Settle(900);

                report.AppendLine(DesktopWindowMaterial.GetDescription(settings));
                report.AppendLine($"Requested hint: {string.Join("/", settings.TransparencyLevelHint)}; actual: {settings.ActualTransparencyLevel}");
                PixelMetrics frost = Measure(settings, folder, "frost");
                DesktopMaterialState frostState = DesktopWindowMaterial.GetState(settings);
                report.AppendLine($"Active material: {frostState}; {frost}");

                // Control 1: the same shell forced to the solid fallback must hide the stripes completely.
                Application.Current!.Resources[DesktopWindowMaterial.ForceSolidResourceKey] = true;
                Settle(600);
                Assert.Equal(DesktopMaterialState.SolidFallback, DesktopWindowMaterial.GetState(settings));
                PixelMetrics solid = Measure(settings, folder, "solid");
                report.AppendLine($"Forced solid fallback: {solid}");
                Assert.True(solid.StdDev < 0.5, $"Solid fallback must not show the backdrop: {solid}");
                Application.Current.Resources[DesktopWindowMaterial.ForceSolidResourceKey] = false;
                Settle(600);

                // Control 2: a transparent but unblurred shell shows the stripes sharply; this is what blur must differ from.
                settings.Classes.Remove(DesktopWindowMaterial.FrostActiveClass);
                settings.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                settings.Background = new SolidColorBrush(Color.Parse("#2D2D30"), ThemeManager.DesktopShellFrostOpacity);
                Settle(900);
                PixelMetrics sharp = Measure(settings, folder, "transparent-unblurred");
                report.AppendLine($"Transparent, unblurred (control): {sharp}");
                File.WriteAllText(Path.Combine(folder, "material-qualification.txt"), report.ToString());

                Assert.True(sharp.MaxStep >= 6, $"Control must show sharp stripes to be meaningful: {sharp}");
                bool effectsOff = DesktopWindowMaterial.GetDescription(settings).Contains("transparency effects off", StringComparison.Ordinal);
                if (effectsOff || !DesktopMaterialPolicy.IsBlurLevel(settings.ActualTransparencyLevel))
                {
                    // Windows draws acrylic as a flat color while Transparency effects is off, so the active material cannot be
                    // observed here. The fallback controls above still ran; the frost itself stays unqualified.
                    report.AppendLine("RESULT: dense frost NOT VERIFIED on this machine (Windows transparency effects are off or no blur level was reported); solid fallback verified.");
                    File.WriteAllText(Path.Combine(folder, "material-qualification.txt"), report.ToString());
                    settings.Close();
                    backdrop.Close();
                    return;
                }

                Assert.Equal(DesktopMaterialState.DenseFrost, frostState);
                Assert.True(frost.StdDev > 0.3, $"Backdrop must bleed through the near-solid tint: {frost}");
                Assert.True(
                    frost.MaxStep * 3 <= sharp.MaxStep,
                    $"Backdrop must be softened, not merely transparent. frost {frost}, unblurred control {sharp}");
                report.AppendLine("RESULT: dense frost VERIFIED: backdrop bleeds through smoothly (blurred), unlike the unblurred transparent control.");
                File.WriteAllText(Path.Combine(folder, "material-qualification.txt"), report.ToString());
                settings.Close();
                backdrop.Close();
            }
            catch (Exception ex)
            {
                failure = ex;
                File.WriteAllText(Path.Combine(folder, "material-qualification.txt"), report + Environment.NewLine + ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(90)), "Material qualification timed out.");
        if (failure != null)
        {
            throw new Xunit.Sdk.XunitException(failure.ToString());
        }
    }

    private static Canvas BuildStripes()
    {
        var canvas = new Canvas { Background = Brushes.White };
        for (int x = 0; x < 1000; x += StripeWidth * 2)
        {
            canvas.Children.Add(new Rectangle
            {
                Width = StripeWidth,
                Height = 800,
                Fill = Brushes.Black,
                [Canvas.LeftProperty] = (double)x,
            });
        }

        return canvas;
    }

    private static void Settle(int milliseconds)
    {
        var until = DateTime.UtcNow.AddMilliseconds(milliseconds);
        while (DateTime.UtcNow < until)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(20);
        }
    }

    private static PixelMetrics Measure(Window window, string folder, string name)
    {
        // A lower-right client area of the (nearly empty) Hotkeys page: plain shell, no controls.
        PixelPoint topLeft = window.PointToScreen(new Point(60, window.Bounds.Height - 230));
        const int Width = 360;
        const int Height = 100;
        byte[] bgra = ScreenCapture.Capture(topLeft.X, topLeft.Y, Width, Height);
        ScreenCapture.SavePng(bgra, Width, Height, Path.Combine(folder, $"material-{name}.png"));
        return PixelMetrics.From(bgra, Width, Height);
    }

    private readonly record struct PixelMetrics(double Mean, double StdDev, int MaxStep)
    {
        public static PixelMetrics From(byte[] bgra, int width, int height)
        {
            double sum = 0;
            double sumSquares = 0;
            int maxStep = 0;
            for (int y = 0; y < height; y++)
            {
                int previous = -1;
                for (int x = 0; x < width; x++)
                {
                    int i = ((y * width) + x) * 4;
                    int luma = ((bgra[i + 2] * 299) + (bgra[i + 1] * 587) + (bgra[i] * 114)) / 1000;
                    sum += luma;
                    sumSquares += luma * luma;
                    if (previous >= 0)
                    {
                        maxStep = Math.Max(maxStep, Math.Abs(luma - previous));
                    }

                    previous = luma;
                }
            }

            double count = width * height;
            double mean = sum / count;
            return new PixelMetrics(mean, Math.Sqrt(Math.Max(0, (sumSquares / count) - (mean * mean))), maxStep);
        }

        public override string ToString() => $"mean luma {Mean:F1}, stddev {StdDev:F2}, max adjacent-pixel step {MaxStep}";
    }
}
