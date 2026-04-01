namespace FocusTimer.App.Tests;

using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

public class MainWindowViewModelTests
{
    [Fact]
    public async Task Constructor_ImmediatelyStartsWithLoadingPlaceholder()
    {
        var vm = new MainWindowViewModel(new DelayedSettingsProvider(50, new Settings { WorklogDirectory = "X" }));

        Assert.Equal("Loading...", vm.WorklogDirectory);
        await Task.Delay(60);
    }

    [Fact]
    public async Task Constructor_GivenProviderResult_LoadsWorklogDirectoryFromSettings()
    {
        var expected = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var vm = new MainWindowViewModel(new StubSettingsProvider(new Settings { WorklogDirectory = expected }));

        for (var i = 0; i < 20 && vm.WorklogDirectory == "Loading..."; i++)
        {
            await Task.Delay(25);
        }

        Assert.Equal(expected, vm.WorklogDirectory);
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

    private sealed class DelayedSettingsProvider : ISettingsProvider
    {
        private readonly int _delayMs;
        private readonly Settings _settings;

        public DelayedSettingsProvider(int delayMs, Settings settings)
        {
            _delayMs = delayMs;
            _settings = settings;
        }

        public async Task<Settings> LoadAsync()
        {
            await Task.Delay(_delayMs);
            return _settings;
        }

        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }
}
