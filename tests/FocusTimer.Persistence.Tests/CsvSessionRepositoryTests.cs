namespace FocusTimer.Persistence.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

public class CsvSessionRepositoryTests
{
    [Fact]
    public async Task SaveSessionAsync_GivenValidEntries_RoundTripsExpectedFields()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var settings = new Settings
            {
                WorklogDirectory = root,
                DataRetentionDays = 365,
            };

            var repository = new CsvSessionRepository(new StubSettingsProvider(settings), NullLogger.Instance);
            var day = new DateTime(2026, 3, 31, 9, 0, 0);
            var entries = new[]
            {
                new TimeEntry
                {
                    StartTime = day,
                    EndTime = day.AddMinutes(45),
                    AppName = "code",
                    WindowTitle = "FocusTimer",
                    ProjectTag = "FT",
                },
            };

            await repository.SaveSessionAsync(entries);
            var loaded = (await repository.GetSessionsByDateAsync(day)).ToList();

            Assert.Single(loaded);
            Assert.Equal("code", loaded[0].AppName);
            Assert.Equal("FocusTimer", loaded[0].WindowTitle);
            Assert.Equal("FT", loaded[0].ProjectTag);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task GetSessionsByDateAsync_GivenMissingFile_ReturnsEmpty()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var settings = new Settings { WorklogDirectory = root };
            var repository = new CsvSessionRepository(new StubSettingsProvider(settings), NullLogger.Instance);

            var result = await repository.GetSessionsByDateAsync(DateTime.Today);

            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task SaveSessionAsync_GivenRetentionEnabled_DeletesExpiredFiles()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var settings = new Settings
            {
                WorklogDirectory = root,
                DataRetentionDays = 1,
            };

            var oldDate = DateTime.Today.AddDays(-5);
            var oldDir = Path.Combine(root, oldDate.ToString("yyyy"), oldDate.ToString("MM"));
            Directory.CreateDirectory(oldDir);
            var oldFile = Path.Combine(oldDir, $"{oldDate:yyyy-MM-dd}-worklog.csv");
            await File.WriteAllTextAsync(oldFile, "Date,StartTime,EndTime,DurationSeconds,AppName,WindowTitle,ProjectTag");

            var repository = new CsvSessionRepository(new StubSettingsProvider(settings), NullLogger.Instance);
            var today = DateTime.Today.AddHours(9);

            await repository.SaveSessionAsync(
            [
                new TimeEntry
                {
                    StartTime = today,
                    EndTime = today.AddMinutes(30),
                    AppName = "A",
                    WindowTitle = "B",
                },
            ]);

            Assert.False(File.Exists(oldFile));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task SaveSessionAsync_GivenOpenEntry_SkipsEntryWithoutEndTime()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var date = DateTime.Today.AddHours(8);
            var settings = new Settings { WorklogDirectory = root, DataRetentionDays = 365 };
            var repository = new CsvSessionRepository(new StubSettingsProvider(settings), NullLogger.Instance);

            await repository.SaveSessionAsync(
            [
                new TimeEntry { StartTime = date, EndTime = null, AppName = "open", WindowTitle = "draft" },
            ]);

            var loaded = await repository.GetSessionsByDateAsync(date);

            Assert.Empty(loaded);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task SaveSessionAsync_GivenCommaInField_EscapesAndParsesFieldCorrectly()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var date = DateTime.Today.AddHours(8);
            var settings = new Settings { WorklogDirectory = root, DataRetentionDays = 365 };
            var repository = new CsvSessionRepository(new StubSettingsProvider(settings), NullLogger.Instance);

            await repository.SaveSessionAsync(
            [
                new TimeEntry
                {
                    StartTime = date,
                    EndTime = date.AddMinutes(25),
                    AppName = "Code,Editor",
                    WindowTitle = "Main Window",
                    ProjectTag = "A,B",
                },
            ]);

            var loaded = (await repository.GetSessionsByDateAsync(date)).ToList();

            Assert.Single(loaded);
            Assert.Equal("Code,Editor", loaded[0].AppName);
            Assert.Equal("A,B", loaded[0].ProjectTag);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task GetSessionsByDateAsync_GivenMalformedCsvLine_SkipsInvalidLine()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var day = new DateTime(2026, 3, 31);
            var yearDir = Path.Combine(root, day.ToString("yyyy"), day.ToString("MM"));
            Directory.CreateDirectory(yearDir);
            var csv = Path.Combine(yearDir, $"{day:yyyy-MM-dd}-worklog.csv");
            await File.WriteAllLinesAsync(csv,
            [
                "Date,StartTime,EndTime,DurationSeconds,AppName,WindowTitle,ProjectTag",
                "broken,line",
                "2026-03-31,09:00:00,09:30:00,1800,app,title,tag",
            ]);

            var settings = new Settings { WorklogDirectory = root, DataRetentionDays = 365 };
            var repository = new CsvSessionRepository(new StubSettingsProvider(settings), NullLogger.Instance);

            var loaded = (await repository.GetSessionsByDateAsync(day)).ToList();

            Assert.Single(loaded);
            Assert.Equal("app", loaded[0].AppName);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private sealed class StubSettingsProvider : ISettingsProvider
    {
        private readonly Settings _settings;

        public StubSettingsProvider(Settings settings)
        {
            _settings = settings;
        }

        public Task<Settings> LoadAsync() => Task.FromResult(_settings);

        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }
}
