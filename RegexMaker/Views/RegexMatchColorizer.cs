// RegexMaker/Views/RegexMatchColorizer.cs
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Avalonia.Media;

public class RegexMatchColorizer : DocumentColorizingTransformer
{
    private readonly Regex _regex;
    private readonly List<IBrush> _highlightBrushes;
    private List<(int Start, int Length, int ColorIndex)> _matches = new();

    public RegexMatchColorizer(string pattern, RegexOptions options = RegexOptions.None)
    {
        _regex = new Regex(pattern, options);
        _highlightBrushes = new List<IBrush>
        {
            new SolidColorBrush(Color.FromRgb(255, 255, 200)), // Light yellow
            new SolidColorBrush(Color.FromRgb(200, 230, 255)), // Light blue
        };
    }

    public void UpdateMatches(string text)
    {
        _matches.Clear();
        int colorIndex = 0;
        foreach (Match match in _regex.Matches(text))
        {
            if (match.Length > 0)
            {
                _matches.Add((match.Index, match.Length, colorIndex));
                colorIndex = (colorIndex + 1) % _highlightBrushes.Count;
            }
        }
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_matches.Count == 0)
            return;

        int lineStart = line.Offset;
        int lineEnd = lineStart + line.Length;

        foreach (var (start, length, colorIndex) in _matches)
        {
            int matchStart = start;
            int matchEnd = start + length;

            // Only colorize if the match is within this line
            if (matchEnd <= lineStart || matchStart >= lineEnd)
                continue;

            int colorStart = Math.Max(matchStart, lineStart);
            int colorEnd = Math.Min(matchEnd, lineEnd);

            ChangeLinePart(
                colorStart,
                colorEnd,
                element =>
                {
                    if (element.TextRunProperties is VisualLineElementTextRunProperties props)
                    {
                        props.SetBackgroundBrush(_highlightBrushes[colorIndex]);
                    }
                }
            );
        }
    }
}