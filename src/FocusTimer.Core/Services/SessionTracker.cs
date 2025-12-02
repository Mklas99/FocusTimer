using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Services;

/// <summary>
/// Tracks active window changes and creates TimeEntry segments.
/// Manages the current open segment and completed segments for logging.
/// </summary>
public class SessionTracker
{
    private readonly IActiveWindowService _activeWindowService;
    private readonly List<TimeEntry> _completedEntries = new();
    
    private ActiveWindowInfo? _currentWindowInfo;
    private TimeEntry? _currentEntry;
    private string? _currentProjectTag;
    private bool _isTracking;

    public SessionTracker(IActiveWindowService activeWindowService)
    {
        _activeWindowService = activeWindowService ?? throw new ArgumentNullException(nameof(activeWindowService));
    }

    /// <summary>
    /// Starts tracking with the given project tag.
    /// Should be called when timer starts.
    /// </summary>
    public async Task StartAsync(string? projectTag)
    {
        if (_isTracking)
            return; // Already tracking

        _currentProjectTag = projectTag;
        _isTracking = true;
        
        // Get initial window and create first entry
        try
        {
            var windowInfo = await _activeWindowService.GetForegroundWindowAsync();
            CreateNewEntry(windowInfo, DateTime.Now);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start session tracking: {ex.Message}");
            // Create a fallback entry even if window detection fails
            CreateNewEntry(null, DateTime.Now);
        }
    }

    /// <summary>
    /// Called on each timer tick to check for window changes.
    /// Creates new segments when the active window changes.
    /// </summary>
    public async Task OnTimerTickAsync()
    {
        if (!_isTracking)
            return;

        try
        {
            var windowInfo = await _activeWindowService.GetForegroundWindowAsync();
            
            // Check if window changed
            bool windowChanged = HasWindowChanged(_currentWindowInfo, windowInfo);

            if (windowChanged)
            {
                var now = DateTime.Now;
                
                // Close current entry if it exists
                CloseCurrentEntry(now);

                // Start new entry with new window info
                CreateNewEntry(windowInfo, now);
            }
        }
        catch (Exception ex)
        {
            // Silently ignore errors during tracking to avoid crashes
            // The app should continue running even if window detection fails
            System.Diagnostics.Debug.WriteLine($"Error during window tracking: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the project tag for the current and future entries.
    /// Does NOT close and reopen the current entry, just updates the tag.
    /// </summary>
    public void UpdateProjectTag(string? projectTag)
    {
        _currentProjectTag = projectTag;
        if (_currentEntry != null)
        {
            _currentEntry.ProjectTag = projectTag;
        }
    }

    /// <summary>
    /// Stops tracking and returns all completed entries.
    /// Closes the current entry if open.
    /// Clears internal state after returning entries.
    /// </summary>
    public IReadOnlyList<TimeEntry> CollectAndResetSegments()
    {
        _isTracking = false;

        // Close current entry if it exists
        CloseCurrentEntry(DateTime.Now);

        // Return completed entries and clear
        var entries = _completedEntries.ToList();
        _completedEntries.Clear();
        _currentWindowInfo = null;

        return entries;
    }

    /// <summary>
    /// Gets the count of entries (completed + current open entry).
    /// Useful for diagnostics and testing.
    /// </summary>
    public int CompletedEntryCount => _completedEntries.Count + (_currentEntry != null ? 1 : 0);

    /// <summary>
    /// Determines if the window has changed by comparing process name and window title.
    /// </summary>
    private bool HasWindowChanged(ActiveWindowInfo? previous, ActiveWindowInfo? current)
    {
        // Both null = no change
        if (previous == null && current == null)
            return false;

        // One is null = changed
        if (previous == null || current == null)
            return true;

        // Compare process name and window title
        return previous.ProcessName != current.ProcessName ||
               previous.WindowTitle != current.WindowTitle;
    }

    /// <summary>
    /// Closes the current entry by setting its EndTime.
    /// Adds it to completed entries if it has a valid duration.
    /// </summary>
    private void CloseCurrentEntry(DateTime endTime)
    {
        if (_currentEntry != null)
        {
            _currentEntry.EndTime = endTime;
            
            // Only add entries with meaningful duration (at least 1 second)
            if (_currentEntry.Duration.HasValue && _currentEntry.Duration.Value.TotalSeconds >= 1)
            {
                _completedEntries.Add(_currentEntry);
            }
            
            _currentEntry = null;
        }
    }

    /// <summary>
    /// Creates a new entry for the given window info.
    /// If windowInfo is null, creates an "Unknown" entry.
    /// </summary>
    private void CreateNewEntry(ActiveWindowInfo? windowInfo, DateTime startTime)
    {
        _currentWindowInfo = windowInfo;
        
        _currentEntry = new TimeEntry
        {
            StartTime = startTime,
            AppName = windowInfo?.ProcessName ?? "Unknown",
            WindowTitle = windowInfo?.WindowTitle ?? "No active window",
            ProjectTag = _currentProjectTag
        };
    }
}
