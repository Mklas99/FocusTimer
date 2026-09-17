#pragma warning disable

namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
/// <summary>Uses runtime platform information without platform dependencies.</summary>
public sealed class SourcePlatformProvider : ISourcePlatformProvider
{ public SourcePlatform GetCurrentPlatform() => OperatingSystem.IsWindows() ? SourcePlatform.Windows : OperatingSystem.IsLinux() ? SourcePlatform.Linux : OperatingSystem.IsMacOS() ? SourcePlatform.MacOS : SourcePlatform.Unknown; }
