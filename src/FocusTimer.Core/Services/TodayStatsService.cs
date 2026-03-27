namespace FocusTimer.Core.Services
{
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Provides statistics and management for today's focus sessions.
    /// </summary>
    public class TodayStatsService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IAppLogger _logger;
        private TimeSpan _todayTotal = TimeSpan.Zero;
        private DateTime _today = DateTime.Today;

        /// <summary>
        /// Initializes a new instance of the <see cref="TodayStatsService"/> class.
        /// </summary>
        /// <param name="sessionRepository">The session repository to use for retrieving session data.</param>
        /// <param name="logger">The logger to use for logging errors and information.</param>
        public TodayStatsService(ISessionRepository sessionRepository, IAppLogger logger)
        {
            this._sessionRepository = sessionRepository;
            this._logger = logger;
        }

        /// <summary>
        /// Refreshes the today statistics by retrieving all sessions for today from the repository.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RefreshTodayAsync()
        {
            try
            {
                var today = DateTime.Today;
                var entries = await this._sessionRepository.GetSessionsByDateAsync(today);
                this._today = today;
                this._todayTotal = entries
                    .Where(e => e.StartTime.Date == today)
                    .Select(e => e.Duration ?? TimeSpan.Zero)
                    .Aggregate(TimeSpan.Zero, (sum, duration) => sum + duration);
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to refresh today stats from session repository.", ex);
            }
        }

        /// <summary>
        /// Adds time entries for today to the total, refreshing if the date has changed.
        /// </summary>
        /// <param name="entries">The time entries to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddEntriesAsync(IEnumerable<TimeEntry> entries)
        {
            var today = DateTime.Today;
            if (this._today != today)
            {
                this._today = today;
                this._todayTotal = TimeSpan.Zero;
                await this.RefreshTodayAsync();
                return;
            }

            foreach (var entry in entries)
            {
                var entryDate = entry.StartTime.Date;
                if (entryDate == today && entry.Duration.HasValue)
                {
                    this._todayTotal += entry.Duration.Value;
                }
            }
        }

        /// <summary>
        /// Gets the total focus time for today.
        /// </summary>
        /// <returns>The total duration of focus sessions for today.</returns>
        public TimeSpan GetTodayTotal() => this._todayTotal;

        /// <summary>
        /// Gets a formatted summary text of today's total focus time.
        /// </summary>
        /// <returns>A string representation of today's total focus time in the format "Today: Xh YYm".</returns>
        public string GetTodaySummaryText()
        {
            return $"Today: {(int)this._todayTotal.TotalHours}h {this._todayTotal.Minutes:D2}m";
        }
    }
}
