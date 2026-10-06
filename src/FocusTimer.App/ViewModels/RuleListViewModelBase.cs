namespace FocusTimer.App.ViewModels
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Input;
    using FocusTimer.Core.Models;
    using ReactiveUI;

    /// <summary>The editable draft of an ordered list of rules (rows, ordering, validation).</summary>
    /// <typeparam name="TItem">The row type.</typeparam>
    public abstract class RuleListViewModelBase<TItem> : ReactiveObject
        where TItem : ReactiveObject, IRuleRow, new()
    {
        /// <summary>Initializes a new instance of the <see cref="RuleListViewModelBase{TItem}"/> class.</summary>
        /// <param name="title">The heading shown above the list.</param>
        /// <param name="description">Help text shown under the heading.</param>
        /// <param name="errorText">The validation message shown while any row is invalid.</param>
        protected RuleListViewModelBase(string title, string description, string errorText)
        {
            this.Title = title;
            this.Description = description;
            this.ErrorText = errorText;
            this.AddCommand = ReactiveCommand.Create(this.Add);
            this.RemoveCommand = ReactiveCommand.Create<TItem>(this.Remove);
            this.MoveUpCommand = ReactiveCommand.Create<TItem>(rule => this.Move(rule, -1));
            this.MoveDownCommand = ReactiveCommand.Create<TItem>(rule => this.Move(rule, 1));
        }

        /// <summary>Occurs when a row is added, removed, moved, or edited.</summary>
        public event System.EventHandler? DraftChanged;

        /// <summary>Gets the heading shown above the list.</summary>
        public string Title { get; }

        /// <summary>Gets the help text shown under the heading.</summary>
        public string Description { get; }

        /// <summary>Gets the rows in evaluation order.</summary>
        public ObservableCollection<TItem> Rules { get; } = [];

        /// <summary>Gets the command that appends a blank row.</summary>
        public ICommand AddCommand { get; }

        /// <summary>Gets the command that removes a row.</summary>
        public ICommand RemoveCommand { get; }

        /// <summary>Gets the command that moves a row earlier.</summary>
        public ICommand MoveUpCommand { get; }

        /// <summary>Gets the command that moves a row later.</summary>
        public ICommand MoveDownCommand { get; }

        /// <summary>Gets validation feedback for the list, empty when every row is valid.</summary>
        public string Error => this.Rules.Any(r => !string.IsNullOrEmpty(r.Error)) ? this.ErrorText : string.Empty;

        private string ErrorText { get; }

        /// <summary>
        /// Removes every fully blank row, as committing settings drops them.
        /// </summary>
        public void PruneBlank()
        {
            foreach (TItem row in this.Rules.Where(r => r.IsBlank).ToList())
            {
                row.PropertyChanged -= this.OnItemChanged;
                this.Rules.Remove(row);
            }

            this.NotifyChanged();
        }

        /// <summary>Replaces the draft rows.</summary>
        /// <param name="items">The rows to show.</param>
        protected void LoadItems(IEnumerable<TItem> items)
        {
            foreach (TItem item in this.Rules)
            {
                item.PropertyChanged -= this.OnItemChanged;
            }

            this.Rules.Clear();
            foreach (TItem item in items)
            {
                this.Attach(item);
            }

            this.RaisePropertyChanged(nameof(this.Error));
        }

        private void Attach(TItem item)
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
            this.Attach(new TItem());
            this.NotifyChanged();
        }

        private void Remove(TItem? rule)
        {
            if (rule is null || !this.Rules.Remove(rule))
            {
                return;
            }

            rule.PropertyChanged -= this.OnItemChanged;
            this.NotifyChanged();
        }

        private void Move(TItem? rule, int offset)
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
