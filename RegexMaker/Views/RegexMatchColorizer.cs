// RegexMaker/Views/RegexMatchColorizer.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

public class RegexMatchColorizer : DocumentColorizingTransformer
{
    private readonly List<IBrush> _highlightBrushes;

    public RegexMatchColorizer(string pattern = "", RegexOptions options = RegexOptions.None)
    {
        _highlightBrushes = [];
        try
        {
            Regex = new Regex(pattern, options);
            _highlightBrushes =
            [
                new SolidColorBrush(Color.FromRgb(255, 255, 200)), // Light yellow
                new SolidColorBrush(Color.FromRgb(200, 230, 255)) // Light blue
            ];
        }
        catch (Exception ex)
        {
            Regex = new Regex(""); // Fallback to an empty regex to avoid null reference issues
            // Handle regex compilation errors
            Debug.WriteLine($"Error compiling regex: {ex.Message}");
        }
    }

    public MatchCollection? MatchCollection { get; private set; }

    public Regex Regex { get; }

    /// <summary>
    ///     Exposes match info so a background renderer can draw highlights behind whitespace glyphs.
    /// </summary>
    public List<(int Start, int Length, int ColorIndex)> MatchInfo { get; } = new();

    public void UpdateMatches(string text)
    {
        MatchInfo.Clear();
        var colorIndex = 0;
        MatchCollection = Regex.Matches(text);
        if (MatchCollection is null) return;
        foreach (Match match in MatchCollection!)
            if (match.Length > 0)
            {
                MatchInfo.Add((match.Index, match.Length, colorIndex));
                colorIndex = (colorIndex + 1) % _highlightBrushes.Count;
            }
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (MatchInfo.Count == 0)
            return;

        var lineStart = line.Offset;
        var lineEnd = lineStart + line.Length;

        foreach (var (start, length, colorIndex) in MatchInfo)
        {
            var matchStart = start;
            var matchEnd = start + length;

            // Only colorize if the match is within this line
            if (matchEnd <= lineStart || matchStart >= lineEnd)
                continue;

            var colorStart = Math.Max(matchStart, lineStart);
            var colorEnd = Math.Min(matchEnd, lineEnd);

            ChangeLinePart(
                colorStart,
                colorEnd,
                element =>
                {
                    if (element.TextRunProperties is VisualLineElementTextRunProperties props)
                        props.SetBackgroundBrush(_highlightBrushes[colorIndex]);
                }
            );
        }
    }
}