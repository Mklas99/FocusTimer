using FocusTimer.Core.Interfaces;
using Microsoft.Win32;
using System.Reflection;

namespace FocusTimer.Platform.Windows;

/// <summary>
/// Windows implementation of auto-start service using registry.
/// </summary>
public class WindowsAutoStartService : IAutoStartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "FocusTimer";

    public void SetAutoStart(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null)
            {
                System.Diagnostics.Debug.WriteLine("Failed to open registry key for auto-start.");
                return;
            }

            if (enabled)
            {
                // Get the path to the current executable
                var exePath = GetExecutablePath();
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                    System.Diagnostics.Debug.WriteLine($"Auto-start enabled: {exePath}");
                }
            }
            else
            {
                // Remove the registry value
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName);
                    System.Diagnostics.Debug.WriteLine("Auto-start disabled.");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set auto-start: {ex.Message}");
        }
    }

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            if (key == null)
                return false;

            var value = key.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to check auto-start status: {ex.Message}");
            return false;
        }
    }

    private static string GetExecutablePath()
    {
        // For .NET apps, we need to resolve the actual executable path
        var processPath = Environment.ProcessPath;
        
        if (!string.IsNullOrEmpty(processPath))
            return processPath;

        // Fallback to assembly location
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.Location;
    }
}
