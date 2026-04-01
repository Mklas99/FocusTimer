namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class TodayStatsServiceTests
{
    [Fact]
    public async Task RefreshTodayAsync_GivenMixedDates_SumsOnlyTodayEntries()
    {
        var today = DateTime.Today;
        var repository = new StubSessionRepository
        {
            Entries =
            [
                new TimeEntry { StartTime = today.AddHours(9), EndTime = today.AddHours(10) },
                new TimeEntry { StartTime = today.AddHours(11), EndTime = today.AddHours(11.5) },
                new TimeEntry { StartTime = today.AddDays(-1).AddHours(9), EndTime = today.AddDays(-1).AddHours(10) },
            ],
        };

        var service = new TodayStatsService(repository, NullLogger.Instance);

        await service.RefreshTodayAsync();

        Assert.Equal(TimeSpan.FromMinutes(90), service.GetTodayTotal());
        Assert.Equal("Today: 1h 30m", service.GetTodaySummaryText());
    }

    [Fact]
    public async Task AddEntriesAsync_GivenMixedEntries_AddsOnlyTodayClosedEntries()
    {
        var today = DateTime.Today;
        var service = new TodayStatsService(new StubSessionRepository(), NullLogger.Instance);

        await service.AddEntriesAsync(
        [
            new TimeEntry { StartTime = today.AddHours(1), EndTime = today.AddHours(1.5) },
            new TimeEntry { StartTime = today.AddDays(-1), EndTime = today.AddDays(-1).AddMinutes(30) },
            new TimeEntry { StartTime = today.AddHours(2), EndTime = null },
        ]);

        Assert.Equal(TimeSpan.FromMinutes(30), service.GetTodayTotal());
    }

    [Theory]
    [InlineData(0, "Today: 0h 00m")]
    [InlineData(30, "Today: 0h 30m")]
    [InlineData(90, "Today: 1h 30m")]
    [InlineData(125, "Today: 2h 05m")]
    public async Task GetTodaySummaryText_GivenTotals_FormatsExpectedText(int totalMinutes, string expected)
    {
        var today = DateTime.Today;
        var service = new TodayStatsService(
            new StubSessionRepository
            {
                Entries =
                [
                    new TimeEntry
                    {
                        StartTime = today,
                        EndTime = today.AddMinutes(totalMinutes),
                    },
                ],
            },
            NullLogger.Instance);

        await service.RefreshTodayAsync();

        Assert.Equal(expected, service.GetTodaySummaryText());
    }

    [Fact]
    public async Task RefreshTodayAsync_GivenRepositoryThrows_DoesNotThrowAndLeavesZeroTotal()
    {
        var service = new TodayStatsService(new ThrowingRepository(), NullLogger.Instance);

        await service.RefreshTodayAsync();

        Assert.Equal(TimeSpan.Zero, service.GetTodayTotal());
    }

    private sealed class StubSessionRepository : ISessionRepository
    {
        public IEnumerable<TimeEntry> Entries { get; set; } = [];

        public Task<IEnumerable<TimeEntry>> GetSessionsByDateAsync(DateTime date)
        {
            return Task.FromResult(Entries);
        }

        public Task SaveSessionAsync(IEnumerable<TimeEntry> entries)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingRepository : ISessionRepository
    {
        public Task SaveSessionAsync(IEnumerable<TimeEntry> entries) => Task.CompletedTask;

        public Task<IEnumerable<TimeEntry>> GetSessionsByDateAsync(DateTime date)
            => throw new InvalidOperationException("boom");
    }
}
