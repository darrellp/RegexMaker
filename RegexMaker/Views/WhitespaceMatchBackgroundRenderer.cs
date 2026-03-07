using System;
using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace RegexMaker.Views;

/// <summary>
///     Draws match-highlight backgrounds behind all characters including whitespace glyphs.
///     AvaloniaEdit's built-in whitespace rendering (ShowSpaces, ShowTabs, ShowEndOfLine)
///     creates special visual elements that bypass DocumentColorizingTransformer, so this
///     IBackgroundRenderer ensures those glyphs still appear on top of the correct match color.
/// </summary>
public class WhitespaceMatchBackgroundRenderer : IBackgroundRenderer
{
    private readonly List<IBrush> _highlightBrushes;
    private List<(int Start, int Length, int ColorIndex)> _matchInfo = new();

    public WhitespaceMatchBackgroundRenderer()
    {
        _highlightBrushes = new List<IBrush>
        {
            new SolidColorBrush(Color.FromRgb(255, 255, 200)), // Light yellow (must match RegexMatchColorizer)
            new SolidColorBrush(Color.FromRgb(200, 230, 255)) // Light blue
        };
    }

    /// <summary>
    ///     Draw behind the text layer so whitespace glyphs render on top.
    /// </summary>
    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_matchInfo.Count == 0)
            return;

        foreach (var visualLine in textView.VisualLines)
        {
            var lineStart = visualLine.FirstDocumentLine.Offset;
            // Use TotalLength to include the line delimiter (\n or \r\n)
            var lineEnd = lineStart + visualLine.LastDocumentLine.TotalLength;

            foreach (var (start, length, colorIndex) in _matchInfo)
            {
                var matchEnd = start + length;

                if (matchEnd <= lineStart || start >= lineEnd)
                    continue;

                var drawStart = Math.Max(start, lineStart);
                var drawEnd = Math.Min(matchEnd, lineEnd);

                var rects = BackgroundGeometryBuilder.GetRectsForSegment(
                    textView, new TextSegment { StartOffset = drawStart, Length = drawEnd - drawStart });

                var brush = _highlightBrushes[colorIndex];
                foreach (var rect in rects) drawingContext.FillRectangle(brush, rect);
            }
        }
    }

    public void UpdateMatches(List<(int Start, int Length, int ColorIndex)> matchInfo)
    {
        _matchInfo = matchInfo;
    }
}