#pragma warning disable
namespace FocusTimer.App.Tests;

using System.Reflection;
using System.Text.Json;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using FocusTimer.Persistence;

public class ExclusionRuleEditorTests
{
    [Fact]
    public async Task BlankRuleShowsErrorAndCannotSave()
    {
        var provider = new Provider(); var vm = Create(provider);
        vm.AddExclusionRuleCommand.Execute(null);
        Assert.Single(vm.ExclusionRules);
        Assert.NotEmpty(vm.ExclusionRules[0].Error);
        Assert.NotEmpty(vm.ExclusionRulesError);
        await Apply(vm);
        Assert.False(vm.LastApplySucceeded); Assert.Equal(0, provider.Saves);
        Assert.NotEmpty(vm.CommitError);
        vm.ExclusionRules[0].AppPattern = "keepass";
        Assert.Empty(vm.ExclusionRules[0].Error);
        Assert.Empty(vm.ExclusionRulesError);
    }

    [Fact]
    public async Task AddEditReorderRemoveAreCommittedInOrderWithTrimmedPatterns()
    {
        var provider = new Provider(); var vm = Create(provider);
        foreach (var name in new[] { " a ", "b", "c" })
        {
            vm.AddExclusionRuleCommand.Execute(null);
            vm.ExclusionRules[^1].AppPattern = name;
        }
        vm.ExclusionRules[2].TitlePattern = "*t*";
        vm.MoveExclusionRuleUpCommand.Execute(vm.ExclusionRules[2]);
        vm.MoveExclusionRuleUpCommand.Execute(vm.ExclusionRules[0]); // no-op at the top
        vm.MoveExclusionRuleDownCommand.Execute(vm.ExclusionRules[2]); // no-op at the bottom
        vm.RemoveExclusionRuleCommand.Execute(vm.ExclusionRules[2]);
        await Apply(vm);
        Assert.True(vm.LastApplySucceeded);
        Assert.Equal(new[] { new WindowMatchRule("a", null), new WindowMatchRule("c", "*t*") },
            provider.Saved.ExclusionRules);
    }

    [Fact]
    public async Task CancelledDraftIsDiscardedAndAppliedListSurvivesReopen()
    {
        var provider = new Provider(); var vm = Create(provider);
        vm.AddExclusionRuleCommand.Execute(null);
        vm.ExclusionRules[0].AppPattern = "keepass";
        Assert.Empty(provider.Saved.ExclusionRules);
        await Apply(vm);
        Assert.Single(provider.Saved.ExclusionRules);
        vm.AddExclusionRuleCommand.Execute(null);
        vm.ExclusionRules[1].AppPattern = "draft-only";
        var reopened = Create(provider);
        Assert.Equal(new[] { "keepass" }, reopened.ExclusionRules.Select(r => r.AppPattern));
        vm.TryDiscardAndClose();
        Assert.Equal(new[] { "keepass" }, vm.ExclusionRules.Select(r => r.AppPattern));
    }

    [Fact]
    public async Task DraftEditsDoNotReachTrackerButAppliedSettingsDo()
    {
        var provider = new Provider();
        var logger = new Logger();
        var notifications = new LinuxNotificationServiceStub();
        var tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), logger);
        using var timer = new TimerService(tracker);
        using var reminders = new BreakReminderService(notifications, provider);
        using var vm = new TimerWidgetViewModel(provider, logger, new CsvSessionRepository(provider), notifications,
            tracker, reminders, timer, null, new EventBus());
        var field = typeof(SessionTracker).GetField("_exclusionRules", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var draft = new Settings();
        draft.ExclusionRules.Add(new WindowMatchRule("x", null));
        Assert.Empty((WindowMatchRule[])field.GetValue(tracker)!);

        await vm.ActivateSettingsAsync(draft);
        Assert.Equal(new[] { new WindowMatchRule("x", null) }, (WindowMatchRule[])field.GetValue(tracker)!);
    }

    private static SettingsWindowViewModel Create(Provider p) => new(
        p, new AutoStart(), new ThemeService(), new ThemeManager(), new Logger());
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

    private sealed class EmptySummary2 : IWorklogSummaryService
    {
        public Task<WorklogSummary> SummarizeAsync(WorklogSummaryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogSummary(request, WorklogOutcome.Success(), [], TimeSpan.Zero, []));
    }
}
