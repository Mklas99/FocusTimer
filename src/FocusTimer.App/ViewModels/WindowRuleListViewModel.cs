namespace FocusTimer.App.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using FocusTimer.Core.Models;

    /// <summary>The editable draft of an ordered list of window rules (application and/or title patterns).</summary>
    public sealed class WindowRuleListViewModel : RuleListViewModelBase<WindowRuleItemViewModel>
    {
        /// <summary>Initializes a new instance of the <see cref="WindowRuleListViewModel"/> class.</summary>
        /// <param name="title">The heading shown above the list.</param>
        /// <param name="description">Help text shown under the heading.</param>
        /// <param name="errorText">The validation message shown while any row is invalid.</param>
        public WindowRuleListViewModel(string title, string description, string errorText)
            : base(title, description, errorText)
        {
        }

        /// <summary>Replaces the draft rows with the given rules.</summary>
        /// <param name="rules">The rules to show.</param>
        public void Load(IEnumerable<WindowMatchRule> rules) =>
            this.LoadItems(rules.Select(r => new WindowRuleItemViewModel(r)));

        /// <summary>Converts the draft rows to rules in order.</summary>
        /// <returns>The rules.</returns>
        public List<WindowMatchRule> ToRules() => this.Rules.Where(r => !r.IsBlank).Select(r => r.ToRule()).ToList();
    }
}
