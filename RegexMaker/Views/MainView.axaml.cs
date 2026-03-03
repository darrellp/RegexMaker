using Avalonia;
using Avalonia.Controls;
using Avalonia.DragCanvas;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Controls;
using RegexMaker.Nodes;
using RegexMaker.ViewModels;
using RegexMaker.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization.Metadata;
using System.Collections.ObjectModel;

namespace RegexMaker.Views;

public partial class MainView : UserControl
{
    private RgxNode? _currentlySelectedNode;
    private RgxNodeControl? _currentlySelectedNodeControl;
    private object? _currentViewModel;
    private MainViewModel? _mainViewModel;
    private RegexMatchColorizer _colorizer;

    public MainView()
    {
        InitializeComponent();

        // Handle DataContext changes
        DataContextChanged += OnDataContextChanged;

        // Exemplars are set up in the static constructor of RgxNode using reflection to find all derived types
        // from RgxNode and create instances of them. We can just loop through those exemplars and create TextBlocks
        // for each of them in the UI.
        foreach (var RgxNode in Nodes.RgxNode.Exemplars)
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
    }

    // Handler for caret movement (text cursor)
    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (_colorizer == null || _colorizer.MatchCollection == null)
            return;
        RetrieveMatchData();
    }

    //private void RetrieveMatchInfo()
    //{
    //    int caretOffset = SampleTextEditor.CaretOffset;

    //    Debug.Assert(_colorizer is not null);

    //    foreach (Match match in _colorizer.MatchCollection)
    //    {
    //        var matchStart = match.Index;
    //        if (caretOffset >= matchStart && caretOffset <= matchStart + match.Length)
    //        {
    //            var captures = match.Captures;
    //        }
    //    }
    //}

    static int offset = -1;
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
        }

        // Subscribe to new view model
        if (DataContext is MainViewModel newViewModel)
        {
            _mainViewModel = newViewModel;
            _mainViewModel.SaveRequested += OnSaveRequested;
            _mainViewModel.LoadRequested += OnLoadRequested;
            _mainViewModel.ClearRequested += OnClearRequested;
            _mainViewModel.CopyRegexRequested += OnCopyRegexRequested;

            _mainViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.RegexPattern))
                {
                    // Only runs when RegexPattern changes
                    _colorizer = new RegexMatchColorizer(_mainViewModel.RegexPattern);
                    SampleTextEditor.TextArea.TextView.LineTransformers.Clear();
                    SampleTextEditor.TextArea.TextView.LineTransformers.Add(_colorizer);
                    UpdateRegexHighlights();
                }
            };

            _mainViewModel.NodeDisplayUpdateRequested += () =>
            {
                _currentlySelectedNodeControl?.UpdateTextBlock();
            };
        }
        else
        {
            _mainViewModel = null;
        }
    }

    private Point? _toolboxDragStartPoint;
    private const double DragThreshold = 5.0;
    private RgxNodeControl? _nodeBeingCreated;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Handles pointer pressed on toolbox items. </summary>
    ///
    /// <remarks>   Darrell Plank, 2/26/2026. </remarks>
    ///
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
    ///
    /// <remarks>   Darrell Plank, 2/26/2026. </remarks>
    ///
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Pointer event information. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    private async void OnToolboxItemPointerMoved(object? sender, PointerEventArgs e)
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
                    // Get position relative to canvas
                    var canvasPosition = e.GetPosition(DragCanvasMain);

                    // Create new node control at cursor position
                    _nodeBeingCreated = new RgxNodeControl
                    {
                        NodeName = nodeName
                    };

                    // Set position on canvas
                    Canvas.SetLeft(_nodeBeingCreated, canvasPosition.X - 50);
                    Canvas.SetTop(_nodeBeingCreated, canvasPosition.Y - 15);

                    // Add to canvas
                    DragCanvasMain.Children.Add(_nodeBeingCreated);

                    // Force layout pass so the control is properly positioned
                    DragCanvasMain.UpdateLayout();

                    // Capture the pointer on the canvas so subsequent events go there
                    e.Pointer.Capture(DragCanvasMain);

                    // Directly initiate the drag operation
                    DragCanvasMain.BeginDrag(_nodeBeingCreated, canvasPosition);

                    // Mark the event as handled
                    e.Handled = true;
                }

                _toolboxDragStartPoint = null;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Handles pointer released on toolbox items. </summary>
    ///
    /// <remarks>   Darrell Plank, 2/26/2026. </remarks>
    ///
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
    ///
    /// <remarks>   Darrell Plank, 2/25/2026. </remarks>
    ///
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
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Handles the connection event. </summary>
    ///
    /// <remarks>   Connect the RgxNodes in the underlying data model based on the connection created in the UI
    ///             Darrell Plank, 2/25/2026. </remarks>
    ///
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
    /// Handles the connection deleted event from the DragCanvas.
    /// </summary>
    ///
    /// <remarks>   Darrell Plank, 2/26/2026. </remarks>
    ///
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
    /// Handles the node deleted event from the DragCanvas.
    /// Updates the underlying RgxNode data model to reflect the deletion.
    /// The node has already been removed from the canvas at this point.
    /// </summary>
    ///
    /// <remarks>   Darrell Plank, 2/26/2026. </remarks>
    ///
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Event information to send to registered event handlers. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    private void OnNodeDeleted(object? sender, NodeDeletedEventArgs e)
    {
        if (e.Node is RgxNodeControl rgxNodeControl && rgxNodeControl.RgxNode != null)
        {
            var rgxNode = rgxNodeControl.RgxNode;

            // Clear selection if this is the selected node
            if (_currentlySelectedNode == rgxNode)
            {
                NodeSwitched(null);
            }

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

        int currentCount = node.Parameters.Count;

        if (newCount < currentCount)
        {
            // Remove UI connections to ports being deleted before modifying the model
            for (int i = newCount; i < currentCount; i++)
            {
                var connectionsToRemove = DragCanvasMain.Connections
                    .Where(c => c.TargetNode == nodeControl && c.TargetPortIndex == i)
                    .ToList();

                foreach (var connection in connectionsToRemove)
                {
                    DragCanvasMain.DeleteConnection(connection);
                }
            }
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

        int currentCount = node.Parameters.Count;

        if (newCount < currentCount)
        {
            // Remove UI connections to ports being deleted before modifying the model
            for (int i = newCount; i < currentCount; i++)
            {
                var connectionsToRemove = DragCanvasMain.Connections
                    .Where(c => c.TargetNode == nodeControl && c.TargetPortIndex == i)
                    .ToList();

                foreach (var connection in connectionsToRemove)
                {
                    DragCanvasMain.DeleteConnection(connection);
                }
            }
        }

        NodeGraphService.SetPortCount(node, newCount);
        nodeControl.PortCtLeft = newCount;
        UpdateNodeDisplay();
    }

    private void UnsubscribeFromViewModel()
    {
        if (_currentViewModel is ObservableObject observableObject)
        {
            observableObject.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdateNodeDisplay();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Updates the visual display of the selected node. </summary>
    ///
    /// <remarks>   Darrell Plank, 2/25/2026. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    private void UpdateNodeDisplay()
    {
        if (_currentlySelectedNode != null)
        {
            if (_mainViewModel != null)
            {
                _mainViewModel.RegexPattern = _currentlySelectedNode.ProduceResult();
            }
        }

        if (_currentlySelectedNodeControl != null)
        {
            _currentlySelectedNodeControl.UpdateTextBlock();
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Shows the parameter page in the carousel corresponding to the given RgxNodeType.
    /// </summary>
    ///
    /// <remarks>   Darrell Plank, 2/25/2026. </remarks>
    ///
    /// <param name="nodeType"> Type of the node. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    internal void ShowNodeParameters(RgxNodeType nodeType)
    {
        if (_mainViewModel != null)
        {
            _mainViewModel.SelectedCarouselIndex = (int)nodeType;
        }
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

                    if (_mainViewModel != null)
                    {
                        _mainViewModel.RegexPattern = string.Empty;
                    }

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
        if (_mainViewModel != null)
        {
            _mainViewModel.RegexPattern = string.Empty;
        }
        UpdateNodeDisplay();
    }

    private RgxNodeControl? CreateNodeFromTypeName(string? typeName)
    {
        if (typeName == "RgxNodeControl")
        {
            return new RgxNodeControl();
        }
        return null;
    }
    private async void OnCopyRegexRequested(object? sender, CopyRegexRequestedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard != null)
        {
            await topLevel.Clipboard.SetTextAsync(e.RegexPattern);
        }
    }

    private void UpdateRegexHighlights()
    {
        if (SampleTextEditor?.Document == null || _colorizer == null)
            return;

        string text = SampleTextEditor.Text;
        _colorizer.UpdateMatches(text);
        RetrieveMatchData();

        // Force the editor to redraw so highlights update
        SampleTextEditor.TextArea.TextView.Redraw();
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
    /// Switches the currently selected node and updates related state.
    /// </summary>
    /// <param name="node">The node to switch to, or null to clear selection.</param>
    private void NodeSwitched(RgxNode? node)
    {
        _currentlySelectedNode = node;
        // You may want to update the UI or ViewModel here as needed
        // For example, clear or update parameter panels, etc.
    }

    /// <summary>
    /// Finds the RgxNodeControl associated with the given RgxNode, if any.
    /// </summary>
    /// <param name="node">The RgxNode to find the control for.</param>
    /// <returns>The corresponding RgxNodeControl, or null if not found.</returns>
    private RgxNodeControl? FindNodeControlForRgxNode(RgxNode node)
    {
        foreach (var child in DragCanvasMain.Children)
        {
            if (child is RgxNodeControl control && control.RgxNode == node)
            {
                return control;
            }
        }
        return null;
    }
}