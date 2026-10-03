namespace FocusTimer.App.ViewModels
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Input;
    using FocusTimer.Core.Models;
    using ReactiveUI;

    /// <summary>The editable draft of an ordered list of window rules (rows, ordering, validation).</summary>
    public sealed class WindowRuleListViewModel : ReactiveObject
    {
        private readonly string _errorText;

        /// <summary>Initializes a new instance of the <see cref="WindowRuleListViewModel"/> class.</summary>
        /// <param name="title">The heading shown above the list.</param>
        /// <param name="description">Help text shown under the heading.</param>
        /// <param name="errorText">The validation message shown while any row is invalid.</param>
        public WindowRuleListViewModel(string title, string description, string errorText)
        {
            this.Title = title;
            this.Description = description;
            this._errorText = errorText;
            this.AddCommand = ReactiveCommand.Create(this.Add);
            this.RemoveCommand = ReactiveCommand.Create<WindowRuleItemViewModel>(this.Remove);
            this.MoveUpCommand = ReactiveCommand.Create<WindowRuleItemViewModel>(rule => this.Move(rule, -1));
            this.MoveDownCommand = ReactiveCommand.Create<WindowRuleItemViewModel>(rule => this.Move(rule, 1));
        }

        /// <summary>Occurs when a row is added, removed, moved, or edited.</summary>
        public event System.EventHandler? DraftChanged;

        /// <summary>Gets the heading shown above the list.</summary>
        public string Title { get; }

        /// <summary>Gets the help text shown under the heading.</summary>
        public string Description { get; }

        /// <summary>Gets the rows in evaluation order.</summary>
        public ObservableCollection<WindowRuleItemViewModel> Rules { get; } = new();

        /// <summary>Gets the command that appends a blank row.</summary>
        public ICommand AddCommand { get; }

        /// <summary>Gets the command that removes a row.</summary>
        public ICommand RemoveCommand { get; }

        /// <summary>Gets the command that moves a row earlier.</summary>
        public ICommand MoveUpCommand { get; }

        /// <summary>Gets the command that moves a row later.</summary>
        public ICommand MoveDownCommand { get; }

        /// <summary>Gets validation feedback for the list, empty when every row is valid.</summary>
        public string Error => this.Rules.Any(r => !string.IsNullOrEmpty(r.Error)) ? this._errorText : string.Empty;

        /// <summary>Replaces the draft rows with the given rules.</summary>
        /// <param name="rules">The rules to show.</param>
        public void Load(IEnumerable<WindowMatchRule> rules)
        {
            foreach (WindowRuleItemViewModel item in this.Rules)
            {
                item.PropertyChanged -= this.OnItemChanged;
            }

            this.Rules.Clear();
            foreach (WindowMatchRule rule in rules)
            {
                this.Attach(new WindowRuleItemViewModel(rule));
            }

            this.RaisePropertyChanged(nameof(this.Error));
        }

        /// <summary>Converts the draft rows to rules in order.</summary>
        /// <returns>The rules.</returns>
        public List<WindowMatchRule> ToRules() => this.Rules.Select(r => r.ToRule()).ToList();

        private void Attach(WindowRuleItemViewModel item)
        {
            item.PropertyChanged += this.OnItemChanged;
            this.Rules.Add(item);
        }

        private void OnItemChanged(object? sender, PropertyChangedEventArgs e) => this.NotifyChanged();

        private void NotifyChanged()
        {
            this.RaisePropertyChanged(nameof(this.Error));
            this.DraftChanged?.Invoke(this, System.EventArgs.Empty);
        }

        private void Add()
        {
            this.Attach(new WindowRuleItemViewModel());
            this.NotifyChanged();
        }

        private void Remove(WindowRuleItemViewModel? rule)
        {
            if (rule is null || !this.Rules.Remove(rule))
            {
                return;
            }

            rule.PropertyChanged -= this.OnItemChanged;
            this.NotifyChanged();
        }

        private void Move(WindowRuleItemViewModel? rule, int offset)
        {
            int index = rule is null ? -1 : this.Rules.IndexOf(rule);
            int target = index + offset;
            if (index < 0 || target < 0 || target >= this.Rules.Count)
            {
                return;
            }

            this.Rules.Move(index, target);
            this.NotifyChanged();
        }
    }
}
