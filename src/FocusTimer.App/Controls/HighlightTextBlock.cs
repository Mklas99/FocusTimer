namespace FocusTimer.App.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Documents;
    using Avalonia.Media;

    /// <summary>
    /// A text block that makes the parts of its text that match search words bold and underlined. It does not rely on
    /// color, so the matches stay visible in every theme, including High Contrast and on a selected row.
    /// </summary>
    public class HighlightTextBlock : TextBlock
    {
        /// <summary>Identifies <see cref="SourceText"/>.</summary>
        public static readonly StyledProperty<string?> SourceTextProperty =
            AvaloniaProperty.Register<HighlightTextBlock, string?>(nameof(SourceText));

        /// <summary>Identifies <see cref="Terms"/>.</summary>
        public static readonly StyledProperty<IReadOnlyList<string>?> TermsProperty =
            AvaloniaProperty.Register<HighlightTextBlock, IReadOnlyList<string>?>(nameof(Terms));

        /// <summary>Gets or sets the text to show.</summary>
        public string? SourceText
        {
            get => this.GetValue(SourceTextProperty);
            set => this.SetValue(SourceTextProperty, value);
        }

        /// <summary>Gets or sets the words to highlight, ignoring case.</summary>
        public IReadOnlyList<string>? Terms
        {
            get => this.GetValue(TermsProperty);
            set => this.SetValue(TermsProperty, value);
        }

        /// <inheritdoc/>
        protected override Type StyleKeyOverride => typeof(TextBlock);

        /// <summary>
        /// Finds the parts of a text that match any of the words, ignoring case. Overlapping and touching matches are
        /// merged, so each part of the text is covered once.
        /// </summary>
        /// <param name="text">The text to search.</param>
        /// <param name="terms">The words to find; blank words are ignored.</param>
        /// <returns>The matching ranges in text order.</returns>
        public static IReadOnlyList<(int Start, int Length)> FindMatches(string? text, IReadOnlyList<string>? terms)
        {
            var ranges = new List<(int Start, int End)>();
            if (string.IsNullOrEmpty(text) || terms is null)
            {
                return [];
            }

            foreach (var term in terms.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var from = 0;
                while (from <= text.Length - term.Length)
                {
                    var index = text.IndexOf(term, from, StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                    {
                        break;
                    }

                    ranges.Add((index, index + term.Length));
                    from = index + term.Length;
                }
            }

            var merged = new List<(int Start, int Length)>();
            foreach (var (start, end) in ranges.OrderBy(r => r.Start).ThenBy(r => r.End))
            {
                if (merged.Count > 0 && start <= merged[^1].Start + merged[^1].Length)
                {
                    var last = merged[^1];
                    merged[^1] = (last.Start, Math.Max(last.Start + last.Length, end) - last.Start);
                }
                else
                {
                    merged.Add((start, end - start));
                }
            }

            return merged;
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SourceTextProperty || change.Property == TermsProperty)
            {
                this.Rebuild();
            }
        }

        private void Rebuild()
        {
            var text = this.SourceText;
            var matches = FindMatches(text, this.Terms);
            if (matches.Count == 0 || text is null)
            {
                this.Inlines?.Clear();
                this.Text = text;
                return;
            }

            this.Text = null;
            var inlines = this.Inlines ?? new InlineCollection();
            inlines.Clear();
            var position = 0;
            foreach (var (start, length) in matches)
            {
                if (start > position)
                {
                    inlines.Add(new Run(text[position..start]));
                }

                inlines.Add(new Run(text.Substring(start, length))
                {
                    FontWeight = FontWeight.Bold,
                    TextDecorations = Avalonia.Media.TextDecorations.Underline,
                });
                position = start + length;
            }

            if (position < text.Length)
            {
                inlines.Add(new Run(text[position..]));
            }
        }
    }
}
