namespace FocusTimer.Core.Models
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Application settings/preferences.
    /// </summary>
    public class Settings : INotifyPropertyChanged
    {
        private bool _autoStartOnLogin = false;
        private bool _startMinimized = false;
        private bool _alwaysOnTop = true;
        private int _breakIntervalMinutes = 50;
        private bool _breakRemindersEnabled = true;
        private string _logDirectory = DefaultApplicationLogDirectory;
        private string _worklogDirectory = DefaultWorklogDirectory;
        private int _dataRetentionDays = 90;
        private double _widgetScale = 1.0;
        private double _widgetOpacity = 1.0;
        private bool _useCompactMode;
        private string? _hotkeyShowHide;
        private string? _hotkeyToggleTimer;
        private Theme _theme = new Theme();
        private string _activeThemeName = "Dark";
        private string? _customThemePath;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the default root directory for the application.
        /// </summary>
        public static string DefaultRootDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "FocusTimer");

        /// <summary>
        /// Gets the default directory for application logs.
        /// </summary>
        public static string DefaultApplicationLogDirectory => Path.Combine(DefaultRootDirectory, "logs");

        /// <summary>
        /// Gets the default directory for worklogs.
        /// </summary>
        public static string DefaultWorklogDirectory => Path.Combine(DefaultRootDirectory, "worklogs");

        // General

        /// <summary>
        /// Gets or sets a value indicating whether the application should start automatically on login.
        /// </summary>
        public bool AutoStartOnLogin
        {
            get => this._autoStartOnLogin;
            set => this.SetField(ref this._autoStartOnLogin, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the application should start minimized.
        /// </summary>
        public bool StartMinimized
        {
            get => this._startMinimized;
            set => this.SetField(ref this._startMinimized, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the application window should always stay on top.
        /// </summary>
        public bool AlwaysOnTop
        {
            get => this._alwaysOnTop;
            set => this.SetField(ref this._alwaysOnTop, value);
        }

        // Break reminders

        /// <summary>
        /// Gets or sets the break interval in minutes.
        /// </summary>
        public int BreakIntervalMinutes
        {
            get => this._breakIntervalMinutes;
            set => this.SetField(ref this._breakIntervalMinutes, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether break reminders are enabled.
        /// </summary>
        public bool BreakRemindersEnabled
        {
            get => this._breakRemindersEnabled;
            set => this.SetField(ref this._breakRemindersEnabled, value);
        }

        // Logging

        /// <summary>
        /// Gets or sets the directory for application logs.
        /// </summary>
        public string LogDirectory
        {
            get => this._logDirectory;
            set => this.SetField(ref this._logDirectory, value);
        }

        /// <summary>
        /// Gets or sets the directory for worklogs.
        /// </summary>
        public string WorklogDirectory
        {
            get => this._worklogDirectory;
            set => this.SetField(ref this._worklogDirectory, value);
        }

        /// <summary>
        /// Gets or sets the number of days to retain data.
        /// </summary>
        public int DataRetentionDays
        {
            get => this._dataRetentionDays;
            set => this.SetField(ref this._dataRetentionDays, value);
        }

        // Widget appearance - with validation

        /// <summary>
        /// Gets or sets the scale of the widget.
        /// </summary>
        public double WidgetScale
        {
            get => this._widgetScale;
            set
            {
                var clamped = Math.Clamp(value, 0.5, 3.0);
                this.SetField(ref this._widgetScale, clamped);
            }
        }

        /// <summary>
        /// Gets or sets the opacity of the widget (0.0 to 1.0).
        /// </summary>
        public double WidgetOpacity
        {
            get => this._widgetOpacity;
            set
            {
                var clamped = Math.Clamp(value, 0.0, 1.0);
                this.SetField(ref this._widgetOpacity, clamped);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the widget should use compact mode.
        /// </summary>
        public bool UseCompactMode
        {
            get => this._useCompactMode;
            set => this.SetField(ref this._useCompactMode, value);
        }

        // Future: Hotkey settings (placeholders)

        /// <summary>
        /// Gets or sets the hotkey for showing or hiding the application.
        /// </summary>
        public string? HotkeyShowHide
        {
            get => this._hotkeyShowHide;
            set => this.SetField(ref this._hotkeyShowHide, value);
        }

        /// <summary>
        /// Gets or sets the hotkey for toggling the timer.
        /// </summary>
        public string? HotkeyToggleTimer
        {
            get => this._hotkeyToggleTimer;
            set => this.SetField(ref this._hotkeyToggleTimer, value);
        }

        // Theme Management

        /// <summary>
        /// Gets or sets the current theme settings.
        /// </summary>
        public Theme Theme
        {
            get => this._theme;
            set => this.SetField(ref this._theme, value);
        }

        /// <summary>
        /// Gets or sets the name of the active theme (e.g., "Light", "Dark", "Custom").
        /// </summary>
        public string ActiveThemeName
        {
            get => this._activeThemeName;
            set => this.SetField(ref this._activeThemeName, value);
        }

        /// <summary>
        /// Gets or sets the file path to a custom theme, if ActiveThemeName is "Custom".
        /// </summary>
        public string? CustomThemePath
        {
            get => this._customThemePath;
            set => this.SetField(ref this._customThemePath, value);
        }

        /// <summary>
        /// Raises the PropertyChanged event for a given property name.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the field to the specified value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to set.</param>
        /// <param name="value">The new value for the field.</param>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <returns>True if the field was changed; otherwise, false.</returns>
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
    }
}
