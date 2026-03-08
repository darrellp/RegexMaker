using Avalonia.DragCanvas;

namespace RegexMaker.Models;

/// <summary>
///     Top-level data transfer object for the regex maker save file.
///     Wraps the canvas data together with the sample text and replacement string.
/// </summary>
public class RegexFileData
{
    /// <summary>
    ///     The serialized node/connection graph.
    /// </summary>
    public CanvasSerializationData? CanvasData { get; set; }

    /// <summary>
    ///     The sample text shown in the editor.
    /// </summary>
    public string? SampleText { get; set; }

    /// <summary>
    ///     The replacement pattern string.
    /// </summary>
    public string? ReplacePattern { get; set; }
}