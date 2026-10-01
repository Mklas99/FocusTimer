namespace FocusTimer.Core.Interfaces;

using FocusTimer.Core.Models;

/// <summary>Identifies the group an entry belongs to.</summary>
/// <param name="Key">A stable key; entries with equal keys share a row.</param>
/// <param name="Label">The text shown for the group.</param>
/// <param name="IsUnassigned">Whether this key is the bucket for entries without a value.</param>
public readonly record struct GroupKey(string Key, string Label, bool IsUnassigned = false);
