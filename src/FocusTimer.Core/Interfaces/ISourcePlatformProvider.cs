#pragma warning disable

namespace FocusTimer.Core.Interfaces;

using FocusTimer.Core.Models;
/// <summary>Supplies the platform encoded into captured worklog entries.</summary>
public interface ISourcePlatformProvider { SourcePlatform GetCurrentPlatform(); }
