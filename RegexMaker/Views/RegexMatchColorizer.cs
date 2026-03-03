// RegexMaker/Views/RegexMatchColorizer.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Avalonia.Media;

public class RegexMatchColorizer : DocumentColorizingTransformer
{
    private readonly Regex _regex;
    private readonly List<IBrush> _highlightBrushes;
    private List<(int Start, int Length, int ColorIndex)> _matchInfo = new();
    private MatchCollection? _matchCollection = null;
    public MatchCollection? MatchCollection => _matchCollection;

    public RegexMatchColorizer(string pattern, RegexOptions options = RegexOptions.None)
    {
        try
        {
            _regex = new Regex(pattern, options);
            _highlightBrushes = new List<IBrush>
            {
                new SolidColorBrush(Color.FromRgb(255, 255, 200)), // Light yellow
                new SolidColorBrush(Color.FromRgb(200, 230, 255)), // Light blue
            };
        }
        catch (Exception ex)
        {
            _regex = new Regex(""); // Fallback to an empty regex to avoid null reference issues
            // Handle regex compilation errors
            Debug.WriteLine($"Error compiling regex: {ex.Message}");
        }
    }

    public void UpdateMatches(string text)
    {
        _matchInfo.Clear();
        int colorIndex = 0;
        _matchCollection = _regex.Matches(text);
        foreach (Match match in MatchCollection)
        {
            if (match.Length > 0)
            {
                _matchInfo.Add((match.Index, match.Length, colorIndex));
                colorIndex = (colorIndex + 1) % _highlightBrushes.Count;
            }
        }
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_matchInfo.Count == 0)
            return;

        int lineStart = line.Offset;
        int lineEnd = lineStart + line.Length;

        foreach (var (start, length, colorIndex) in _matchInfo)
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