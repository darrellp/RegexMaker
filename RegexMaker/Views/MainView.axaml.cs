using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.DragCanvas;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Controls;
using RegexMaker.Nodes;
using RegexMaker.Services;
using RegexMaker.ViewModels;

namespace RegexMaker.Views;

public partial class MainView : UserControl
{
    private const double DragThreshold = 5.0;

    private static int offset = -1;
    private RegexMatchColorizer _colorizer;
    private RgxNode? _currentlySelectedNode;
    private RgxNodeControl? _currentlySelectedNodeControl;
    private object? _currentViewModel;
    private MainViewModel? _mainViewModel;
    private RgxNodeControl? _nodeBeingCreated;

    private Point? _toolboxDragStartPoint;
    private WhitespaceMatchBackgroundRenderer? _whitespaceRenderer;

    /// <summary>
    ///     Remembers the last user-set width for the replacement column so it
    ///     persists across RPLC toggle off/on cycles.
    /// </summary>
    private GridLength _lastReplaceColumnWidth = new(1, GridUnitType.Star);

    /// <summary>
    ///     The column definition for the splitter (index 1 in SampleTextGrid).
    /// </summary>
    private ColumnDefinition SplitterColumn => SampleTextGrid.ColumnDefinitions[1];

    /// <summary>
    ///     The column definition for the replace panel (index 2 in SampleTextGrid).
    /// </summary>
    private ColumnDefinition ReplaceColumn => SampleTextGrid.ColumnDefinitions[2];

    public MainView()
    {
        InitializeComponent();
        _colorizer = new RegexMatchColorizer();
        SampleTextEditor.Text = "Replace with Sample Text";

        // Handle DataContext changes
        DataContextChanged += OnDataContextChanged;

        // Exemplars are set up in the static constructor of RgxNode using reflection to find all derived types
        // from RgxNode and create instances of them. We can just loop through those exemplars and create TextBlocks
        // for each of them in the UI.
        foreach (var RgxNode in RgxNode.Exemplars)
        {
            var text = new TextBlock
            {
                Text = RgxNode.Name,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Left,
                Tag = RgxNode.Name // Store node name for later retrieval
            };

            // Enable drag-drop for toolbox items
            text.PointerPressed += OnToolboxItemPointerPressed;
            text.PointerMoved += OnToolboxItemPointerMoved;
            text.PointerReleased += OnToolboxItemPointerReleased;

            SpToolBox.Children.Add(text);
            text.IsVisible = true;
        }

        // Subscribe to caret position changes
        SampleTextEditor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;

        // Subscribe to pointer move for mouse hover
        SampleTextEditor.PointerMoved += OnSampleTextEditorPointerMoved;

        SampleTextEditor.TextChanged += (s, e) => UpdateRegexHighlights();

        // Intercept Enter key to control newline character based on CRLF mode
        SampleTextEditor.TextArea.TextEntered += OnTextAreaTextEntered;

        // Start with the replace panel hidden
        SetReplacePanelVisible(false);
    }

    // Handler for caret movement (text cursor)
    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (_colorizer == null || _colorizer.MatchCollection == null)
            return;
        RetrieveMatchData();
    }

    // Handler for mouse hover (pointer move)
    private void OnSampleTextEditorPointerMoved(object? sender, PointerEventArgs e)
    {
        // May want this in the future

        //var position = e.GetPosition(SampleTextEditor.TextArea.TextView);
        //var logicalPos = SampleTextEditor.TextArea.TextView.GetPositionFloor(position);
        //if (logicalPos != null)
        //{
        //    // Convert logical position to document offset (character index)
        //    var newOffset = SampleTextEditor.Document.GetOffset(logicalPos.Value.Line, logicalPos.Value.Column);
        //    if (newOffset == offset)
        //    {
        //        return;
        //    }
        //    offset = newOffset;
        //    // You can add your logic here
        //}
    }


    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old view model
        if (_mainViewModel != null)
        {
            _mainViewModel.SaveRequested -= OnSaveRequested;
            _mainViewModel.LoadRequested -= OnLoadRequested;
            _mainViewModel.ClearRequested -= OnClearRequested;
            _mainViewModel.CopyRegexRequested -= OnCopyRegexRequested;
            _mainViewModel.ShowCodeRequested -= OnShowCodeRequested;
            _mainViewModel.LineEndingToggled -= OnLineEndingToggled;
            _mainViewModel.ShowWhitespaceToggled -= OnShowWhitespaceToggled;
            _mainViewModel.ShowReplaceToggled -= OnShowReplaceToggled;
            _mainViewModel.ReplacePatternChanged -= OnReplacePatternChanged;
            _mainViewModel.VariableNameChanged -= OnVariableNameChanged;
        }

        // Subscribe to new view model
        if (DataContext is MainViewModel newViewModel)
        {
            _mainViewModel = newViewModel;
            _mainViewModel.SaveRequested += OnSaveRequested;
            _mainViewModel.LoadRequested += OnLoadRequested;
            _mainViewModel.ClearRequested += OnClearRequested;
            _mainViewModel.CopyRegexRequested += OnCopyRegexRequested;
            _mainViewModel.ShowCodeRequested += OnShowCodeRequested;
            _mainViewModel.LineEndingToggled += OnLineEndingToggled;
            _mainViewModel.ShowWhitespaceToggled += OnShowWhitespaceToggled;
            _mainViewModel.ShowReplaceToggled += OnShowReplaceToggled;
            _mainViewModel.ReplacePatternChanged += OnReplacePatternChanged;
            _mainViewModel.VariableNameChanged += OnVariableNameChanged;

            _mainViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.RegexPattern))
                {
                    // Only runs when RegexPattern changes
                    _colorizer = new RegexMatchColorizer(_mainViewModel.RegexPattern);
                    SampleTextEditor.TextArea.TextView.LineTransformers.Clear();
                    SampleTextEditor.TextArea.TextView.LineTransformers.Add(_colorizer);

                    // Ensure background renderer exists for whitespace highlighting
                    if (_whitespaceRenderer == null)
                    {
                        _whitespaceRenderer = new WhitespaceMatchBackgroundRenderer();
                        SampleTextEditor.TextArea.TextView.BackgroundRenderers.Add(_whitespaceRenderer);
                    }

                    UpdateRegexHighlights();
                }
            };

            _mainViewModel.NodeDisplayUpdateRequested += () => { _currentlySelectedNodeControl?.UpdateTextBlock(); };
        }
        else
        {
            _mainViewModel = null;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Handles pointer pressed on toolbox items. </summary>
    /// <remarks>   Darrell Plank, 2/26/2026. </remarks>
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Pointer event information. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    private void OnToolboxItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBlock textBlock && e.GetCurrentPoint(textBlock).Properties.IsLeftButtonPressed)
        {
            _toolboxDragStartPoint = e.GetPosition(textBlock);
            e.Handled = true;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Handles pointer moved on toolbox items to initiate drag. </summary>
    /// <remarks>   Darrell Plank, 2/26/2026. </remarks>
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Pointer event information. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    private /* async */ void OnToolboxItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_toolboxDragStartPoint.HasValue && sender is TextBlock textBlock)
        {
            var currentPoint = e.GetPosition(textBlock);
            var diff = _toolboxDragStartPoint.Value - currentPoint;

            // Check if we've moved beyond the threshold
            if (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold)
            {
                var nodeName = textBlock.Tag as string;
                if (!string.IsNullOrEmpty(nodeName))
                {
                    // Create new node control at cursor position
                    _nodeBeingCreated = new RgxNodeControl
                    {
                        NodeName = nodeName
                    };
                    DragCanvasMain.BeginDragItem(_nodeBeingCreated, e);
                    // Mark the event as handled
                    e.Handled = true;
                }

                _toolboxDragStartPoint = null;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Handles pointer released on toolbox items. </summary>
    /// <remarks>   Darrell Plank, 2/26/2026. </remarks>
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Pointer event information. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    private void OnToolboxItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _toolboxDragStartPoint = null;
        _nodeBeingCreated = null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Handles node selection events from the DragCanvas. </summary>
    /// <remarks>   Darrell Plank, 2/25/2026. </remarks>
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Event information to send to registered event handlers. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    private void OnNodeSelected(object? sender, NodeSelectedEventArgs e)
    {
        if (e.SelectedNode is RgxNodeControl rgxNodeControl)
        {
            Debug.Assert(rgxNodeControl.RgxNode != null, "Selected RgxNodeControl has a null RgxNode");
            _currentlySelectedNodeControl = rgxNodeControl;
            NodeSwitched(rgxNodeControl.RgxNode);
            if (_mainViewModel != null)
            {
                _mainViewModel.RegexPattern = rgxNodeControl.RgxNode.ProduceResult();
                _mainViewModel.SelectedVariableName = rgxNodeControl.VariableName;
            }
        }
    }

    private void OnVariableNameChanged(string? variableName)
    {
        if (_currentlySelectedNodeControl != null)
            _currentlySelectedNodeControl.VariableName = variableName;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Handles the connection event. </summary>
    /// <remarks>
    ///     Connect the RgxNodes in the underlying data model based on the connection created in the UI
    ///     Darrell Plank, 2/25/2026.
    /// </remarks>
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Event information to send to registered event handlers. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    private void OnConnectionCreated(object? sender, ConnectionEventArgs e)
    {
        var rgxNodeControlSource = e.SourceNode as RgxNodeControl;
        var rgxNodeControlTarget = e.TargetNode as RgxNodeControl;
        var rgxNodeSource = rgxNodeControlSource?.RgxNode;
        var rgxNodeTarget = rgxNodeControlTarget?.RgxNode;
        if (rgxNodeSource != null && rgxNodeTarget != null)
        {
            NodeGraphService.Connect(rgxNodeSource, rgxNodeTarget, e.TargetPortIndex);
            UpdateNodeDisplay();
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Handles the connection deleted event from the DragCanvas.
    /// </summary>
    /// <remarks>   Darrell Plank, 2/26/2026. </remarks>
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Event information to send to registered event handlers. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    private void OnConnectionDeleted(object? sender, ConnectionEventArgs e)
    {
        var rgxNodeControlSource = e.SourceNode as RgxNodeControl;
        var rgxNodeControlTarget = e.TargetNode as RgxNodeControl;
        var rgxNodeSource = rgxNodeControlSource?.RgxNode;
        var rgxNodeTarget = rgxNodeControlTarget?.RgxNode;
        if (rgxNodeSource != null && rgxNodeTarget != null)
        {
            NodeGraphService.Disconnect(rgxNodeSource, rgxNodeTarget, e.TargetPortIndex);
            UpdateNodeDisplay();
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Handles the node deleted event from the DragCanvas.
    ///     Updates the underlying RgxNode data model to reflect the deletion.
    ///     The node has already been removed from the canvas at this point.
    /// </summary>
    /// <remarks>   Darrell Plank, 2/26/2026. </remarks>
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Event information to send to registered event handlers. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    private void OnNodeDeleted(object? sender, NodeDeletedEventArgs e)
    {
        if (e.Node is RgxNodeControl rgxNodeControl && rgxNodeControl.RgxNode != null)
        {
            var rgxNode = rgxNodeControl.RgxNode;

            // Clear selection if this is the selected node
            if (_currentlySelectedNode == rgxNode) NodeSwitched(null);

            // NOTE: Do NOT call DragCanvas.DeleteNode() here!
            // The node has already been removed from the canvas by the time this event is raised.
            // This handler only needs to update the application-level data model.
            NodeGraphService.DeleteNode(rgxNode);
            UpdateNodeDisplay();
        }
    }

    private void OnConcatenatePortCountChanged(ConcatenateNode node, int newCount)
    {
        var nodeControl = FindNodeControlForRgxNode(node);
        if (nodeControl == null)
            return;

        var currentCount = node.Parameters.Count;

        if (newCount < currentCount)
            // Remove UI connections to ports being deleted before modifying the model
            for (var i = newCount; i < currentCount; i++)
            {
                var connectionsToRemove = DragCanvasMain.Connections
                    .Where(c => c.TargetNode == nodeControl && c.TargetPortIndex == i)
                    .ToList();

                foreach (var connection in connectionsToRemove) DragCanvasMain.DeleteConnection(connection);
            }

        NodeGraphService.SetPortCount(node, newCount);
        nodeControl.PortCtLeft = newCount;
        UpdateNodeDisplay();
    }

    private void OnAnyOfPortCountChanged(AnyOfNode node, int newCount)
    {
        var nodeControl = FindNodeControlForRgxNode(node);
        if (nodeControl == null)
            return;

        var currentCount = node.Parameters.Count;

        if (newCount < currentCount)
            // Remove UI connections to ports being deleted before modifying the model
            for (var i = newCount; i < currentCount; i++)
            {
                var connectionsToRemove = DragCanvasMain.Connections
                    .Where(c => c.TargetNode == nodeControl && c.TargetPortIndex == i)
                    .ToList();

                foreach (var connection in connectionsToRemove) DragCanvasMain.DeleteConnection(connection);
            }

        NodeGraphService.SetPortCount(node, newCount);
        nodeControl.PortCtLeft = newCount;
        UpdateNodeDisplay();
    }

    private void UnsubscribeFromViewModel()
    {
        if (_currentViewModel is ObservableObject observableObject)
            observableObject.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateNodeDisplay();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Updates the visual display of the selected node. </summary>
    /// <remarks>   Darrell Plank, 2/25/2026. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    private void UpdateNodeDisplay()
    {
        if (_currentlySelectedNode != null)
            if (_mainViewModel != null)
                _mainViewModel.RegexPattern = _currentlySelectedNode.ProduceResult();

        if (_currentlySelectedNodeControl != null) _currentlySelectedNodeControl.UpdateTextBlock();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Shows the parameter page in the carousel corresponding to the given RgxNodeType.
    /// </summary>
    /// <remarks>   Darrell Plank, 2/25/2026. </remarks>
    /// <param name="nodeType"> Type of the node. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal void ShowNodeParameters(RgxNodeType nodeType)
    {
        if (_mainViewModel != null) _mainViewModel.SelectedCarouselIndex = (int)nodeType;
    }

    private async void OnSaveRequested(object? sender, SaveRequestedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Regex Network",
                DefaultExtension = "json",
                SuggestedFileName = "regex_network.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });

            if (file != null)
            {
                // Serialize the canvas
                var canvasData = DragCanvasMain.SerializeCanvas();
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var json = JsonSerializer.Serialize(canvasData, options);

                // Write to file
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(json);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving file: {ex.Message}");
            // TODO: Show error dialog to user
        }
    }

    private async void OnLoadRequested(object? sender, LoadRequestedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Regex Network",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];

                // Read file content
                await using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                // Deserialize
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var canvasData = JsonSerializer.Deserialize<CanvasSerializationData>(json, options);
                if (canvasData != null)
                {
                    // Clear current selection
                    NodeSwitched(null);

                    // Deserialize the canvas with a factory function
                    DragCanvasMain.DeserializeCanvas(canvasData, CreateNodeFromTypeName);

                    if (_mainViewModel != null) _mainViewModel.RegexPattern = string.Empty;

                    // Update display
                    UpdateNodeDisplay();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading file: {ex.Message}");
            // TODO: Show error dialog to user
        }
    }

    private void OnClearRequested(object? sender, EventArgs e)
    {
        // Clear the canvas
        DragCanvasMain.Children.Clear();
        NodeSwitched(null);
        if (_mainViewModel != null) _mainViewModel.RegexPattern = string.Empty;
        UpdateNodeDisplay();
    }

    private RgxNodeControl? CreateNodeFromTypeName(string? typeName)
    {
        if (typeName == "RgxNodeControl") return new RgxNodeControl();
        return null;
    }

    private async void OnCopyRegexRequested(object? sender, CopyRegexRequestedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard != null) await topLevel.Clipboard.SetTextAsync(e.RegexPattern);
    }

    private void OnShowCodeRequested(object? sender, ShowCodeRequestedEventArgs e)
    {
        var codeWindow = new CodeWindow(e.Code);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is Window ownerWindow)
            codeWindow.Show(ownerWindow);
        else
            codeWindow.Show();
    }

    private void UpdateRegexHighlights()
    {
        if (SampleTextEditor?.Document == null || _colorizer == null)
            return;

        var text = SampleTextEditor.Text;
        _colorizer.UpdateMatches(text);

        // Sync match info to the background renderer so whitespace glyphs get highlighted
        _whitespaceRenderer?.UpdateMatches(_colorizer.MatchInfo);

        RetrieveMatchData();

        // Force the editor to redraw so highlights update
        SampleTextEditor.TextArea.TextView.Redraw();

        // Update replacement text whenever sample text changes
        UpdateReplacementResult();
    }

    // Assume you have a reference to your ViewModel as '_mainViewModel'
    private void RetrieveMatchData()
    {
        var result = MatchDataService.GetMatchAtOffset(
            _colorizer?.MatchCollection, _colorizer?.Regex, SampleTextEditor.CaretOffset);

        if (_mainViewModel == null) return;

        if (result != null)
        {
            _mainViewModel.MatchExtent = result.Extent;
            _mainViewModel.Matches = new ObservableCollection<string>(result.Groups);
        }
        else
        {
            _mainViewModel.MatchExtent = string.Empty;
            _mainViewModel.Matches = new ObservableCollection<string>();
        }
    }

    /// <summary>
    ///     Switches the currently selected node and updates related state.
    /// </summary>
    /// <param name="node">The node to switch to, or null to clear selection.</param>
    private void NodeSwitched(RgxNode? node)
    {
        _currentlySelectedNode = node;

        if (_mainViewModel != null)
            _mainViewModel.SelectNode(node, (rgxNode, newCount) =>
            {
                if (rgxNode is ConcatenateNode catNode)
                    OnConcatenatePortCountChanged(catNode, newCount);
                else if (rgxNode is AnyOfNode aoNode) OnAnyOfPortCountChanged(aoNode, newCount);
                return () => { };
            });
    }

    /// <summary>
    ///     Finds the RgxNodeControl associated with the given RgxNode, if any.
    /// </summary>
    /// <param name="node">The RgxNode to find the control for.</param>
    /// <returns>The corresponding RgxNodeControl, or null if not found.</returns>
    private RgxNodeControl? FindNodeControlForRgxNode(RgxNode node)
    {
        foreach (var child in DragCanvasMain.Children)
            if (child is RgxNodeControl control && control.RgxNode == node)
                return control;

        return null;
    }

    private void OnAnyWordFromTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            if (_mainViewModel?.CurrentNodeViewModel is AnyWordFromNodeViewModel vm) vm.AddWordCommand.Execute(null);
            // Focus stays on TextBox since we just cleared it
            e.Handled = true;
        }
    }

    // Add these two methods to handle the LineEndingToggled and ShowWhitespaceToggled events

    private void OnLineEndingToggled(bool useCrLf)
    {
        if (SampleTextEditor?.Document == null)
            return;

        var text = SampleTextEditor.Text;
        if (useCrLf)
            // Convert LF to CRLF (avoid doubling existing CRLF)
            text = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        else
            // Convert CRLF to LF
            text = text.Replace("\r\n", "\n");

        SampleTextEditor.Text = text;
        UpdateRegexHighlights();
    }

    private void OnShowWhitespaceToggled(bool showWhitespace)
    {
        if (SampleTextEditor == null)
            return;

        SampleTextEditor.Options.ShowSpaces = showWhitespace;
        SampleTextEditor.Options.ShowTabs = showWhitespace;
        SampleTextEditor.Options.ShowEndOfLine = showWhitespace;

        // Force redraw to reflect the change
        SampleTextEditor.TextArea.TextView.Redraw();
    }

    private void OnTextAreaTextEntered(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        if (e.Text == null)
            return;

        var useCrLf = _mainViewModel?.UseCrLf ?? false;

        // After text is inserted, check if the document contains wrong newlines and fix them
        if (e.Text.Contains('\n') || e.Text.Contains('\r'))
        {
            var text = SampleTextEditor.Text;
            string corrected;
            if (useCrLf)
                corrected = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
            else
                corrected = text.Replace("\r\n", "\n");

            if (corrected != text)
            {
                var caretOffset = SampleTextEditor.CaretOffset;
                SampleTextEditor.Text = corrected;
                // Adjust caret: if we removed \r characters, offset may shift
                var diff = text.Length - corrected.Length;
                SampleTextEditor.CaretOffset = Math.Max(0, caretOffset - diff);
            }
        }
    }

    #region Replace Panel

    /// <summary>
    ///     Shows or hides the replacement panel, splitter, and column.
    ///     Remembers the last user-dragged column width across toggles.
    /// </summary>
    private void SetReplacePanelVisible(bool visible)
    {
        ReplaceSplitter.IsVisible = visible;
        ReplacePanel.IsVisible = visible;

        if (visible)
        {
            // Restore the remembered width
            if (ReplaceColumn != null)
                ReplaceColumn.Width = _lastReplaceColumnWidth;
            if (SplitterColumn != null)
                SplitterColumn.Width = GridLength.Auto;
        }
        else
        {
            // Save current width before collapsing
            if (ReplaceColumn != null && ReplaceColumn.Width.Value > 0)
                _lastReplaceColumnWidth = ReplaceColumn.Width;

            ReplaceColumn.Width = new GridLength(0);
            SplitterColumn.Width = new GridLength(0);
        }
    }

    private void OnShowReplaceToggled(bool showReplace)
    {
        SetReplacePanelVisible(showReplace);
        if (showReplace)
            UpdateReplacementResult();
    }

    private void OnReplacePatternChanged()
    {
        UpdateReplacementResult();
    }

    /// <summary>
    ///     Runs the regex replacement against the current sample text and
    ///     displays the result in the replacement editor.
    /// </summary>
    private void UpdateReplacementResult()
    {
        if (_mainViewModel == null || !_mainViewModel.ShowReplace)
            return;

        var sampleText = SampleTextEditor?.Text ?? string.Empty;
        _mainViewModel.UpdateReplacementText(sampleText);
        ReplacementResultEditor.Text = _mainViewModel.ReplacementText;
    }

    #endregion
}