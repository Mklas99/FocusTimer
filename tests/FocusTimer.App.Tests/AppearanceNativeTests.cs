namespace FocusTimer.App.Tests;

using System.Reflection;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;
using Material.Icons.Avalonia;
using ReactiveUI;

[CollectionDefinition("Native appearance", DisableParallelization = true)]
public class NativeAppearanceCollection;

[Collection("Native appearance")]
public class AppearanceNativeTests
{
    [NativeAppearanceFact]
    public void RealizedWidgetIcons_UseLiveStateColorsInBothModes()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                AppBuilder.Configure<AppearanceTestApp>().UsePlatformDetect().SetupWithoutStarting();
                SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
                var manager = new ThemeManager();
                var theme = new Theme
                {
                    ButtonNormal = "#111111", PlayPauseColor = "#222222", ButtonHover = "#333333",
                    ButtonPressed = "#444444", ButtonDisabled = "#555555",
                };
                foreach (Control view in new Control[] { new FullModeView(), new CompactModeView() })
                {
                    var window = new Window { Content = view, ShowActivated = false, Position = new PixelPoint(-3000, -3000) };
                    manager.ApplyTheme(theme);
                    window.Show();
                    Dispatcher.UIThread.RunJobs();
                    foreach (Button button in view.GetVisualDescendants().OfType<Button>())
                    {
                        MaterialIcon? icon = button.GetVisualDescendants().OfType<MaterialIcon>().FirstOrDefault();
                        if (icon == null) continue;
                        button.Command = new EnabledCommand();
                        var states = (IPseudoClasses)typeof(StyledElement).GetProperty("PseudoClasses", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(button)!;
                        Color normal = Color.Parse(icon.Classes.Contains("play-pause-icon") ? theme.PlayPauseColor! : theme.ButtonNormal);
                        Assert.Equal(normal, Assert.IsType<SolidColorBrush>(icon.Foreground).Color);
                        states.Set(":pointerover", true);
                        Assert.Equal(Color.Parse(theme.ButtonHover), Assert.IsType<SolidColorBrush>(icon.Foreground).Color);
                        theme.ButtonHover = "#666666";
                        manager.ApplyTheme(theme);
                        Assert.Equal(Color.Parse(theme.ButtonHover), Assert.IsType<SolidColorBrush>(icon.Foreground).Color);
                        states.Set(":pressed", true);
                        Assert.Equal(Color.Parse(theme.ButtonPressed), Assert.IsType<SolidColorBrush>(icon.Foreground).Color);
                        button.IsEnabled = false;
                        Assert.Equal(Color.Parse(theme.ButtonDisabled), Assert.IsType<SolidColorBrush>(icon.Foreground).Color);
                        button.IsEnabled = true;
                        states.Set(":pressed", false);
                        states.Set(":pointerover", false);
                        Assert.Equal(normal, Assert.IsType<SolidColorBrush>(icon.Foreground).Color);
                    }

                    window.Close();
                }

                foreach (string preset in new[] { "Light", "Monokai", "Solarized Dark" })
                {
                    var editor = SettingsWindowViewModelTests.CreateAppearanceEditor();
                    editor.SelectedThemeName = preset;
                    editor.SelectedTabIndex = AppearanceTab;
                    var gate = new TaskCompletionSource();
                    editor.SetRuntimeActivator(_ => gate.Task);
                    var settings = new SettingsWindow { DataContext = editor, ShowActivated = false, Position = new PixelPoint(-3000, -3000) };
                    settings.Show();
                    Dispatcher.UIThread.RunJobs();
                    ExpandPaletteGroups(settings);
                    TextBox input = settings.GetVisualDescendants().OfType<TextBox>().First();
                    input.Focus();
                    var inputMethod = new TextInputMethodClientRequestedEventArgs
                    {
                        RoutedEvent = InputElement.TextInputMethodClientRequestedEvent,
                    };
                    input.RaiseEvent(inputMethod);
                    Assert.NotNull(inputMethod.Client);
                    inputMethod.Client.SetPreeditText("uncommitted");
                    input.ContextMenu = new ContextMenu { ItemsSource = new[] { new MenuItem { Header = "Paste" } } };
                    input.ContextMenu.Open(input);
                    ComboBox dropdown = settings.GetVisualDescendants().OfType<ComboBox>().First();
                    dropdown.IsDropDownOpen = true;
                    input.Focus();
                    inputMethod.Client.SetPreeditText("uncommitted");
                    Assert.Contains(input.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>(), presenter => presenter.PreeditText == "uncommitted");
                    Task apply = ((ReactiveCommand<Unit, Unit>)editor.ApplyCommand).Execute().ToTask();
                    Dispatcher.UIThread.RunJobs();
                    Assert.True(editor.IsCommitting);
                    Assert.True(settings.FindControl<TabControl>("DraftPanel")!.IsEffectivelyEnabled);
                    Assert.False(dropdown.IsDropDownOpen);
                    Assert.False(input.ContextMenu.IsOpen);
                    Assert.Same(settings.FindControl<TextBlock>("SaveStatus"), settings.FocusManager!.GetFocusedElement());
                    Assert.All(input.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>(), presenter => Assert.True(string.IsNullOrEmpty(presenter.PreeditText)));
                    Capture(settings, preset + "-saving");
                    var text = new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = "blocked" };
                    string? originalText = input.Text;
                    input.RaiseEvent(text);
                    Assert.True(text.Handled);
                    Assert.Equal(originalText, input.Text);
                    var paste = new RoutedEventArgs(TextBox.PastingFromClipboardEvent);
                    input.RaiseEvent(paste);
                    Assert.True(paste.Handled);
                    var cut = new RoutedEventArgs(TextBox.CuttingToClipboardEvent);
                    input.RaiseEvent(cut);
                    Assert.True(cut.Handled);
                    Slider slider = settings.GetVisualDescendants().OfType<Slider>().First();
                    double originalValue = slider.Value;
                    var key = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right };
                    slider.RaiseEvent(key);
                    Assert.True(key.Handled);
                    Assert.Equal(originalValue, slider.Value);
                    Assert.False(editor.TryDiscardAndClose());
                    Complete(((ReactiveCommand<Unit, Unit>)editor.OkCommand).Execute().ToTask());
                    Assert.True(editor.IsCommitting);
                    gate.SetResult();
                    Complete(apply);
                    Assert.Equal(string.Empty, editor.SavingStatus);
                    Assert.Same(input, settings.FocusManager.GetFocusedElement());
                    Capture(settings, preset + "-applied");
                    editor.SetRuntimeActivator(_ => Task.CompletedTask);
                    Complete(((ReactiveCommand<Unit, Unit>)editor.ApplyCommand).Execute().ToTask());
                    Assert.True(editor.LastApplySucceeded);
                    Assert.Equal(string.Empty, editor.SavingStatus);
                    int activations = 0;
                    editor.SetRuntimeActivator(_ => ++activations == 1 ? Task.FromException(new System.IO.IOException("Native failure check")) : Task.CompletedTask);
                    Complete(((ReactiveCommand<Unit, Unit>)editor.ApplyCommand).Execute().ToTask());
                    Assert.True(editor.HasCommitError);
                    Assert.False(editor.RecoveryRequired);
                    Assert.Equal(string.Empty, editor.SavingStatus);
                    Capture(settings, preset + "-failed");
                    editor.SetRuntimeActivator(_ => Task.FromException(new System.IO.IOException("Native recovery check")));
                    Complete(((ReactiveCommand<Unit, Unit>)editor.ApplyCommand).Execute().ToTask());
                    Assert.True(editor.RecoveryRequired);
                    Assert.Equal(string.Empty, editor.SavingStatus);
                    Assert.False(editor.CanCommit);
                    Capture(settings, preset + "-recovery");
                    settings.Close();
                    settings.Close();

                    var reopenedEditor = SettingsWindowViewModelTests.CreateAppearanceEditor();
                    reopenedEditor.SelectedThemeName = preset;
                    reopenedEditor.SelectedTabIndex = AppearanceTab;
                    var reopenedWindow = new SettingsWindow { DataContext = reopenedEditor, ShowActivated = false, Position = new PixelPoint(-3000, -3000) };
                    reopenedWindow.Show();
                    Dispatcher.UIThread.RunJobs();
                    Assert.True(reopenedWindow.FindControl<TabControl>("DraftPanel")!.IsEffectivelyEnabled);
                    Button ok = reopenedWindow.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "OK"));
                    ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    for (int i = 0; i < 100 && reopenedWindow.IsVisible; i++)
                    {
                        Dispatcher.UIThread.RunJobs();
                        Thread.Sleep(5);
                    }

                    Assert.True(reopenedEditor.LastApplySucceeded);
                    Assert.False(reopenedWindow.IsVisible);
                    Assert.False(reopenedEditor.IsDraftVisible);
                }
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Native appearance test timed out.");
        if (failure != null) throw new Xunit.Sdk.XunitException(failure.ToString());
    }

    private const int AppearanceTab = 2;

    private static void ExpandPaletteGroups(SettingsWindow settings)
    {
        settings.UpdateLayout();
        foreach (Expander expander in settings.GetVisualDescendants().OfType<Expander>())
        {
            expander.IsExpanded = true;
        }

        Dispatcher.UIThread.RunJobs();
        settings.UpdateLayout();
    }

    public class AppearanceTestApp : Application
    {
        public override void Initialize()
        {
            this.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
            this.Styles.Add(new MaterialIconStyles(null));
            foreach (string name in new[] { "Tokens", "ThemeResources", "ControlStyles" })
            {
                this.Styles.Add(new StyleInclude(new Uri("avares://FocusTimer.App/"))
                {
                    Source = new Uri($"avares://FocusTimer.App/Styles/{name}.axaml"),
                });
            }
        }
    }

    private static void Complete(Task task)
    {
        for (int i = 0; i < 1000 && !task.IsCompleted; i++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        Assert.True(task.IsCompleted, "UI operation did not complete.");
        task.GetAwaiter().GetResult();
    }

    private static void Capture(Window window, string name)
    {
        window.UpdateLayout();
        using var image = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.Bounds.Width), (int)Math.Ceiling(window.Bounds.Height)));
        image.Render(window);
        string folder = System.IO.Path.Combine(AppContext.BaseDirectory, "appearance-evidence");
        System.IO.Directory.CreateDirectory(folder);
        image.Save(System.IO.Path.Combine(folder, name.Replace(' ', '-') + ".png"));
    }

    private sealed class EnabledCommand : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { }
    }

    public sealed class NativeAppearanceFactAttribute : FactAttribute
    {
        public NativeAppearanceFactAttribute()
        {
            if (!OperatingSystem.IsWindows() || Environment.GetEnvironmentVariable("FOCUSTIMER_NATIVE_APPEARANCE_TESTS") != "1")
            {
                this.Skip = "Run separately on Windows with FOCUSTIMER_NATIVE_APPEARANCE_TESTS=1 to isolate native Avalonia state.";
            }
        }
    }
}
