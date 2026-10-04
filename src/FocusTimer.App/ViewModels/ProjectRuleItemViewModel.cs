namespace FocusTimer.App.ViewModels
{
    using FocusTimer.Core.Models;
    using ReactiveUI;

    /// <summary>One editable row of the project rule draft.</summary>
    public sealed class ProjectRuleItemViewModel : ReactiveObject, IRuleRow
    {
        private string _appPattern;
        private string _titlePattern;
        private string _projectName;

        /// <summary>Initializes a new instance of the <see cref="ProjectRuleItemViewModel"/> class.</summary>
        public ProjectRuleItemViewModel()
            : this(null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ProjectRuleItemViewModel"/> class.</summary>
        /// <param name="rule">The rule to edit, or null for a blank row.</param>
        public ProjectRuleItemViewModel(ProjectRule? rule)
        {
            this._appPattern = rule?.AppPattern ?? string.Empty;
            this._titlePattern = rule?.TitlePattern ?? string.Empty;
            this._projectName = rule?.ProjectName ?? string.Empty;
        }

        /// <summary>Gets or sets the application (process name) pattern.</summary>
        public string AppPattern
        {
            get => this._appPattern;
            set
            {
                this.RaiseAndSetIfChanged(ref this._appPattern, value ?? string.Empty);
                this.RaisePropertyChanged(nameof(this.Error));
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
            }
        }

        /// <summary>Gets or sets the project assigned to matching entries.</summary>
        public string ProjectName
        {
            get => this._projectName;
            set
            {
                this.RaiseAndSetIfChanged(ref this._projectName, value ?? string.Empty);
                this.RaisePropertyChanged(nameof(this.Error));
            }
        }

        /// <inheritdoc/>
        public string Error => this.ToRule().IsValid
            ? string.Empty
            : "Enter an application and/or window title pattern, and a project name.";

        /// <summary>Converts the row to a rule with trimmed values.</summary>
        /// <returns>The rule; blank patterns become null.</returns>
        public ProjectRule ToRule() => new(
            string.IsNullOrWhiteSpace(this._appPattern) ? null : this._appPattern.Trim(),
            string.IsNullOrWhiteSpace(this._titlePattern) ? null : this._titlePattern.Trim(),
            string.IsNullOrWhiteSpace(this._projectName) ? null : this._projectName.Trim());
    }
}
