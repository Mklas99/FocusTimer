namespace FocusTimer.App.Views
{
    using System;
    using System.Linq;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.VisualTree;
    using FocusTimer.App.Services;
    using FocusTimer.App.ViewModels;

    /// <summary>
    /// The window that shows tracked entries, their timeline, and the summary for one day at a time.
    /// </summary>
    public partial class WorklogWindow : Window
    {
        private bool _firstActivation = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogWindow"/> class.
        /// </summary>
        public WorklogWindow()
        {
            this.InitializeComponent();
            DesktopWindowMaterial.Attach(this);
            this.Activated += this.OnWindowActivated;
            this.AddHandler(KeyDownEvent, this.OnWindowKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl + F goes to the search box of the Entries tab from anywhere in the window.
            if (e.Key != Key.F || !e.KeyModifiers.HasFlag(KeyModifiers.Control) || this.DataContext is not WorklogWindowViewModel viewModel)
            {
                return;
            }

            e.Handled = true;
            if (viewModel.SelectedTab != WorklogTab.Entries && this.FindControl<TabControl>("Tabs") is { } tabs)
            {
                tabs.SelectedIndex = 0;
            }

            this.UpdateLayout();
            if (this.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.Name == "SearchBox") is { } box)
            {
                box.Focus();
                box.SelectAll();
            }
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            // The first activation is the one that opens the window, which the controller already loaded.
            if (this._firstActivation)
            {
                this._firstActivation = false;
                return;
            }

            if (this.DataContext is WorklogWindowViewModel viewModel)
            {
                _ = viewModel.ActivateAsync();
            }
        }

        private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.Source, sender)
                || this.DataContext is not WorklogWindowViewModel viewModel
                || (sender as TabControl)?.SelectedItem is not TabItem { Tag: string tag }
                || !Enum.TryParse<WorklogTab>(tag, out var tab)
                || tab == viewModel.SelectedTab)
            {
                return;
            }

            _ = viewModel.SelectTabAsync(tab);
        }
    }
}
