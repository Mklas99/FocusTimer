namespace FocusTimer.App.HeadlessTests;

using System.Reactive;
using System.Reactive.Threading.Tasks;
using Avalonia.Controls;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using ReactiveUI;

/// <summary>
/// Covers SettingsWindowViewModel's three file-dialog commands for the one branch headless
/// Avalonia can actually reach: Avalonia's headless platform backs Window.StorageProvider
/// with NoopStorageProvider, whose pickers always return an empty/null result without
/// throwing — the same outward behavior as a user cancelling the real dialog. StorageProvider
/// isn't virtual, so the "a file was picked" and "the picker threw" branches have no seam to
/// test without changing production code.
/// </summary>
public sealed class SettingsFileDialogTests
{
    public SettingsFileDialogTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public async Task BrowseWorklogDirectoryCommand_WhenUserCancels_LeavesDirectoryUnchanged()
    {
        var editor = CreateEditor();
        string originalDirectory = editor.Settings.WorklogDirectory;
        var window = new Window();

        await Execute(editor.BrowseWorklogDirectoryCommand, window);

        Assert.Equal(originalDirectory, editor.Settings.WorklogDirectory);
    }

    [Fact]
    public async Task ImportThemeCommand_WhenUserCancels_LeavesThemeUnchanged()
    {
        var editor = CreateEditor();
        string originalThemeName = editor.Settings.Theme.ThemeName;
        var window = new Window();

        await Execute(editor.ImportThemeCommand, window);

        Assert.Equal(originalThemeName, editor.Settings.Theme.ThemeName);
    }

    [Fact]
    public async Task ExportThemeCommand_WhenUserCancels_CompletesWithoutLoggingAnExport()
    {
        var logger = new RecordingLogger();
        var editor = CreateEditor(logger);
        var window = new Window();

        await Execute(editor.ExportThemeCommand, window);

        Assert.DoesNotContain(logger.InformationMessages, m => m.Contains("exported", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(logger.Errors);
    }

    private static Task Execute(System.Windows.Input.ICommand command, Window window) =>
        ((ReactiveCommand<Window, Unit>)command).Execute(window).ToTask();

    private static SettingsWindowViewModel CreateEditor(IAppLogger? logger = null) => new(
        new SettingsProviderStub(),
        new LinuxAutoStartServiceStub(),
        new ThemeService(),
        new ThemeManager(),
        logger ?? new RecordingLogger(),
        new WorklogSummaryViewModel(
            new EmptySummaryService(),
            new FocusTimer.Core.Services.WorklogGroupingRegistry(
                [new FocusTimer.Core.Services.ApplicationGrouping(), new FocusTimer.Core.Services.ProjectGrouping()]),
            TimeProvider.System));

    private sealed class RecordingLogger : IAppLogger
    {
        public List<string> InformationMessages { get; } = new();

        public List<string> Errors { get; } = new();

        public void LogCritical(string message, Exception? exception = null) { }

        public void LogError(string message, Exception? exception = null) => this.Errors.Add(message);

        public void LogWarning(string message) { }

        public void LogInformation(string message) => this.InformationMessages.Add(message);

        public void LogDebug(string message) { }
    }

    private sealed class EmptySummaryService : IWorklogSummaryService
    {
        public Task<WorklogSummary> SummarizeAsync(WorklogSummaryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogSummary(request, WorklogOutcome.Success(), [], TimeSpan.Zero, []));
    }
}
