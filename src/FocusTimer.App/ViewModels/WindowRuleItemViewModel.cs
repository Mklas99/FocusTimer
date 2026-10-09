namespace FocusTimer.App.ViewModels
{
    using FocusTimer.Core.Models;
    using ReactiveUI;

    /// <summary>One editable row of the exclusion rule draft.</summary>
    public sealed class WindowRuleItemViewModel : ReactiveObject, IRuleRow
    {
        private string _appPattern;
        private string _titlePattern;

        /// <summary>Initializes a new instance of the <see cref="WindowRuleItemViewModel"/> class.</summary>
        public WindowRuleItemViewModel()
            : this(null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="WindowRuleItemViewModel"/> class.</summary>
        /// <param name="rule">The rule to edit, or null for a blank row.</param>
        public WindowRuleItemViewModel(WindowMatchRule? rule)
        {
            this._appPattern = rule?.AppPattern ?? string.Empty;
            this._titlePattern = rule?.TitlePattern ?? string.Empty;
        }

        /// <summary>Gets or sets the application (process name) pattern.</summary>
        public string AppPattern
        {
            get => this._appPattern;
            set
            {
                this.RaiseAndSetIfChanged(ref this._appPattern, value ?? string.Empty);
                this.RaisePropertyChanged(nameof(this.Error));
                this.RaisePropertyChanged(nameof(this.HasError));
                this.RaisePropertyChanged(nameof(this.IsBlank));
                this.RaisePropertyChanged(nameof(this.Warning));
                this.RaisePropertyChanged(nameof(this.HasWarning));
            }
        }

        /// <summary>Gets or sets the window-title pattern.</summary>
        public string TitlePattern
        {
            get => this._titlePattern;
            set
            {
                this.RaiseAndSetIfChanged(ref this._titlePattern, value ?? string.Empty);
                this.RaisePropertyChanged(nameof(this.Error));
                this.RaisePropertyChanged(nameof(this.HasError));
                this.RaisePropertyChanged(nameof(this.IsBlank));
                this.RaisePropertyChanged(nameof(this.Warning));
                this.RaisePropertyChanged(nameof(this.HasWarning));
            }
        }

        /// <inheritdoc/>
        public bool IsBlank =>
            string.IsNullOrWhiteSpace(this._appPattern) && string.IsNullOrWhiteSpace(this._titlePattern);

        /// <inheritdoc/>
        public string Warning => this.IsBlank ? "Empty rule: it is removed when you apply or confirm." : string.Empty;

        /// <inheritdoc/>
        public bool HasWarning => this.IsBlank;

        /// <inheritdoc/>
        public bool HasError => !string.IsNullOrEmpty(this.Error);

        /// <inheritdoc/>
        public string Error => this.IsBlank || this.ToRule().IsValid
            ? string.Empty
            : "Enter an application pattern, a window title pattern, or both.";

        /// <summary>Converts the row to a rule with trimmed patterns.</summary>
        /// <returns>The rule; blank patterns become null.</returns>
        public WindowMatchRule ToRule() => new(
            string.IsNullOrWhiteSpace(this._appPattern) ? null : this._appPattern.Trim(),
            string.IsNullOrWhiteSpace(this._titlePattern) ? null : this._titlePattern.Trim());
    }
}
