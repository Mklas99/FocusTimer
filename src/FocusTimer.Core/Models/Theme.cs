namespace FocusTimer.Core.Models
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a complete theme with all customizable colors for the FocusTimer application.
    /// </summary>
    public class Theme : INotifyPropertyChanged
    {
        // Metadata
        private string _themeName = "Custom";
        private string _author = "User";
        private string _version = "1.0";

        // Core Window Colors
        private string _windowBackground = "#1E1E1E";
        private string _windowForeground = "#FFFFFF";
        private string _windowBorder = "#3F3F46";

        // Text Colors
        private string _primaryText = "#F5F5F5";
        private string _secondaryText = "#CCCCCC";
        private string _disabledText = "#666666";

        // Timer Display
        private string _timerText = "#FFFFFF";
        private string _timerBackground = "#00000000"; // Transparent

        // Buttons & Controls
        private string _buttonNormal = "#4CDEFFBD";
        private string _buttonHover = "#5DEFFCE7";
        private string _buttonPressed = "#3BBD99AC";
        private string _buttonDisabled = "#40FFFFFF";

        // Opacity Controls
        private double _backgroundOpacity = 1.0;
        private double _timerOpacity = 1.0;
        private double _buttonOpacity = 1.0;

        // Accents
        private string _accentPrimary = "#0078D7";
        private string _accentSecondary = "#005A9E";
        private string _dangerColor = "#D9534F";
        private string _successColor = "#4CAF50";
        private string _warningColor = "#FFA726";

        // Input Controls
        private string _inputBackground = "#1AFFFFFF";
        private string _inputBorder = "#35FFFFFF";
        private string _inputFocusBorder = "#0078D7";
        private string _inputText = "#FFFFFF";

        // Project Tag
        private string _projectTagBackground = "#1AFFFFFF";
        private string _projectTagBorder = "#35FFFFFF";
        private string _projectTagText = "#FFFFFF";

        // Title Bar (Custom Window)
        private string _titleBarBackground = "#00000000";
        private string _titleBarForeground = "#CCCCCC";
        private string _titleBarButtonHover = "#1A000000";
        private string _titleBarButtonPressed = "#33000000";

        // Settings Window
        private string _settingsBackground = "#2D2D30";
        private string _settingsSectionHeader = "#F5F5F5";
        private string _settingsLabelText = "#CCCCCC";

        // Tab Control
        private string _tabBackground = "#2D2D30";
        private string _tabSelectedBackground = "#007ACC";
        private string _tabHoverBackground = "#1E1E1E";
        private string _tabText = "#CCCCCC";
        private string _tabSelectedText = "#FFFFFF";

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        // Metadata Properties

        /// <summary>
        /// Gets or sets the theme name.
        /// </summary>
        [JsonPropertyName("themeName")]
        public string ThemeName
        {
            get => this._themeName;
            set => this.SetField(ref this._themeName, value);
        }

        /// <summary>
        /// Gets or sets the theme author.
        /// </summary>
        [JsonPropertyName("author")]
        public string Author
        {
            get => this._author;
            set => this.SetField(ref this._author, value);
        }

        /// <summary>
        /// Gets or sets the theme version.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version
        {
            get => this._version;
            set => this.SetField(ref this._version, value);
        }

        // Window Colors

        /// <summary>
        /// Gets or sets the window background color.
        /// </summary>
        [JsonPropertyName("windowBackground")]
        public string WindowBackground
        {
            get => this._windowBackground;
            set => this.SetField(ref this._windowBackground, value);
        }

        /// <summary>
        /// Gets or sets the window foreground color.
        /// </summary>
        [JsonPropertyName("windowForeground")]
        public string WindowForeground
        {
            get => this._windowForeground;
            set => this.SetField(ref this._windowForeground, value);
        }

        /// <summary>
        /// Gets or sets the window border color.
        /// </summary>
        [JsonPropertyName("windowBorder")]
        public string WindowBorder
        {
            get => this._windowBorder;
            set => this.SetField(ref this._windowBorder, value);
        }

        // Text Colors

        /// <summary>
        /// Gets or sets the primary text color.
        /// </summary>
        [JsonPropertyName("primaryText")]
        public string PrimaryText
        {
            get => this._primaryText;
            set => this.SetField(ref this._primaryText, value);
        }

        /// <summary>
        /// Gets or sets the secondary text color.
        /// </summary>
        [JsonPropertyName("secondaryText")]
        public string SecondaryText
        {
            get => this._secondaryText;
            set => this.SetField(ref this._secondaryText, value);
        }

        /// <summary>
        /// Gets or sets the disabled text color.
        /// </summary>
        [JsonPropertyName("disabledText")]
        public string DisabledText
        {
            get => this._disabledText;
            set => this.SetField(ref this._disabledText, value);
        }

        // Timer Display

        /// <summary>
        /// Gets or sets the timer text color.
        /// </summary>
        [JsonPropertyName("timerText")]
        public string TimerText
        {
            get => this._timerText;
            set => this.SetField(ref this._timerText, value);
        }

        /// <summary>
        /// Gets or sets the timer background color.
        /// </summary>
        [JsonPropertyName("timerBackground")]
        public string TimerBackground
        {
            get => this._timerBackground;
            set => this.SetField(ref this._timerBackground, value);
        }

        // Buttons & Controls

        /// <summary>
        /// Gets or sets the normal button color.
        /// </summary>
        [JsonPropertyName("buttonNormal")]
        public string ButtonNormal
        {
            get => this._buttonNormal;
            set => this.SetField(ref this._buttonNormal, value);
        }

        /// <summary>
        /// Gets or sets the button hover color.
        /// </summary>
        [JsonPropertyName("buttonHover")]
        public string ButtonHover
        {
            get => this._buttonHover;
            set => this.SetField(ref this._buttonHover, value);
        }

        /// <summary>
        /// Gets or sets the button pressed color.
        /// </summary>
        [JsonPropertyName("buttonPressed")]
        public string ButtonPressed
        {
            get => this._buttonPressed;
            set => this.SetField(ref this._buttonPressed, value);
        }

        /// <summary>
        /// Gets or sets the disabled button color.
        /// </summary>
        [JsonPropertyName("buttonDisabled")]
        public string ButtonDisabled
        {
            get => this._buttonDisabled;
            set => this.SetField(ref this._buttonDisabled, value);
        }

        // Accents

        /// <summary>
        /// Gets or sets the primary accent color.
        /// </summary>
        [JsonPropertyName("accentPrimary")]
        public string AccentPrimary
        {
            get => this._accentPrimary;
            set => this.SetField(ref this._accentPrimary, value);
        }

        /// <summary>
        /// Gets or sets the secondary accent color.
        /// </summary>
        [JsonPropertyName("accentSecondary")]
        public string AccentSecondary
        {
            get => this._accentSecondary;
            set => this.SetField(ref this._accentSecondary, value);
        }

        /// <summary>
        /// Gets or sets the danger color.
        /// </summary>
        [JsonPropertyName("dangerColor")]
        public string DangerColor
        {
            get => this._dangerColor;
            set => this.SetField(ref this._dangerColor, value);
        }

        /// <summary>
        /// Gets or sets the success color.
        /// </summary>
        [JsonPropertyName("successColor")]
        public string SuccessColor
        {
            get => this._successColor;
            set => this.SetField(ref this._successColor, value);
        }

        /// <summary>
        /// Gets or sets the warning color.
        /// </summary>
        [JsonPropertyName("warningColor")]
        public string WarningColor
        {
            get => this._warningColor;
            set => this.SetField(ref this._warningColor, value);
        }

        // Input Controls

        /// <summary>
        /// Gets or sets the input background color.
        /// </summary>
        [JsonPropertyName("inputBackground")]
        public string InputBackground
        {
            get => this._inputBackground;
            set => this.SetField(ref this._inputBackground, value);
        }

        /// <summary>
        /// Gets or sets the input border color.
        /// </summary>
        [JsonPropertyName("inputBorder")]
        public string InputBorder
        {
            get => this._inputBorder;
            set => this.SetField(ref this._inputBorder, value);
        }

        /// <summary>
        /// Gets or sets the input focus border color.
        /// </summary>
        [JsonPropertyName("inputFocusBorder")]
        public string InputFocusBorder
        {
            get => this._inputFocusBorder;
            set => this.SetField(ref this._inputFocusBorder, value);
        }

        /// <summary>
        /// Gets or sets the input text color.
        /// </summary>
        [JsonPropertyName("inputText")]
        public string InputText
        {
            get => this._inputText;
            set => this.SetField(ref this._inputText, value);
        }

        // Project Tag

        /// <summary>
        /// Gets or sets the project tag background color.
        /// </summary>
        [JsonPropertyName("projectTagBackground")]
        public string ProjectTagBackground
        {
            get => this._projectTagBackground;
            set => this.SetField(ref this._projectTagBackground, value);
        }

        /// <summary>
        /// Gets or sets the project tag border color.
        /// </summary>
        [JsonPropertyName("projectTagBorder")]
        public string ProjectTagBorder
        {
            get => this._projectTagBorder;
            set => this.SetField(ref this._projectTagBorder, value);
        }

        /// <summary>
        /// Gets or sets the project tag text color.
        /// </summary>
        [JsonPropertyName("projectTagText")]
        public string ProjectTagText
        {
            get => this._projectTagText;
            set => this.SetField(ref this._projectTagText, value);
        }

        // Title Bar

        /// <summary>
        /// Gets or sets the title bar background color.
        /// </summary>
        [JsonPropertyName("titleBarBackground")]
        public string TitleBarBackground
        {
            get => this._titleBarBackground;
            set => this.SetField(ref this._titleBarBackground, value);
        }

        /// <summary>
        /// Gets or sets the title bar foreground color.
        /// </summary>
        [JsonPropertyName("titleBarForeground")]
        public string TitleBarForeground
        {
            get => this._titleBarForeground;
            set => this.SetField(ref this._titleBarForeground, value);
        }

        /// <summary>
        /// Gets or sets the title bar button hover color.
        /// </summary>
        [JsonPropertyName("titleBarButtonHover")]
        public string TitleBarButtonHover
        {
            get => this._titleBarButtonHover;
            set => this.SetField(ref this._titleBarButtonHover, value);
        }

        /// <summary>
        /// Gets or sets the title bar button pressed color.
        /// </summary>
        [JsonPropertyName("titleBarButtonPressed")]
        public string TitleBarButtonPressed
        {
            get => this._titleBarButtonPressed;
            set => this.SetField(ref this._titleBarButtonPressed, value);
        }

        // Settings Window

        /// <summary>
        /// Gets or sets the settings window background color.
        /// </summary>
        [JsonPropertyName("settingsBackground")]
        public string SettingsBackground
        {
            get => this._settingsBackground;
            set => this.SetField(ref this._settingsBackground, value);
        }

        /// <summary>
        /// Gets or sets the settings section header color.
        /// </summary>
        [JsonPropertyName("settingsSectionHeader")]
        public string SettingsSectionHeader
        {
            get => this._settingsSectionHeader;
            set => this.SetField(ref this._settingsSectionHeader, value);
        }

        /// <summary>
        /// Gets or sets the settings label text color.
        /// </summary>
        [JsonPropertyName("settingsLabelText")]
        public string SettingsLabelText
        {
            get => this._settingsLabelText;
            set => this.SetField(ref this._settingsLabelText, value);
        }

        // Tab Control

        /// <summary>
        /// Gets or sets the tab background color.
        /// </summary>
        [JsonPropertyName("tabBackground")]
        public string TabBackground
        {
            get => this._tabBackground;
            set => this.SetField(ref this._tabBackground, value);
        }

        /// <summary>
        /// Gets or sets the tab selected background color.
        /// </summary>
        [JsonPropertyName("tabSelectedBackground")]
        public string TabSelectedBackground
        {
            get => this._tabSelectedBackground;
            set => this.SetField(ref this._tabSelectedBackground, value);
        }

        /// <summary>
        /// Gets or sets the tab hover background color.
        /// </summary>
        [JsonPropertyName("tabHoverBackground")]
        public string TabHoverBackground
        {
            get => this._tabHoverBackground;
            set => this.SetField(ref this._tabHoverBackground, value);
        }

        /// <summary>
        /// Gets or sets the tab text color.
        /// </summary>
        [JsonPropertyName("tabText")]
        public string TabText
        {
            get => this._tabText;
            set => this.SetField(ref this._tabText, value);
        }

        /// <summary>
        /// Gets or sets the tab selected text color.
        /// </summary>
        [JsonPropertyName("tabSelectedText")]
        public string TabSelectedText
        {
            get => this._tabSelectedText;
            set => this.SetField(ref this._tabSelectedText, value);
        }

        // Opacity Controls

        /// <summary>
        /// Gets or sets the background opacity.
        /// </summary>
        [JsonPropertyName("backgroundOpacity")]
        public double BackgroundOpacity
        {
            get => this._backgroundOpacity;
            set
            {
                var clamped = Math.Clamp(value, 0.0, 1.0);
                this.SetField(ref this._backgroundOpacity, clamped);
            }
        }

        /// <summary>
        /// Gets or sets the timer opacity.
        /// </summary>
        [JsonPropertyName("timerOpacity")]
        public double TimerOpacity
        {
            get => this._timerOpacity;
            set
            {
                var clamped = Math.Clamp(value, 0.0, 1.0);
                this.SetField(ref this._timerOpacity, clamped);
            }
        }

        /// <summary>
        /// Gets or sets the button opacity.
        /// </summary>
        [JsonPropertyName("buttonOpacity")]
        public double ButtonOpacity
        {
            get => this._buttonOpacity;
            set
            {
                var clamped = Math.Clamp(value, 0.0, 1.0);
                this.SetField(ref this._buttonOpacity, clamped);
            }
        }

        /// <summary>
        /// Creates a deep copy of this theme.
        /// </summary>
        /// <returns>A new Theme instance with the same property values.</returns>
        public Theme Clone()
        {
            return new Theme
            {
                ThemeName = this.ThemeName,
                Author = this.Author,
                Version = this.Version,
                WindowBackground = this.WindowBackground,
                WindowForeground = this.WindowForeground,
                WindowBorder = this.WindowBorder,
                PrimaryText = this.PrimaryText,
                SecondaryText = this.SecondaryText,
                DisabledText = this.DisabledText,
                TimerText = this.TimerText,
                TimerBackground = this.TimerBackground,
                ButtonNormal = this.ButtonNormal,
                ButtonHover = this.ButtonHover,
                ButtonPressed = this.ButtonPressed,
                ButtonDisabled = this.ButtonDisabled,
                AccentPrimary = this.AccentPrimary,
                AccentSecondary = this.AccentSecondary,
                DangerColor = this.DangerColor,
                SuccessColor = this.SuccessColor,
                WarningColor = this.WarningColor,
                InputBackground = this.InputBackground,
                InputBorder = this.InputBorder,
                InputFocusBorder = this.InputFocusBorder,
                InputText = this.InputText,
                ProjectTagBackground = this.ProjectTagBackground,
                ProjectTagBorder = this.ProjectTagBorder,
                ProjectTagText = this.ProjectTagText,
                TitleBarBackground = this.TitleBarBackground,
                TitleBarForeground = this.TitleBarForeground,
                TitleBarButtonHover = this.TitleBarButtonHover,
                TitleBarButtonPressed = this.TitleBarButtonPressed,
                SettingsBackground = this.SettingsBackground,
                SettingsSectionHeader = this.SettingsSectionHeader,
                SettingsLabelText = this.SettingsLabelText,
                TabBackground = this.TabBackground,
                TabSelectedBackground = this.TabSelectedBackground,
                TabHoverBackground = this.TabHoverBackground,
                TabText = this.TabText,
                TabSelectedText = this.TabSelectedText,
                BackgroundOpacity = this.BackgroundOpacity,
                TimerOpacity = this.TimerOpacity,
                ButtonOpacity = this.ButtonOpacity,
            };
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets a field and raises PropertyChanged if the value changed.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to set.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The name of the property.</param>
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
