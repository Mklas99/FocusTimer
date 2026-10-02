namespace FocusTimer.Persistence.Tests;

using FocusTimer.Core.Models;

public class JsonWorklogViewStateStoreTests
{
    [Fact]
    public async Task LoadAsync_WhenNothingWasSaved_ReturnsAnEmptyState()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = new JsonWorklogViewStateStore(Path.Combine(root, "worklog-view.json"));

            var state = await store.LoadAsync();

            Assert.Null(state.TimelineHourHeight);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsTheZoom_AndCreatesTheFolder()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "nested", "worklog-view.json");
            var store = new JsonWorklogViewStateStore(path);

            await store.SaveAsync(new WorklogViewState(137.5));
            var state = await new JsonWorklogViewStateStore(path).LoadAsync();

            Assert.Equal(137.5, state.TimelineHourHeight);
            Assert.True(File.Exists(path));
            Assert.False(File.Exists(path + ".tmp"));
            Assert.Contains("timelineHourHeight", await File.ReadAllTextAsync(path));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task SaveAsync_RoundTripsTheTimelineGrouping_AndAnOldFileWithoutOneStillLoads()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "worklog-view.json");
            var store = new JsonWorklogViewStateStore(path);

            await store.SaveAsync(new WorklogViewState(120, "project"));
            var state = await store.LoadAsync();
            Assert.Equal(("project", 120d), (state.TimelineGroupingId, state.TimelineHourHeight));

            await File.WriteAllTextAsync(path, "{ \"timelineHourHeight\": 90 }");
            var old = await store.LoadAsync();
            Assert.Equal(90, old.TimelineHourHeight);
            Assert.Null(old.TimelineGroupingId);
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("")]
    [InlineData("{ \"timelineHourHeight\": \"tall\" }")]
    [InlineData("[1,2,3]")]
    public async Task LoadAsync_GivenADamagedFile_ReturnsAnEmptyStateAndLogsInsteadOfThrowing(string content)
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "worklog-view.json");
            await File.WriteAllTextAsync(path, content);
            var logger = new CountingLogger();
            var store = new JsonWorklogViewStateStore(path, logger);

            var state = await store.LoadAsync();

            Assert.Null(state.TimelineHourHeight);
            Assert.Equal(1, logger.Warnings);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task SaveAsync_WhenThePathIsNotWritable_LogsInsteadOfThrowing()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            // A directory where the file should go cannot be replaced by a file.
            var path = Path.Combine(root, "worklog-view.json");
            Directory.CreateDirectory(path);
            var logger = new CountingLogger();
            var store = new JsonWorklogViewStateStore(path, logger);

            await store.SaveAsync(new WorklogViewState(90));

            Assert.Equal(1, logger.Warnings);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task SaveAsync_Concurrently_LeavesAValidFileWithOneOfTheValues()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "worklog-view.json");
            var store = new JsonWorklogViewStateStore(path);

            await Task.WhenAll(Enumerable.Range(1, 20).Select(i => store.SaveAsync(new WorklogViewState(30 + i))));
            var state = await store.LoadAsync();

            Assert.InRange(state.TimelineHourHeight!.Value, 31, 50);
        }
        finally { Directory.Delete(root, true); }
    }

    private sealed class CountingLogger : FocusTimer.Core.Interfaces.IAppLogger
    {
        public int Warnings { get; private set; }

        public void LogCritical(string message, Exception? ex = null) { }

        public void LogDebug(string message) { }

        public void LogError(string message, Exception? ex = null) { }

        public void LogInformation(string message) { }

        public void LogWarning(string message) => this.Warnings++;
    }
}
