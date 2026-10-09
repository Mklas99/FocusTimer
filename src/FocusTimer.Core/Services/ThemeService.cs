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
            const string White = "#FFFFFF";
            const string Black = "#000000";
            const string TitleBarHover = "#1AFFFFFF";
            const string DarkForeground = "#E9EDF5";
            const string DarkSecondaryText = "#AAB7CA";
            const string DarkAccent = "#8ABAF4";
            const string LightForeground = "#273449";
            const string LightSecondaryText = "#57667A";
            const string LightAccent = "#3569AC";
            const string MonokaiBackground = "#272822";
            const string MonokaiSecondaryText = "#BCBCAE";
            const string MonokaiAccent = "#E9AD68";
            const string SolarizedBackground = "#002B36";
            const string SolarizedForeground = "#D3DFDC";
            const string SolarizedSecondaryText = "#A0B5B5";
            const string SolarizedAccent = "#83C7BD";
            const string NordSecondaryText = "#B3C1D4";
            const string NordAccent = "#88C0D0";
            const string DraculaBackground = "#282A36";
            const string DraculaSecondaryText = "#BEB8D3";
            const string DraculaAccent = "#BD9CE8";

            List<Theme> themes = [];

            // Dark Theme (Default)
            themes.Add(new Theme
            {
                ThemeName = "Dark",
                Author = "FocusTimer",
                Version = "1.0",
                WindowBackground = "#1B2029",
                WindowForeground = DarkForeground,
                WindowBorder = "#394353",
                PrimaryText = DarkForeground,
                SecondaryText = DarkSecondaryText,
                DisabledText = "#647186",
                TimerText = DarkForeground,
                TimerBackground = "#00000000",
                ButtonNormal = DarkAccent,
                ButtonHover = "#ACD0FF",
                ButtonPressed = "#6B9EDB",
                ButtonDisabled = "#465164",
                AccentPrimary = DarkAccent,
                AccentSecondary = "#6B9EDB",
                DangerColor = "#E58B98",
                SuccessColor = "#94C6AC",
                WarningColor = "#DFC08A",
                InputBackground = "#262E3B",
                InputBorder = "#465369",
                InputFocusBorder = DarkAccent,
                InputText = DarkForeground,
                ProjectTagBackground = "#262E3B",
                ProjectTagBorder = "#465369",
                ProjectTagText = DarkForeground,
                TitleBarBackground = "#00000000",
                TitleBarForeground = DarkSecondaryText,
                TitleBarButtonHover = TitleBarHover,
                TitleBarButtonPressed = "#33FFFFFF",
                SettingsBackground = "#1F2631",
                SettingsSectionHeader = DarkForeground,
                SettingsLabelText = DarkSecondaryText,
                TabBackground = "#1F2631",
                TabSelectedBackground = DarkAccent,
                TabHoverBackground = "#2B3544",
                TabText = DarkSecondaryText,
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
                WindowForeground = LightForeground,
                WindowBorder = "#C9D2E0",
                PrimaryText = LightForeground,
                SecondaryText = LightSecondaryText,
                DisabledText = "#96A1B1",
                TimerText = LightForeground,
                TimerBackground = "#00000000",
                ButtonNormal = LightAccent,
                ButtonHover = "#285A98",
                ButtonPressed = "#204B80",
                ButtonDisabled = "#DFE5EF",
                AccentPrimary = LightAccent,
                AccentSecondary = "#285A98",
                DangerColor = "#AD465D",
                SuccessColor = "#357859",
                WarningColor = "#8B651E",
                InputBackground = White,
                InputBorder = "#C9D2E0",
                InputFocusBorder = LightAccent,
                InputText = LightForeground,
                ProjectTagBackground = "#EAF0F8",
                ProjectTagBorder = "#C9D2E0",
                ProjectTagText = LightForeground,
                TitleBarBackground = "#00000000",
                TitleBarForeground = LightSecondaryText,
                TitleBarButtonHover = "#1A000000",
                TitleBarButtonPressed = "#33000000",
                SettingsBackground = "#F4F6FA",
                SettingsSectionHeader = LightForeground,
                SettingsLabelText = LightSecondaryText,
                TabBackground = "#F4F6FA",
                TabSelectedBackground = LightAccent,
                TabHoverBackground = "#E5EBF4",
                TabText = LightSecondaryText,
                TabSelectedText = White,
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
                WindowBackground = MonokaiBackground,
                WindowForeground = "#F8F8F2",
                WindowBorder = "#49493F",
                PrimaryText = "#F8F8F2",
                SecondaryText = MonokaiSecondaryText,
                DisabledText = "#75715E",
                TimerText = "#F2E9D5",
                TimerBackground = "#00000000",
                ButtonNormal = MonokaiAccent,
                ButtonHover = "#F4C38B",
                ButtonPressed = "#CD9252",
                ButtonDisabled = "#666658",
                AccentPrimary = MonokaiAccent,
                AccentSecondary = "#CD9252",
                DangerColor = "#ED829A",
                SuccessColor = "#B5CC7D",
                WarningColor = "#D9C27A",
                InputBackground = "#35362F",
                InputBorder = "#5D5F50",
                InputFocusBorder = MonokaiAccent,
                InputText = "#F8F8F2",
                ProjectTagBackground = "#35362F",
                ProjectTagBorder = "#5D5F50",
                ProjectTagText = "#F8F8F2",
                TitleBarBackground = "#00000000",
                TitleBarForeground = MonokaiSecondaryText,
                TitleBarButtonHover = TitleBarHover,
                TitleBarButtonPressed = "#33FFFFFF",
                SettingsBackground = MonokaiBackground,
                SettingsSectionHeader = "#F8F8F2",
                SettingsLabelText = MonokaiSecondaryText,
                TabBackground = MonokaiBackground,
                TabSelectedBackground = MonokaiAccent,
                TabHoverBackground = "#35362F",
                TabText = MonokaiSecondaryText,
                TabSelectedText = MonokaiBackground,
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
                WindowBackground = SolarizedBackground,
                WindowForeground = SolarizedForeground,
                WindowBorder = "#23505A",
                PrimaryText = SolarizedForeground,
                SecondaryText = SolarizedSecondaryText,
                DisabledText = "#586E75",
                TimerText = SolarizedAccent,
                TimerBackground = "#00000000",
                ButtonNormal = SolarizedAccent,
                ButtonHover = "#A2DBD1",
                ButtonPressed = "#60ADA3",
                ButtonDisabled = "#45676E",
                AccentPrimary = SolarizedAccent,
                AccentSecondary = "#60ADA3",
                DangerColor = "#DF8D87",
                SuccessColor = "#ADC184",
                WarningColor = "#D8BC7F",
                InputBackground = "#073642",
                InputBorder = "#45676E",
                InputFocusBorder = SolarizedAccent,
                InputText = SolarizedForeground,
                ProjectTagBackground = "#073642",
                ProjectTagBorder = "#45676E",
                ProjectTagText = SolarizedForeground,
                TitleBarBackground = "#00000000",
                TitleBarForeground = SolarizedSecondaryText,
                TitleBarButtonHover = TitleBarHover,
                TitleBarButtonPressed = "#33FFFFFF",
                SettingsBackground = SolarizedBackground,
                SettingsSectionHeader = SolarizedForeground,
                SettingsLabelText = SolarizedSecondaryText,
                TabBackground = SolarizedBackground,
                TabSelectedBackground = SolarizedAccent,
                TabHoverBackground = "#073642",
                TabText = SolarizedSecondaryText,
                TabSelectedText = SolarizedBackground,
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
                SecondaryText = NordSecondaryText,
                DisabledText = "#4C566A",
                TimerText = NordAccent,
                TimerBackground = "#00000000",
                ButtonNormal = NordAccent,
                ButtonHover = "#ADD4DF",
                ButtonPressed = "#74ADBE",
                ButtonDisabled = "#4C566A",
                AccentPrimary = NordAccent,
                AccentSecondary = "#74ADBE",
                DangerColor = "#BF616A",
                SuccessColor = "#A3BE8C",
                WarningColor = "#EBCB8B",
                InputBackground = "#3B4252",
                InputBorder = "#56647B",
                InputFocusBorder = NordAccent,
                InputText = "#ECEFF4",
                ProjectTagBackground = "#3B4252",
                ProjectTagBorder = "#56647B",
                ProjectTagText = "#ECEFF4",
                TitleBarBackground = "#00000000",
                TitleBarForeground = NordSecondaryText,
                TitleBarButtonHover = TitleBarHover,
                TitleBarButtonPressed = "#33FFFFFF",
                SettingsBackground = "#2E3440",
                SettingsSectionHeader = "#ECEFF4",
                SettingsLabelText = NordSecondaryText,
                TabBackground = "#2E3440",
                TabSelectedBackground = NordAccent,
                TabHoverBackground = "#3B4252",
                TabText = NordSecondaryText,
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
                WindowBackground = DraculaBackground,
                WindowForeground = "#F8F8F2",
                WindowBorder = "#494B63",
                PrimaryText = "#F8F8F2",
                SecondaryText = DraculaSecondaryText,
                DisabledText = "#6272A4",
                TimerText = "#DDD0F3",
                TimerBackground = "#00000000",
                ButtonNormal = DraculaAccent,
                ButtonHover = "#D5BAF7",
                ButtonPressed = "#A583D0",
                ButtonDisabled = "#555670",
                AccentPrimary = DraculaAccent,
                AccentSecondary = "#A583D0",
                DangerColor = "#ED8FAD",
                SuccessColor = "#9BD3AF",
                WarningColor = "#E3D69C",
                InputBackground = "#343746",
                InputBorder = "#595D7A",
                InputFocusBorder = DraculaAccent,
                InputText = "#F8F8F2",
                ProjectTagBackground = "#343746",
                ProjectTagBorder = "#595D7A",
                ProjectTagText = "#F8F8F2",
                TitleBarBackground = "#00000000",
                TitleBarForeground = DraculaSecondaryText,
                TitleBarButtonHover = TitleBarHover,
                TitleBarButtonPressed = "#33FFFFFF",
                SettingsBackground = DraculaBackground,
                SettingsSectionHeader = "#F8F8F2",
                SettingsLabelText = DraculaSecondaryText,
                TabBackground = DraculaBackground,
                TabSelectedBackground = DraculaAccent,
                TabHoverBackground = "#343746",
                TabText = DraculaSecondaryText,
                TabSelectedText = DraculaBackground,
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
                WindowBackground = Black,
                WindowForeground = White,
                WindowBorder = White,
                PrimaryText = White,
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
                InputBackground = Black,
                InputBorder = White,
                InputFocusBorder = "#00FFFF",
                InputText = White,
                ProjectTagBackground = Black,
                ProjectTagBorder = White,
                ProjectTagText = White,
                TitleBarBackground = "#00000000",
                TitleBarForeground = "#FFFF00",
                TitleBarButtonHover = "#40FFFFFF",
                TitleBarButtonPressed = "#80FFFFFF",
                SettingsBackground = Black,
                SettingsSectionHeader = White,
                SettingsLabelText = "#FFFF00",
                TabBackground = Black,
                TabSelectedBackground = "#00FFFF",
                TabHoverBackground = "#1A1A1A",
                TabText = "#FFFF00",
                TabSelectedText = Black,
                BackgroundOpacity = 1.0,
                WidgetBlurMode = WidgetBlurModes.Off,
                TimerOpacity = 1.0,
                ButtonOpacity = 1.0,
            });

            return themes;
        }
    }
}
