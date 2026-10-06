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
    public async Task BlankRuleIsWarnedAboutAndDroppedWhenApplied()
    {
        var provider = new Provider(); var vm = Create(provider);
        vm.AddExclusionRuleCommand.Execute(null);
        Assert.Single(vm.ExclusionRules);
        Assert.Empty(vm.ExclusionRules[0].Error);
        Assert.NotEmpty(vm.ExclusionRules[0].Warning);
        Assert.True(vm.ExclusionRules[0].HasWarning);
        Assert.Empty(vm.ExclusionRulesError);
        await Apply(vm);
        Assert.True(vm.LastApplySucceeded);
        Assert.Empty(provider.Saved.ExclusionRules);
        Assert.Empty(vm.ExclusionRules);
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
    public async Task SegmentationListIsIndependentValidatedAndCommitted()
    {
        var provider = new Provider(); var vm = Create(provider);
        vm.ExclusionList.AddCommand.Execute(null);
        vm.ExclusionRules[0].AppPattern = "keepass";
        vm.SegmentationList.AddCommand.Execute(null);
        Assert.Empty(vm.SegmentationList.Error);
        Assert.True(vm.SegmentationList.Rules[0].IsBlank);
        Assert.Empty(vm.ExclusionList.Error);
        vm.SegmentationList.Rules[0].AppPattern = "chrome";
        vm.SegmentationList.Rules[0].TitlePattern = " * ";
        await Apply(vm);
        Assert.True(vm.LastApplySucceeded);
        Assert.Equal(new[] { new WindowMatchRule("keepass", null) }, provider.Saved.ExclusionRules);
        Assert.Equal(new[] { new WindowMatchRule("chrome", "*") }, provider.Saved.SegmentationRules);
        vm.SegmentationList.AddCommand.Execute(null);
        vm.SegmentationList.Rules[1].AppPattern = "draft";
        var reopened = Create(provider);
        Assert.Equal(new[] { "chrome" }, reopened.SegmentationList.Rules.Select(r => r.AppPattern));
        vm.TryDiscardAndClose();
        Assert.Equal(new[] { "chrome" }, vm.SegmentationList.Rules.Select(r => r.AppPattern));
    }

    [Fact]
    public async Task ProjectRulesAreValidatedCommittedReorderedAndCancelled()
    {
        var provider = new Provider(); var vm = Create(provider);
        vm.ProjectList.AddCommand.Execute(null);
        Assert.Empty(vm.ProjectList.Error); // a blank row is only warned about
        Assert.True(vm.ProjectList.Rules[0].HasWarning);
        vm.ProjectList.Rules[0].AppPattern = "code";
        Assert.False(vm.ProjectList.Rules[0].HasWarning);
        Assert.True(vm.ProjectList.Rules[0].HasError);
        Assert.NotEmpty(vm.ProjectList.Error); // project name still missing
        await Apply(vm);
        Assert.False(vm.LastApplySucceeded); Assert.Equal(0, provider.Saves);
        vm.ProjectList.Rules[0].ProjectName = "  Alpha ";
        Assert.Empty(vm.ProjectList.Error);
        vm.ProjectList.AddCommand.Execute(null);
        vm.ProjectList.Rules[1].TitlePattern = "*repo*";
        vm.ProjectList.Rules[1].ProjectName = "Beta";
        vm.ProjectList.MoveUpCommand.Execute(vm.ProjectList.Rules[1]);
        Assert.Empty(provider.Saved.ProjectRules);
        await Apply(vm);
        Assert.True(vm.LastApplySucceeded);
        Assert.Equal(new[] { new ProjectRule(null, "*repo*", "Beta"), new ProjectRule("code", null, "Alpha") }, provider.Saved.ProjectRules);
        vm.ProjectList.RemoveCommand.Execute(vm.ProjectList.Rules[0]);
        vm.TryDiscardAndClose();
        Assert.Equal(new[] { "Beta", "Alpha" }, vm.ProjectList.Rules.Select(r => r.ProjectName));
        Assert.Equal(new[] { "Beta", "Alpha" }, Create(provider).ProjectList.Rules.Select(r => r.ProjectName));
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

        draft.SegmentationRules.Add(new WindowMatchRule("y", null));
        await vm.ActivateSettingsAsync(draft);
        Assert.Equal(new[] { new WindowMatchRule("x", null) }, (WindowMatchRule[])field.GetValue(tracker)!);
        Assert.Equal(new[] { new WindowMatchRule("y", null) }, (WindowMatchRule[])typeof(SessionTracker)
            .GetField("_segmentationRules", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tracker)!);
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
