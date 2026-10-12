using System;
using Avalonia.Threading;
using Avalonia.Win32.WinRT.Composition;

namespace Avalonia.Win32;

/// <summary>Controls only the native backdrop owned by a single Avalonia Windows window.</summary>
public sealed class Win32BackdropControllerV1 : IDisposable
{
    private readonly WeakReference<WindowImpl> _window;
    private readonly WeakReference<WinUiCompositedWindowSurface> _surface;
    private readonly IntPtr _handle;
    private bool _disposed;

    internal Win32BackdropControllerV1(WindowImpl window, WinUiCompositedWindowSurface surface, IntPtr handle)
    {
        _window = new(window);
        _surface = new(surface);
        _handle = handle;
    }

    /// <summary>Creates a controller only for a live window using the qualified WinUI composition surface.</summary>
    public static bool TryCreate(IntPtr handle, out Win32BackdropControllerV1? controller)
    {
        Dispatcher.UIThread.VerifyAccess();
        controller = WindowImpl.CreateBackdropController(handle);
        return controller != null;
    }

    /// <summary>Queues physical-pixel clip radius and independent backdrop opacity. False means unavailable.</summary>
    public bool TryUpdate(float cornerRadiusPixels, float opacity, bool enabled)
        => TryUpdateCore(cornerRadiusPixels, opacity, enabled, null);

    /// <summary>
    /// Queues an update and reports application or failure on the UI thread. Coalesced obsolete updates may be skipped.
    /// The callback can report failure again if the surface is subsequently disposed. No callbacks follow controller disposal.
    /// </summary>
    public bool TryUpdateWithCompletion(float cornerRadiusPixels, float opacity, bool enabled, Action<bool> completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        return TryUpdateCore(cornerRadiusPixels, opacity, enabled, applied =>
        {
            if (!_disposed && _window.TryGetTarget(out var window) && window.MatchesBackdropHandle(_handle))
                completed(applied);
        });
    }

    private bool TryUpdateCore(float cornerRadiusPixels, float opacity, bool enabled, Action<bool>? completed)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (!float.IsFinite(cornerRadiusPixels) || cornerRadiusPixels < 0 || !float.IsFinite(opacity) || opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(cornerRadiusPixels));
        if (_disposed || !_window.TryGetTarget(out var window) || !window.MatchesBackdropHandle(_handle)
            || !_surface.TryGetTarget(out var surface)) return false;
        return surface.QueueBackdrop(new BackdropStateV1(cornerRadiusPixels, opacity, enabled), completed);
    }

    /// <summary>True only after the latest queued update was applied; visual qualification is still required.</summary>
    public bool IsApplied => !_disposed && _window.TryGetTarget(out var window) && window.MatchesBackdropHandle(_handle)
        && _surface.TryGetTarget(out var surface) && surface.BackdropApplied;

    /// <summary>Releases this controller and restores the surface's original backdrop policy.</summary>
    public void Dispose()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (_disposed) return;
        if (_window.TryGetTarget(out var window) && window.MatchesBackdropHandle(_handle)
            && _surface.TryGetTarget(out var surface)) surface.QueueBackdrop(null);
        _disposed = true;
    }
}

internal readonly record struct BackdropStateV1(float CornerRadiusPixels, float Opacity, bool Enabled);
