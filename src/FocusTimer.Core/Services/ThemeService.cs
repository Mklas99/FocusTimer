namespace FocusTimer.Core.Services
{
    using System.Text.Json;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Service for managing application themes.
    /// </summary>
    public class ThemeService : IThemeService
    {
        private static readonly JsonSerializerOptions DeserializeThemeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private static readonly JsonSerializerOptions SerializeThemeOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly List<Theme> _builtInThemes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeService"/> class.
        /// </summary>
        public ThemeService()
        {
            this._builtInThemes = CreateBuiltInThemes();
            this.CurrentTheme = this._builtInThemes[0].Clone(); // Default to Dark theme
        }

        /// <inheritdoc/>
        public Theme CurrentTheme { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<Theme> BuiltInThemes => this._builtInThemes.AsReadOnly();

        /// <inheritdoc/>
        public void ApplyTheme(Theme theme)
        {
            this.CurrentTheme = theme.Clone();
        }

        /// <inheritdoc/>
        public async Task<Theme> LoadThemeFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Theme file not found: {filePath}");
            }

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                Theme? theme = JsonSerializer.Deserialize<Theme>(json, DeserializeThemeOptions);

                if (theme == null)
                {
                    throw new InvalidOperationException("Failed to deserialize theme file.");
                }

                if (!this.ValidateTheme(theme))
                {
                    throw new InvalidOperationException("Theme file is missing required properties.");
                }

                return theme;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid theme file format: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task SaveThemeToFileAsync(Theme theme, string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(theme, SerializeThemeOptions);

            await File.WriteAllTextAsync(filePath, json);
        }

        /// <inheritdoc/>
        public Theme? GetBuiltInTheme(string themeName)
        {
            return this._builtInThemes.Find(t =>
                t.ThemeName.Equals(themeName, StringComparison.OrdinalIgnoreCase))?.Clone();
        }

        /// <inheritdoc/>
        public void ResetToDefault()
        {
            this.CurrentTheme = this._builtInThemes[0].Clone();
        }

        /// <inheritdoc/>
        public bool ValidateTheme(Theme theme)
        {
            // Check that all required color properties are not null or empty
            return WidgetBlurModes.IsValid(theme.WidgetBlurMode) &&
                   !string.IsNullOrWhiteSpace(theme.WindowBackground) &&
                   !string.IsNullOrWhiteSpace(theme.PrimaryText) &&
                   !string.IsNullOrWhiteSpace(theme.ButtonNormal) &&
                   !string.IsNullOrWhiteSpace(theme.AccentPrimary);
        }

        private static List<Theme> CreateBuiltInThemes()
        {
            List<Theme> themes = [];

            // Dark Theme (Default)
            themes.Add(new Theme
            {
                ThemeName = "Dark",
                Author = "FocusTimer",
                Version = "1.0",
                WindowBackground = "#1B2029",
                WindowForeground = "#E9EDF5",
                WindowBorder = "#394353",
                PrimaryText = "#E9EDF5",
                SecondaryText = "#AAB7CA",
                DisabledText = "#647186",
                TimerText = "#E9EDF5",
                TimerBackground = "#00000000",
                ButtonNormal = "#8ABAF4",
                ButtonHover = "#ACD0FF",
                ButtonPressed = "#6B9EDB",
                ButtonDisabled = "#465164",
                AccentPrimary = "#8ABAF4",
                AccentSecondary = "#6B9EDB",
                DangerColor = "#E58B98",
                SuccessColor = "#94C6AC",
                WarningColor = "#DFC08A",
                InputBackground = "#262E3B",
                InputBorder = "#465369",
                InputFocusBorder = "#8ABAF4",
                InputText = "#E9EDF5",
                ProjectTagBackground = "#262E3B",
                ProjectTagBorder = "#465369",
                ProjectTagText = "#E9EDF5",
                TitleBarBackground = "#00000000",
                TitleBarForeground = "#AAB7CA",
                TitleBarButtonHover = "#1AFFFFFF",
                TitleBarButtonPressed = "#33FFFFFF",
                SettingsBackground = "#1F2631",
                SettingsSectionHeader = "#E9EDF5",
                SettingsLabelText = "#AAB7CA",
                TabBackground = "#1F2631",
                TabSelectedBackground = "#8ABAF4",
                TabHoverBackground = "#2B3544",
                TabText = "#AAB7CA",
                TabSelectedText = "#1B2029",
                BackgroundOpacity = 0.80,
                TimerOpacity = 1.0,
                ButtonOpacity = 0.9,
            });

            // Light Theme
            themes.Add(new Theme
            {
                ThemeName = "Light",
                Author = "FocusTimer",
                Version = "1.0",
                WindowBackground = "#F4F6FA",
                WindowForeground = "#273449",
                WindowBorder = "#C9D2E0",
                PrimaryText = "#273449",
                SecondaryText = "#57667A",
                DisabledText = "#96A1B1",
                TimerText = "#273449",
                TimerBackground = "#00000000",
                ButtonNormal = "#3569AC",
                ButtonHover = "#285A98",
                ButtonPressed = "#204B80",
                ButtonDisabled = "#DFE5EF",
                AccentPrimary = "#3569AC",
                AccentSecondary = "#285A98",
                DangerColor = "#AD465D",
                SuccessColor = "#357859",
                WarningColor = "#8B651E",
                InputBackground = "#FFFFFF",
                InputBorder = "#C9D2E0",
                InputFocusBorder = "#3569AC",
                InputText = "#273449",
                ProjectTagBackground = "#EAF0F8",
                ProjectTagBorder = "#C9D2E0",
                ProjectTagText = "#273449",
                TitleBarBackground = "#00000000",
                TitleBarForeground = "#57667A",
                TitleBarButtonHover = "#1A000000",
                TitleBarButtonPressed = "#33000000",
                SettingsBackground = "#F4F6FA",
                SettingsSectionHeader = "#273449",
                SettingsLabelText = "#57667A",
                TabBackground = "#F4F6FA",
                TabSelectedBackground = "#3569AC",
                TabHoverBackground = "#E5EBF4",
                TabText = "#57667A",
                TabSelectedText = "#FFFFFF",
                BackgroundOpacity = 0.90,
                TimerOpacity = 1.0,
                ButtonOpacity = 0.95,
            });

            // Monokai Theme
            themes.Add(new Theme
            {
                ThemeName = "Monokai",
                Author = "FocusTimer",
                Version = "1.0",
                WindowBackground = "#272822",
                WindowForeground = "#F8F8F2",
                WindowBorder = "#49493F",
                PrimaryText = "#F8F8F2",
                SecondaryText = "#BCBCAE",
                DisabledText = "#75715E",
                TimerText = "#F2E9D5",
                TimerBackground = "#00000000",
                ButtonNormal = "#E9AD68",
                ButtonHover = "#F4C38B",
                ButtonPressed = "#CD9252",
                ButtonDisabled = "#666658",
                AccentPrimary = "#E9AD68",
                AccentSecondary = "#CD9252",
                DangerColor = "#ED829A",
                SuccessColor = "#B5CC7D",
                WarningColor = "#D9C27A",
                InputBackground = "#35362F",
                InputBorder = "#5D5F50",
                InputFocusBorder = "#E9AD68",
                InputText = "#F8F8F2",
                ProjectTagBackground = "#35362F",
                ProjectTagBorder = "#5D5F50",
                ProjectTagText = "#F8F8F2",
                TitleBarBackground = "#00000000",
                TitleBarForeground = "#BCBCAE",
                TitleBarButtonHover = "#1AFFFFFF",
                TitleBarButtonPressed = "#33FFFFFF",
                SettingsBackground = "#272822",
                SettingsSectionHeader = "#F8F8F2",
                SettingsLabelText = "#BCBCAE",
                TabBackground = "#272822",
                TabSelectedBackground = "#E9AD68",
                TabHoverBackground = "#35362F",
                TabText = "#BCBCAE",
                TabSelectedText = "#272822",
                BackgroundOpacity = 0.80,
                TimerOpacity = 1.0,
                ButtonOpacity = 0.88,
            });

            // Solarized Dark Theme
            themes.Add(new Theme
            {
                ThemeName = "Solarized Dark",
                Author = "FocusTimer",
                Version = "1.0",
                WindowBackground = "#002B36",
                WindowForeground = "#D3DFDC",
                WindowBorder = "#23505A",
                PrimaryText = "#D3DFDC",
                SecondaryText = "#A0B5B5",
                DisabledText = "#586E75",
                TimerText = "#83C7BD",
                TimerBackground = "#00000000",
                ButtonNormal = "#83C7BD",
                ButtonHover = "#A2DBD1",
                ButtonPressed = "#60ADA3",
                ButtonDisabled = "#45676E",
                AccentPrimary = "#83C7BD",
                AccentSecondary = "#60ADA3",
                DangerColor = "#DF8D87",
                SuccessColor = "#ADC184",
                WarningColor = "#D8BC7F",
                InputBackground = "#073642",
                InputBorder = "#45676E",
                InputFocusBorder = "#83C7BD",
                InputText = "#D3DFDC",
                ProjectTagBackground = "#073642",
                ProjectTagBorder = "#45676E",
                ProjectTagText = "#D3DFDC",
                TitleBarBackground = "#00000000",
                TitleBarForeground = "#A0B5B5",
                TitleBarButtonHover = "#1AFFFFFF",
                TitleBarButtonPressed = "#33FFFFFF",
                SettingsBackground = "#002B36",
                SettingsSectionHeader = "#D3DFDC",
                SettingsLabelText = "#A0B5B5",
                TabBackground = "#002B36",
                TabSelectedBackground = "#83C7BD",
                TabHoverBackground = "#073642",
                TabText = "#A0B5B5",
                TabSelectedText = "#002B36",
                BackgroundOpacity = 0.80,
                TimerOpacity = 1.0,
                ButtonOpacity = 0.9,
            });

            // Nord Theme
            themes.Add(new Theme
            {
                ThemeName = "Nord",
                Author = "FocusTimer",
                Version = "1.0",
                WindowBackground = "#2E3440",
                WindowForeground = "#ECEFF4",
                WindowBorder = "#3B4252",
                PrimaryText = "#ECEFF4",
                SecondaryText = "#B3C1D4",
                DisabledText = "#4C566A",
                TimerText = "#88C0D0",
                TimerBackground = "#00000000",
                ButtonNormal = "#88C0D0",
                ButtonHover = "#ADD4DF",
                ButtonPressed = "#74ADBE",
                ButtonDisabled = "#4C566A",
                AccentPrimary = "#88C0D0",
                AccentSecondary = "#74ADBE",
                DangerColor = "#BF616A",
                SuccessColor = "#A3BE8C",
                WarningColor = "#EBCB8B",
                InputBackground = "#3B4252",
                InputBorder = "#56647B",
                InputFocusBorder = "#88C0D0",
                InputText = "#ECEFF4",
                ProjectTagBackground = "#3B4252",
                ProjectTagBorder = "#56647B",
                ProjectTagText = "#ECEFF4",
                TitleBarBackground = "#00000000",
                TitleBarForeground = "#B3C1D4",
                TitleBarButtonHover = "#1AFFFFFF",
                TitleBarButtonPressed = "#33FFFFFF",
                SettingsBackground = "#2E3440",
                SettingsSectionHeader = "#ECEFF4",
                SettingsLabelText = "#B3C1D4",
                TabBackground = "#2E3440",
                TabSelectedBackground = "#88C0D0",
                TabHoverBackground = "#3B4252",
                TabText = "#B3C1D4",
                TabSelectedText = "#10151D",
                BackgroundOpacity = 0.80,
                TimerOpacity = 1.0,
                ButtonOpacity = 0.89,
            });

            // Dracula Theme
            themes.Add(new Theme
            {
                ThemeName = "Dracula",
                Author = "FocusTimer",
                Version = "1.0",
                WindowBackground = "#282A36",
                WindowForeground = "#F8F8F2",
                WindowBorder = "#494B63",
                PrimaryText = "#F8F8F2",
                SecondaryText = "#BEB8D3",
                DisabledText = "#6272A4",
                TimerText = "#DDD0F3",
                TimerBackground = "#00000000",
                ButtonNormal = "#BD9CE8",
                ButtonHover = "#D5BAF7",
                ButtonPressed = "#A583D0",
                ButtonDisabled = "#555670",
                AccentPrimary = "#BD9CE8",
                AccentSecondary = "#A583D0",
                DangerColor = "#ED8FAD",
                SuccessColor = "#9BD3AF",
                WarningColor = "#E3D69C",
                InputBackground = "#343746",
                InputBorder = "#595D7A",
                InputFocusBorder = "#BD9CE8",
                InputText = "#F8F8F2",
                ProjectTagBackground = "#343746",
                ProjectTagBorder = "#595D7A",
                ProjectTagText = "#F8F8F2",
                TitleBarBackground = "#00000000",
                TitleBarForeground = "#BEB8D3",
                TitleBarButtonHover = "#1AFFFFFF",
                TitleBarButtonPressed = "#33FFFFFF",
                SettingsBackground = "#282A36",
                SettingsSectionHeader = "#F8F8F2",
                SettingsLabelText = "#BEB8D3",
                TabBackground = "#282A36",
                TabSelectedBackground = "#BD9CE8",
                TabHoverBackground = "#343746",
                TabText = "#BEB8D3",
                TabSelectedText = "#282A36",
                BackgroundOpacity = 0.80,
                TimerOpacity = 1.0,
                ButtonOpacity = 0.92,
            });

            // High Contrast Theme
            themes.Add(new Theme
            {
                ThemeName = "High Contrast",
                Author = "FocusTimer",
                Version = "1.0",
                WindowBackground = "#000000",
                WindowForeground = "#FFFFFF",
                WindowBorder = "#FFFFFF",
                PrimaryText = "#FFFFFF",
                SecondaryText = "#FFFF00",
                DisabledText = "#808080",
                TimerText = "#00FFFF",
                TimerBackground = "#00000000",
                ButtonNormal = "#00FF00",
                ButtonHover = "#00FF00",
                ButtonPressed = "#008000",
                ButtonDisabled = "#808080",
                AccentPrimary = "#00FFFF",
                AccentSecondary = "#0080FF",
                DangerColor = "#FF0000",
                SuccessColor = "#00FF00",
                WarningColor = "#FFFF00",
                InputBackground = "#000000",
                InputBorder = "#FFFFFF",
                InputFocusBorder = "#00FFFF",
                InputText = "#FFFFFF",
                ProjectTagBackground = "#000000",
                ProjectTagBorder = "#FFFFFF",
                ProjectTagText = "#FFFFFF",
                TitleBarBackground = "#00000000",
                TitleBarForeground = "#FFFF00",
                TitleBarButtonHover = "#40FFFFFF",
                TitleBarButtonPressed = "#80FFFFFF",
                SettingsBackground = "#000000",
                SettingsSectionHeader = "#FFFFFF",
                SettingsLabelText = "#FFFF00",
                TabBackground = "#000000",
                TabSelectedBackground = "#00FFFF",
                TabHoverBackground = "#1A1A1A",
                TabText = "#FFFF00",
                TabSelectedText = "#000000",
                BackgroundOpacity = 1.0,
                WidgetBlurMode = WidgetBlurModes.Off,
                TimerOpacity = 1.0,
                ButtonOpacity = 1.0,
            });

            return themes;
        }
    }
}
