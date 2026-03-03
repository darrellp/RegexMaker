using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RegexMaker.Services;

public record MatchResult(string Extent, List<string> Groups);

public static class MatchDataService
{
    /// <summary>
    /// Finds the match containing the given caret offset and returns formatted match data.
    /// Pure logic — no UI dependencies.
    /// </summary>
    public static MatchResult? GetMatchAtOffset(
        MatchCollection? matchCollection, Regex? regex, int caretOffset)
    {
        if (matchCollection == null)
            return null;

        Match? containingMatch = null;
        foreach (Match match in matchCollection)
        {
            if (match.Success && match.Index <= caretOffset && caretOffset < match.Index + match.Length)
            {
                containingMatch = match;
                break;
            }
        }

        if (containingMatch == null)
            return null;

        var extent = $"Range: {containingMatch.Index} to {containingMatch.Index + containingMatch.Length}";
        var groups = new List<string>();

        for (int i = 0; i < containingMatch.Groups.Count; i++)
        {
            var group = containingMatch.Groups[i];
            string name = i.ToString();

            if (regex != null)
            {
                var groupName = regex.GroupNameFromNumber(i);
                if (!string.IsNullOrEmpty(groupName))
                    name = groupName;
            }

            string displayName = name == i.ToString() ? $"<{i}>" : $"<{name}>";
            groups.Add($"{displayName}:  \"{group.Value}\"");
        }

        return new MatchResult(extent, groups);
    }
}