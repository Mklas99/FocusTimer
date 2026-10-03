#pragma warning disable
namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class WindowMatchingTests
{
    private static ActiveWindowInfo W(string app, string title = "t") => new() { ProcessName = app, WindowTitle = title };

    [Theory]
    [InlineData("*Inbox*", "Inbox (3) - Mail", true)]
    [InlineData("inbox*", "INBOX", true)]
    [InlineData("a?c", "abc", true)]
    [InlineData("a?c", "ac", false)]
    [InlineData("a.c", "abc", false)]
    [InlineData("(x)[1]", "(x)[1]", true)]
    [InlineData("(x)[1]", "x1", false)]
    [InlineData("*", "", true)]
    [InlineData("a*b*c", "aXXbYYc", true)]
    [InlineData("a*b*c", "aXXbYY", false)]
    [InlineData("abc", "abcd", false)]
    public void GlobSemantics(string pattern, string value, bool expected) =>
        Assert.Equal(expected, GlobMatcher.IsMatch(pattern, value));

    [Fact]
    public void ApplicationOnlyIgnoresTitle() =>
        Assert.True(new WindowMatchRule("KeePass", null).Matches(W("keepass.exe", "anything")));

    [Fact]
    public void ExeExtensionIsToleratedOnEitherSide()
    {
        Assert.True(new WindowMatchRule("keepass.exe", null).Matches(W("KeePass")));
        Assert.True(new WindowMatchRule("keepass", null).Matches(W("KeePass.EXE")));
    }

    [Fact]
    public void WildcardOnlyExeStemIsNotCollapsedToMatchEverything()
    {
        Assert.True(new WindowMatchRule("*.exe", null).Matches(W("notepad.exe")));
        Assert.False(new WindowMatchRule("*.exe", null).Matches(W("bash")));
        Assert.False(new WindowMatchRule("?.exe", null).Matches(W("notepad")));
        Assert.True(new WindowMatchRule("note*.exe", null).Matches(W("notepad")));
    }

    [Fact]
    public void AppAndTitleBothMustMatch()
    {
        var rule = new WindowMatchRule("chrome", "*bank*");
        Assert.True(rule.Matches(W("chrome", "My Bank")));
        Assert.False(rule.Matches(W("chrome", "News")));
        Assert.False(rule.Matches(W("firefox", "My Bank")));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "  ")]
    public void BlankRuleIsInvalidAndNeverMatches(string? app, string? title)
    {
        var rule = new WindowMatchRule(app, title);
        Assert.False(rule.IsValid);
        Assert.False(rule.Matches(W("x")));
    }

    [Fact]
    public void NullWindowNeverMatches() => Assert.False(new WindowMatchRule("*", null).Matches(null));

    [Fact]
    public void FirstMatchingRuleWins()
    {
        var first = new WindowMatchRule("app*", null);
        var rules = new[] { new WindowMatchRule("zzz", null), first, new WindowMatchRule("app", null) };
        Assert.Same(first, WindowRuleMatcher.FindFirst(rules, W("app")));
        Assert.Null(WindowRuleMatcher.FindFirst(rules, W("other")));
        Assert.Null(WindowRuleMatcher.FindFirst(null, W("app")));
    }
}

public class AppExclusionTrackerTests
{
    [Fact]
    public async Task SwitchToExcludedClosesSegmentAndRecordsNothing_ThenReturnStartsFresh()
    {
        var (t, clock, win) = Create();
        t.SetExclusionRules(new[] { new WindowMatchRule("secret", null) });
        await t.StartAsync(null);
        clock.Advance(10);
        win.Window = Win("secret");
        await t.OnTimerTickAsync();
        var closed = Assert.Single(t.DrainCompletedSegments());
        Assert.Equal("app", closed.AppName);
        Assert.Equal(10, (closed.EndedAt - closed.StartedAt).TotalSeconds);

        clock.Advance(30);
        await t.OnTimerTickAsync();
        Assert.Empty(t.DrainCompletedSegments());

        win.Window = Win("app");
        var returnAt = clock.Now.AddSeconds(10);
        clock.Advance(10);
        await t.OnTimerTickAsync();
        clock.Advance(5);
        var last = Assert.Single(t.CollectAndResetSegments());
        Assert.Equal(returnAt, last.StartedAt);
        Assert.Equal(5, (last.EndedAt - last.StartedAt).TotalSeconds);
    }

    [Fact]
    public async Task StartOnExcludedWindowOpensNothingUntilVisible()
    {
        var (t, clock, win) = Create();
        win.Window = Win("secret");
        t.SetExclusionRules(new[] { new WindowMatchRule("secret", null) });
        await t.StartAsync(null);
        Assert.Equal(0, t.CompletedEntryCount);
        clock.Advance(10);
        await t.OnTimerTickAsync();
        Assert.Equal(2, win.Calls);
        win.Window = Win("app");
        clock.Advance(10);
        await t.OnTimerTickAsync();
        Assert.Equal(1, t.CompletedEntryCount);
    }

    [Fact]
    public async Task ExcludedAcrossMidnightWritesNothing()
    {
        var (t, clock, win) = Create();
        clock.Now = new DateTimeOffset(2026, 9, 27, 23, 59, 50, TimeSpan.Zero);
        win.Window = Win("secret");
        t.SetExclusionRules(new[] { new WindowMatchRule("secret", null) });
        await t.StartAsync(null);
        clock.Advance(30);
        await t.OnTimerTickAsync();
        Assert.Empty(t.CollectAndResetSegments());
    }

    [Fact]
    public async Task LookupFailureWhileExcludedStaysExcluded()
    {
        var (t, clock, win) = Create();
        win.Window = Win("secret");
        t.SetExclusionRules(new[] { new WindowMatchRule("secret", null) });
        await t.StartAsync(null);
        win.Fail = true;
        clock.Advance(10);
        await t.OnTimerTickAsync();
        Assert.Equal(0, t.CompletedEntryCount);
        Assert.Empty(t.CollectAndResetSegments());
    }

    [Fact]
    public async Task RuleChangeRequestsAnImmediateSampleWithoutWaitingForTheInterval()
    {
        var (t, clock, win) = Create();
        t.SetPollingInterval(60);
        await t.StartAsync(null);
        clock.Advance(5);
        Assert.Equal(1, win.Calls);
        t.SetExclusionRules(new[] { new WindowMatchRule("app", null) });
        Assert.Equal(1, t.CompletedEntryCount);
        Assert.Empty(t.DrainCompletedSegments());
        clock.Advance(1);
        await t.OnTimerTickAsync();
        Assert.Equal(2, win.Calls);
        var entry = Assert.Single(t.DrainCompletedSegments());
        Assert.Equal(6, (entry.EndedAt - entry.StartedAt).TotalSeconds);
        Assert.Equal(0, t.CompletedEntryCount);
        clock.Advance(1);
        await t.OnTimerTickAsync();
        Assert.Equal(2, win.Calls);
    }

    [Fact]
    public async Task RuleRemovedWhileExcludedStartsSegmentAtNextSample()
    {
        var (t, clock, win) = Create();
        t.SetExclusionRules(new[] { new WindowMatchRule("app", null) });
        await t.StartAsync(null);
        t.SetExclusionRules(null);
        clock.Advance(10);
        await t.OnTimerTickAsync();
        Assert.Equal(1, t.CompletedEntryCount);
    }

    [Fact]
    public async Task InvalidRulesAreIgnoredAndIdenticalListIsNoOp()
    {
        var (t, clock, win) = Create();
        t.SetExclusionRules(new[] { new WindowMatchRule(null, null), new WindowMatchRule(" ", "") });
        await t.StartAsync(null);
        Assert.Equal(1, t.CompletedEntryCount);
        var rules = new[] { new WindowMatchRule("zzz", null) };
        t.SetExclusionRules(rules);
        t.SetExclusionRules(new[] { new WindowMatchRule("zzz", null) });
        clock.Advance(10);
        await t.OnTimerTickAsync();
        Assert.Equal(1, t.CompletedEntryCount);
    }

    [Fact]
    public async Task TitleRuleExcludesOnlyMatchingTitle()
    {
        var (t, clock, win) = Create();
        t.SetExclusionRules(new[] { new WindowMatchRule("app", "*private*") });
        await t.StartAsync(null);
        win.Window = new ActiveWindowInfo { ProcessName = "app", WindowTitle = "My private tab" };
        clock.Advance(10);
        await t.OnTimerTickAsync();
        Assert.Single(t.DrainCompletedSegments());
        Assert.Equal(0, t.CompletedEntryCount);
    }

    private static ActiveWindowInfo Win(string app) => new() { ProcessName = app, WindowTitle = "title" };

    private static (SessionTracker, Clock, Windows) Create()
    {
        var clock = new Clock(); var windows = new Windows();
        return (new SessionTracker(windows, NullLogger.Instance, clock, new SourcePlatformProvider(), () => "device"), clock, windows);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 9, 27, 9, 0, 0, TimeSpan.Zero);
        private long timestamp;
        public override DateTimeOffset GetUtcNow() => Now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => timestamp;
        public void Advance(int seconds) { timestamp += seconds; Now = Now.AddSeconds(seconds); }
    }

    private sealed class Windows : IActiveWindowService
    {
        public int Calls;
        public bool Fail;
        public ActiveWindowInfo Window = Win("app");
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("synthetic failure");
            return Task.FromResult<ActiveWindowInfo?>(Window);
        }
    }
}
