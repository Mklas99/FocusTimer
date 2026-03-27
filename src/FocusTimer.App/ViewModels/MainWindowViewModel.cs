namespace FocusTimer.App.ViewModels
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// ViewModel for the MainWindow placeholder UI.
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsProvider _settingsProvider;
        private Settings? _settings;
        private string _worklogDirectory = "Loading...";

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        /// <param name="settingsProvider">The settings provider to load application settings.</param>
        public MainWindowViewModel(ISettingsProvider settingsProvider)
        {
            this._settingsProvider = settingsProvider;
            _ = this.LoadSettingsAsync();
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the worklog directory path from settings.
        /// </summary>
        public string WorklogDirectory
        {
            get => this._worklogDirectory;
            private set
            {
                this._worklogDirectory = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task LoadSettingsAsync()
        {
            this._settings = await this._settingsProvider.LoadAsync();
            this.WorklogDirectory = this._settings.WorklogDirectory;
        }
    }
}
