namespace FocusTimer.App.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using FocusTimer.Core.Models;

    /// <summary>The editable draft of the ordered project rules (patterns plus a project name).</summary>
    public sealed class ProjectRuleListViewModel : RuleListViewModelBase<ProjectRuleItemViewModel>
    {
        /// <summary>Initializes a new instance of the <see cref="ProjectRuleListViewModel"/> class.</summary>
        /// <param name="title">The heading shown above the list.</param>
        /// <param name="description">Help text shown under the heading.</param>
        /// <param name="errorText">The validation message shown while any row is invalid.</param>
        public ProjectRuleListViewModel(string title, string description, string errorText)
            : base(title, description, errorText)
        {
        }

        /// <summary>Replaces the draft rows with the given rules.</summary>
        /// <param name="rules">The rules to show.</param>
        public void Load(IEnumerable<ProjectRule> rules) =>
            this.LoadItems(rules.Select(r => new ProjectRuleItemViewModel(r)));

        /// <summary>Converts the draft rows to rules in order.</summary>
        /// <returns>The rules.</returns>
        public List<ProjectRule> ToRules() => this.Rules.Where(r => !r.IsBlank).Select(r => r.ToRule()).ToList();
    }
}
