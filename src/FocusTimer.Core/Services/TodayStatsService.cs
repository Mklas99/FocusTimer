using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Services;

public class TodayStatsService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IAppLogger _logger;
    private TimeSpan _todayTotal = TimeSpan.Zero;
    private DateTime _today = DateTime.Today;

    public TodayStatsService(ISessionRepository sessionRepository, IAppLogger logger)
    {
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task RefreshTodayAsync()
    {
        try
        {
            var today = DateTime.Today;
            var entries = await _sessionRepository.GetSessionsByDateAsync(today);
            _today = today;
            _todayTotal = entries
                .Where(e => e.StartTime.Date == today)
                .Select(e => e.Duration ?? TimeSpan.Zero)
                .Aggregate(TimeSpan.Zero, (sum, duration) => sum + duration);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to refresh today stats from session repository.", ex);
        }
    }

    public async Task AddEntriesAsync(IEnumerable<TimeEntry> entries)
    {
        var today = DateTime.Today;
        if (_today != today)
        {
            _today = today;
            _todayTotal = TimeSpan.Zero;
            await RefreshTodayAsync();
            return;
        }

        foreach (var entry in entries)
        {
            var entryDate = entry.StartTime.Date;
            if (entryDate == today && entry.Duration.HasValue)
            {
                _todayTotal += entry.Duration.Value;
            }
        }
    }

    public TimeSpan GetTodayTotal() => _todayTotal;

    public string GetTodaySummaryText()
    {
        return $"Today: {(int)_todayTotal.TotalHours}h {_todayTotal.Minutes:D2}m";
    }
}
