#pragma warning disable
namespace FocusTimer.App.Tests;

using System.Reflection;
using System.Text.Json;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class ActivityPollingEditorTests
{
    [Theory]
    [InlineData("1.5")]
    [InlineData("0")]
    [InlineData("61")]
    public async Task InvalidDraftIsVisibleAndCannotSave(string input)
    {
        var provider = new Provider(); var vm = Create(provider);
        vm.ActivityPollingIntervalInput = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        Assert.NotEmpty(vm.ActivityPollingIntervalError);
        await Apply(vm);
        Assert.False(vm.LastApplySucceeded); Assert.Equal(0, provider.Saves);
        Assert.Equal(10, provider.Saved.ActivityPollingIntervalSeconds);
    }

    [Fact]
    public async Task ApplyAndCancelEditsPreserveLastAppliedValueAcrossReopen()
    {
        var provider = new Provider(); var vm = Create(provider); var applied = 0;
        vm.SettingsApplied += (_, _) => applied++;
        vm.ActivityPollingIntervalInput = 5;
        Assert.Equal(10, provider.Saved.ActivityPollingIntervalSeconds);
        await Apply(vm);
        Assert.True(vm.LastApplySucceeded); Assert.Equal(1, applied);
        vm.ActivityPollingIntervalInput = 60; // Unapplied draft discarded when the editor is closed.
        var reopened = Create(provider);
        Assert.Equal(5, reopened.ActivityPollingIntervalInput);
        for (var i = 0; i < 7; i++) reopened.RegisterVersionInfoClick();
        Assert.True(reopened.IsDeveloperModeVisible);
        Assert.Equal(5, reopened.ActivityPollingIntervalInput);
        reopened.ActivityPollingIntervalInput = 1;
        await (Task)typeof(SettingsWindowViewModel).GetMethod("OkAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(reopened, null)!;
        Assert.Equal(1, provider.Saved.ActivityPollingIntervalSeconds);
    }

    [Fact]
    public async Task SaveFailureDoesNotActivateDraft()
    {
        var provider = new Provider { Fail = true }; var vm = Create(provider); var applied = 0;
        vm.SettingsApplied += (_, _) => applied++;
        vm.ActivityPollingIntervalInput = 60;
        await Apply(vm);
        Assert.False(vm.LastApplySucceeded); Assert.Equal(0, applied);
        Assert.Equal(10, provider.Saved.ActivityPollingIntervalSeconds);
    }

    private static SettingsWindowViewModel Create(Provider p) => new(p, new AutoStart(), new ThemeService(), new ThemeManager(), new Logger());
    private static Task Apply(SettingsWindowViewModel vm) => (Task)typeof(SettingsWindowViewModel)
        .GetMethod("ApplyAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null)!;

    private sealed class Provider : ISettingsProvider
    {
        public Settings Saved = new(); public int Saves; public bool Fail;
        public Task<Settings> LoadAsync() => Task.FromResult(JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(Saved))!);
        public Task SaveAsync(Settings settings)
        {
            if (Fail) throw new IOException("synthetic failure");
            Saved = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(settings))!;
            Saves++; return Task.CompletedTask;
        }
    }

    private sealed class AutoStart : IAutoStartService
    {
        public bool IsAutoStartEnabled() => false;
        public void SetAutoStart(bool value) { }
    }

    private sealed class Logger : IAppLogger
    {
        public void LogCritical(string m, Exception? e = null) { }
        public void LogError(string m, Exception? e = null) { }
        public void LogWarning(string m) { }
        public void LogInformation(string m) { }
        public void LogDebug(string m) { }
    }
}
