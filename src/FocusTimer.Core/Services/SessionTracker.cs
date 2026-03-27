namespace FocusTimer.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Tracks active window changes and creates TimeEntry segments.
    /// Manages the current open segment and completed segments for logging.
    /// </summary>
    public class SessionTracker
    {
        private readonly IActiveWindowService _activeWindowService;
        private readonly IAppLogger _logger;
        private readonly List<TimeEntry> _completedEntries = new();

        private ActiveWindowInfo? _currentWindowInfo;
        private TimeEntry? _currentEntry;
        private string? _currentProjectTag;
        private bool _isTracking;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionTracker"/> class.
        /// </summary>
        /// <param name="activeWindowService">The service for getting the active window information.</param>
        /// <param name="logger">The logger for logging errors and warnings.</param>
        /// <exception cref="ArgumentNullException">Thrown when activeWindowService or logger is null.</exception>
        public SessionTracker(IActiveWindowService activeWindowService, IAppLogger logger)
        {
            this._activeWindowService = activeWindowService ?? throw new ArgumentNullException(nameof(activeWindowService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a value indicating whether gets whether any completed entries are currently buffered and ready to persist.
        /// </summary>
        public bool HasCompletedEntries => this._completedEntries.Count > 0;

        /// <summary>
        /// Gets the count of entries (completed + current open entry).
        /// Useful for diagnostics and testing.
        /// </summary>
        public int CompletedEntryCount => this._completedEntries.Count + (this._currentEntry != null ? 1 : 0);

        /// <summary>
        /// Starts tracking with the given project tag.
        /// Should be called when timer starts.
        /// </summary>
        /// <param name="projectTag">The project tag to associate with the tracking session. Can be null.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task StartAsync(string? projectTag)
        {
            if (this._isTracking)
            {
                return; // Already tracking
            }

            this._currentProjectTag = projectTag;
            this._isTracking = true;

            // Get initial window and create first entry
            try
            {
                var windowInfo = await this._activeWindowService.GetForegroundWindowAsync();
                this.CreateNewEntry(windowInfo, DateTime.Now);
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to start session tracking.", ex);

                // Create a fallback entry even if window detection fails
                this.CreateNewEntry(null, DateTime.Now);
            }
        }

        /// <summary>
        /// Called on each timer tick to check for window changes.
        /// Creates new segments when the active window changes.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTimerTickAsync()
        {
            if (!this._isTracking)
            {
                return;
            }

            try
            {
                var windowInfo = await this._activeWindowService.GetForegroundWindowAsync();

                // Check if window changed
                bool windowChanged = SessionTracker.HasWindowChanged(this._currentWindowInfo, windowInfo);

                if (windowChanged)
                {
                    var now = DateTime.Now;

                    // Close current entry if it exists
                    this.CloseCurrentEntry(now);

                    // Start new entry with new window info
                    this.CreateNewEntry(windowInfo, now);
                }
            }
            catch (Exception ex)
            {
                // Silently ignore errors during tracking to avoid crashes
                // The app should continue running even if window detection fails
                this._logger.LogWarning($"Window tracking tick failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the project tag for the current and future entries.
        /// Does NOT close and reopen the current entry, just updates the tag.
        /// </summary>
        /// <param name="projectTag">The new project tag to associate with the current and future entries. Can be null.</param>
        public void UpdateProjectTag(string? projectTag)
        {
            this._currentProjectTag = projectTag;
            if (this._currentEntry != null)
            {
                this._currentEntry.ProjectTag = projectTag;
            }
        }

        /// <summary>
        /// Stops tracking and returns all completed entries.
        /// Closes the current entry if open.
        /// Clears internal state after returning entries.
        /// </summary>
        /// <returns>A list of all completed time entries collected during the tracking session.</returns>
        public IReadOnlyList<TimeEntry> CollectAndResetSegments()
        {
            this._isTracking = false;

            // Close current entry if it exists
            this.CloseCurrentEntry(DateTime.Now);

            // Return completed entries and clear
            var entries = this._completedEntries.ToList();
            this._completedEntries.Clear();
            this._currentWindowInfo = null;

            return entries;
        }

        /// <summary>
        /// Returns completed entries without stopping the current tracking session.
        /// The active entry remains open so tracking can continue uninterrupted.
        /// </summary>
        /// <returns>A list of all completed time entries collected since the last drain, without stopping the tracking session.</returns>
        public IReadOnlyList<TimeEntry> DrainCompletedSegments()
        {
            var entries = this._completedEntries.ToList();
            this._completedEntries.Clear();
            return entries;
        }

        /// <summary>
        /// Determines if the window has changed by comparing process name and window title.
        /// </summary>
        private static bool HasWindowChanged(ActiveWindowInfo? previous, ActiveWindowInfo? current)
        {
            // Both null = no change
            if (previous == null && current == null)
            {
                return false;
            }

            // One is null = changed
            if (previous == null || current == null)
            {
                return true;
            }

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
            if (this._currentEntry != null)
            {
                this._currentEntry.EndTime = endTime;

                // Only add entries with meaningful duration (at least 1 second)
                if (this._currentEntry.Duration.HasValue && this._currentEntry.Duration.Value.TotalSeconds >= 1)
                {
                    this._completedEntries.Add(this._currentEntry);
                }

                this._currentEntry = null;
            }
        }

        /// <summary>
        /// Creates a new entry for the given window info.
        /// If windowInfo is null, creates an "Unknown" entry.
        /// </summary>
        private void CreateNewEntry(ActiveWindowInfo? windowInfo, DateTime startTime)
        {
            this._currentWindowInfo = windowInfo;

            this._currentEntry = new TimeEntry
            {
                StartTime = startTime,
                AppName = windowInfo?.ProcessName ?? "Unknown",
                WindowTitle = windowInfo?.WindowTitle ?? "No active window",
                ProjectTag = this._currentProjectTag,
            };
        }
    }
}
