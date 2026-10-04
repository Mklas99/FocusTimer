#pragma warning disable
namespace FocusTimer.App.Tests;

using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class RuleListViewModelTests
{
    [Fact]
    public void RemoveAndMoveIgnoreUnknownRowsNullAndEdges()
    {
        var list = new WindowRuleListViewModel("t", "d", "bad");
        list.Load(new[] { new WindowMatchRule("a", null), new WindowMatchRule("b", null) });
        var changes = 0;
        list.DraftChanged += (_, _) => changes++;

        list.RemoveCommand.Execute(null);
        list.RemoveCommand.Execute(new WindowRuleItemViewModel());
        list.MoveUpCommand.Execute(null);
        list.MoveUpCommand.Execute(new WindowRuleItemViewModel());
        list.MoveUpCommand.Execute(list.Rules[0]);
        list.MoveDownCommand.Execute(list.Rules[1]);
        Assert.Equal(0, changes);
        Assert.Equal(new[] { "a", "b" }, list.Rules.Select(r => r.AppPattern));

        list.MoveDownCommand.Execute(list.Rules[0]);
        Assert.Equal(new[] { "b", "a" }, list.Rules.Select(r => r.AppPattern));
        Assert.Equal(1, changes);
    }

    [Fact]
    public void LoadReplacesRowsDetachesOldOnesAndReportsErrors()
    {
        var list = new WindowRuleListViewModel("Title", "Desc", "bad");
        Assert.Equal("Title", list.Title);
        Assert.Equal("Desc", list.Description);
        list.Load(new[] { new WindowMatchRule("a", null) });
        var old = list.Rules[0];
        list.Load(new[] { new WindowMatchRule("b", null) });
        var changes = 0;
        list.DraftChanged += (_, _) => changes++;
        old.AppPattern = "edited-after-removal";
        Assert.Equal(0, changes);
        list.Rules[0].AppPattern = "";
        Assert.True(changes > 0);
        Assert.Equal("bad", list.Error);
        Assert.Equal(1, list.ToRules().Count);
    }

    [Fact]
    public void ProjectRowsValidateEveryFieldAndConvertToTrimmedRules()
    {
        var row = new ProjectRuleItemViewModel();
        Assert.NotEmpty(row.Error);
        row.AppPattern = " code ";
        Assert.NotEmpty(row.Error);
        row.ProjectName = " Alpha ";
        Assert.Empty(row.Error);
        row.TitlePattern = "  ";
        Assert.Equal(new ProjectRule("code", null, "Alpha"), row.ToRule());
        row.AppPattern = null!;
        row.TitlePattern = null!;
        row.ProjectName = null!;
        Assert.Equal(new ProjectRule(null, null, null), row.ToRule());

        var list = new ProjectRuleListViewModel("t", "d", "bad");
        list.Load(new[] { new ProjectRule("a", "b", "P") });
        Assert.Equal(new[] { new ProjectRule("a", "b", "P") }, list.ToRules());
    }

    [Fact]
    public void WindowRowsTrimAndNormalizeNull()
    {
        var row = new WindowRuleItemViewModel(new WindowMatchRule(" a ", null));
        Assert.Equal(new WindowMatchRule("a", null), row.ToRule());
        row.AppPattern = null!;
        row.TitlePattern = null!;
        Assert.NotEmpty(row.Error);
    }

    [Fact]
    public void EntriesViewModelWithoutLoadedEntriesIgnoresRuleChanges()
    {
        var rules = new CountingProvider();
        var vm = new WorklogEntriesViewModel(new MemoryWorklogStore(), TimeProvider.System, null, new RuleProjectResolver(rules), rules);
        Assert.Equal(1, rules.Subscribers);
        rules.Raise(); // nothing loaded yet: must not throw or create rows
        Assert.Empty(vm.AllRows);
        GC.KeepAlive(vm);
    }

    private sealed class CountingProvider : IProjectRuleProvider
    {
        private EventHandler? _handlers;
        public int Subscribers => this._handlers?.GetInvocationList().Length ?? 0;
        public IReadOnlyList<ProjectRule> Rules => Array.Empty<ProjectRule>();
        public event EventHandler? RulesChanged
        {
            add => this._handlers += value;
            remove => this._handlers -= value;
        }
        public void Raise() => this._handlers?.Invoke(this, EventArgs.Empty);
    }
}
