#pragma warning disable

namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Provides today totals from the storage-neutral worklog query contract.</summary>
public sealed class TodayStatsService
{
    private readonly IWorklogStore _worklogStore;
    private readonly IAppLogger _logger;
    private readonly TimeProvider _timeProvider;
    private TimeSpan _todayTotal;
    private DateTime _today = DateTime.Today;

    /// <summary>Initializes a new instance of the <see cref="TodayStatsService"/> class.</summary>
    public TodayStatsService(IWorklogStore worklogStore, IAppLogger logger, TimeProvider? timeProvider = null)
    {
        this._worklogStore = worklogStore;
        this._logger = logger;
        this._timeProvider = timeProvider ?? TimeProvider.System;
    }
    /// <summary>Refreshes the total using the local-day half-open interval.</summary>
    public async Task RefreshTodayAsync()
    {
        try
        {
            var date = this._timeProvider.GetLocalNow().Date;
            var timeZone = this._timeProvider.LocalTimeZone;
            var start = new DateTimeOffset(date, timeZone.GetUtcOffset(date));
            var nextDate = date.AddDays(1);
            var end = new DateTimeOffset(nextDate, timeZone.GetUtcOffset(nextDate));
            var read = await this._worklogStore.QueryAsync(new WorklogQuery(start, end));
            if (!read.Outcome.IsSuccess)
            {
                this._logger.LogWarning(read.Outcome.Message ?? "Unable to read worklog.");
                return;
            }
            this._today = date;
            this._todayTotal = read.Entries.Aggregate(TimeSpan.Zero, (sum, entry) => sum + entry.Duration);
        }
        catch (Exception ex) { this._logger.LogError("Failed to refresh today stats from worklog store.", ex); }
    }
    /// <summary>Adds newly persisted entries to the cached total.</summary>
    public async Task AddEntriesAsync(IEnumerable<TimeEntry> entries) { if (this._today != this._timeProvider.GetLocalNow().Date) { await this.RefreshTodayAsync(); return; } this._todayTotal += entries.Where(e => e.StartedAt.Date == this._today).Aggregate(TimeSpan.Zero, (sum, entry) => sum + entry.Duration); }
    /// <summary>Gets the total focus time for today.</summary>
    public TimeSpan GetTodayTotal() => this._todayTotal;
    /// <summary>Gets a formatted total.</summary>
    public string GetTodaySummaryText() => $"Today: {(int)this._todayTotal.TotalHours}h {this._todayTotal.Minutes:D2}m";
}
