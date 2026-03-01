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
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization.Metadata;

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

        //var node = new CharClassNode(CharClassType.Digit);
        //var options = new JsonSerializerOptions
        //{
        //    WriteIndented = true,
        //    Converters = { new JsonStringEnumConverter() }
        //};
        //options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        //{
        //    Modifiers =
        //    {
        //        ti =>
        //        {
        //            if (ti.Type == typeof(RgxNode))
        //                ti.PolymorphismOptions = new JsonPolymorphismOptions
        //                {
        //                    TypeDiscriminatorPropertyName = "$type",
        //                    IgnoreUnrecognizedTypeDiscriminators = true,
        //                    DerivedTypes =
        //                    {
        //                        new JsonDerivedType(typeof(CharClassNode), "CharClassNode"),
        //                        // Add other derived types as needed
        //                    }
        //                };
        //        }
        //    }
        //};
        //options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        //{
        //    Modifiers =
        //    {
        //        ti =>
        //        {
        //            if (ti.Type == typeof(RgxNode))
        //                ti.PolymorphismOptions = new JsonPolymorphismOptions
        //                {
        //                    TypeDiscriminatorPropertyName = "$type",
        //                    IgnoreUnrecognizedTypeDiscriminators = true,
        //                    DerivedTypes =
        //                    {
        //                        new JsonDerivedType(typeof(CharClassNode), "CharClassNode"),
        //                        // Add other derived types as needed
        //                    }
        //                };
        //        }
        //    }
        //};
        //var json = JsonSerializer.Serialize(node, options);
        //Trace.WriteLine(json); // Should include "CharClass":"Digit"

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

        // Example: Use a simple regex pattern. Replace with your actual pattern.
        //string pattern = "Sample"; // Or bind to your ViewModel's RegexPattern
        //_colorizer = new RegexMatchColorizer(pattern, RegexOptions.IgnoreCase);

        //// Attach the colorizer
        //SampleTextEditor.TextArea.TextView.LineTransformers.Add(_colorizer);

        //// Initial highlight
        //UpdateRegexHighlights();

        // Update highlights when text changes
        SampleTextEditor.TextChanged += (s, e) => UpdateRegexHighlights();
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
                    _colorizer = new RegexMatchColorizer(_mainViewModel.RegexPattern, RegexOptions.IgnoreCase);
                    SampleTextEditor.TextArea.TextView.LineTransformers.Clear();
                    SampleTextEditor.TextArea.TextView.LineTransformers.Add(_colorizer);
                    UpdateRegexHighlights();
                }
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
            var targetNodeIndex = e.TargetPortIndex;
            rgxNodeTarget.Parameters[targetNodeIndex] = rgxNodeSource;
            rgxNodeSource.Parents.Add(rgxNodeTarget);
            rgxNodeTarget.MakeDirty();
            UpdateNodeDisplay();
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Called when a node is selected - updates the carousel and sets up data binding via MainViewModel.
    /// </summary>
    ///
    /// <remarks>   Darrell Plank, 2/25/2026. </remarks>
    ///
    /// <param name="node"> The node. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    private void NodeSwitched(RgxNode? node)
    {
        if (_mainViewModel == null)
            return;

        if (node == null)
        {
            _currentlySelectedNode = null;
            UnsubscribeFromViewModel();
            _currentViewModel = null;
            _mainViewModel.CurrentNodeViewModel = null;
            _mainViewModel.SelectedCarouselIndex = 0;
            return;
        }

        _currentlySelectedNode = node;

        // Unsubscribe from previous ViewModel
        UnsubscribeFromViewModel();

        // Set carousel to the appropriate page via MainViewModel
        _mainViewModel.SelectedCarouselIndex = (int)node.NodeType;

        // Create ViewModel and set it on MainViewModel instead of individual controls
        switch (node.NodeType)
        {
            case RgxNodeType.StringSearch:
                if (node is LiteralNode stringSearchNode)
                {
                    _currentViewModel = new StringSearchNodeViewModel(stringSearchNode, UpdateNodeDisplay);
                    _mainViewModel.CurrentNodeViewModel = _currentViewModel;
                }
                break;

            case RgxNodeType.Repeat:
                if (node is RepeatNode repeatNode)
                {
                    _currentViewModel = new RepeatNodeViewModel(repeatNode, UpdateNodeDisplay);
                    _mainViewModel.CurrentNodeViewModel = _currentViewModel;
                }
                break;

            case RgxNodeType.Range:
                if (node is RangeNode rangeNode)
                {
                    _currentViewModel = new RangeNodeViewModel(rangeNode, UpdateNodeDisplay);
                    _mainViewModel.CurrentNodeViewModel = _currentViewModel;
                }
                break;

            case RgxNodeType.AnyCharFrom:
                if (node is AnyCharFromNode anyCharFromNode)
                {
                    _currentViewModel = new AnyCharFromNodeViewModel(anyCharFromNode, UpdateNodeDisplay);
                    _mainViewModel.CurrentNodeViewModel = _currentViewModel;
                }
                break;

            case RgxNodeType.Concatenate:
                if (node is ConcatenateNode concatenateNode)
                {
                    _currentViewModel = new ConcatenateNodeViewModel(concatenateNode, newCount => OnConcatenatePortCountChanged(concatenateNode, newCount));
                    _mainViewModel.CurrentNodeViewModel = _currentViewModel;
                }
                break;

            case RgxNodeType.AnyOf:
                if (node is AnyOfNode anyOfNode)
                {
                    _currentViewModel = new AnyOfViewModel(anyOfNode, newCount => OnAnyOfPortCountChanged(anyOfNode, newCount));
                    _mainViewModel.CurrentNodeViewModel = _currentViewModel;
                }
                break;

            case RgxNodeType.CharClass:
                if (node is CharClassNode charClassNode)
                {
                    _currentViewModel = new CharClassNodeViewModel(charClassNode, UpdateNodeDisplay);
                    _mainViewModel.CurrentNodeViewModel = _currentViewModel;
                }
                break;

            case RgxNodeType.PatternStart:
            case RgxNodeType.PatternEnd:
                _currentViewModel = null;
                _mainViewModel.CurrentNodeViewModel = null;
                break;
        }

        // Subscribe to new ViewModel's property changes
        if (_currentViewModel is ObservableObject observableObject)
        {
            observableObject.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnConcatenatePortCountChanged(ConcatenateNode node, int newCount)
    {
        var nodeControl = FindNodeControlForRgxNode(node);
        if (nodeControl == null)
            return;

        int currentCount = node.Parameters.Count;
        
        if (newCount > currentCount)
        {
            // Add ports at the bottom
            int portsToAdd = newCount - currentCount;
            for (int i = 0; i < portsToAdd; i++)
            {
                node.Parameters.Add(null);
            }
            nodeControl.PortCtLeft = newCount;
        }
        else if (newCount < currentCount)
        {
            // Remove ports from the bottom
            // First, remove connections to the ports being deleted
            for (int i = newCount; i < currentCount; i++)
            {
                // Find and remove connections to this port
                var connectionsToRemove = DragCanvasMain.Connections
                    .Where(c => c.TargetNode == nodeControl && c.TargetPortIndex == i)
                    .ToList();

                foreach (var connection in connectionsToRemove)
                {
                    DragCanvasMain.DeleteConnection(connection);
                }

                // Clear the parameter
                node.Parameters[i] = null;
            }

            // Now remove the extra parameters
            int portsToRemove = currentCount - newCount;
            for (int i = 0; i < portsToRemove; i++)
            {
                node.Parameters.RemoveAt(node.Parameters.Count - 1);
            }

            nodeControl.PortCtLeft = newCount;
        }

        node.MakeDirty();
        UpdateNodeDisplay();
    }

    private void OnAnyOfPortCountChanged(AnyOfNode node, int newCount)
    {
        var nodeControl = FindNodeControlForRgxNode(node);
        if (nodeControl == null)
            return;

        int currentCount = node.Parameters.Count;
        
        if (newCount > currentCount)
        {
            // Add ports at the bottom
            int portsToAdd = newCount - currentCount;
            for (int i = 0; i < portsToAdd; i++)
            {
                node.Parameters.Add(null);
            }
            nodeControl.PortCtLeft = newCount;
        }
        else if (newCount < currentCount)
        {
            // Remove ports from the bottom
            // First, remove connections to the ports being deleted
            for (int i = newCount; i < currentCount; i++)
            {
                // Find and remove connections to this port
                var connectionsToRemove = DragCanvasMain.Connections
                    .Where(c => c.TargetNode == nodeControl && c.TargetPortIndex == i)
                    .ToList();

                foreach (var connection in connectionsToRemove)
                {
                    DragCanvasMain.DeleteConnection(connection);
                }

                // Clear the parameter
                node.Parameters[i] = null;
            }

            // Now remove the extra parameters
            int portsToRemove = currentCount - newCount;
            for (int i = 0; i < portsToRemove; i++)
            {
                node.Parameters.RemoveAt(node.Parameters.Count - 1);
            }

            nodeControl.PortCtLeft = newCount;
        }

        node.MakeDirty();
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
            var targetNodeIndex = e.TargetPortIndex;
            if (targetNodeIndex >= 0 && targetNodeIndex < rgxNodeTarget.Parameters.Count)
            {
                rgxNodeTarget.Parameters[targetNodeIndex] = null;
            }
            rgxNodeSource.Parents.Remove(rgxNodeTarget);
            rgxNodeTarget.MakeDirty();
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

            // Remove the node from parent references in all its parents
            foreach (var parent in rgxNode.Parents.ToList())
            {
                if (parent is RgxNode parentRgxNode)
                {
                    // Find all parameters that reference this node and set them to null
                    for (int i = 0; i < parentRgxNode.Parameters.Count; i++)
                    {
                        if (parentRgxNode.Parameters[i] == rgxNode)
                        {
                            parentRgxNode.Parameters[i] = null;
                        }
                    }
                    parentRgxNode.MakeDirty();
                }
            }

            // Clear this node's parent list
            rgxNode.Parents.Clear();

            // Clear this node's parameters (disconnect from children)
            for (int i = 0; i < rgxNode.Parameters.Count; i++)
            {
                if (rgxNode.Parameters[i] is RgxNode childNode)
                {
                    childNode.Parents.Remove(rgxNode);
                    childNode.MakeDirty();
                }
                rgxNode.Parameters[i] = null;
            }

            // NOTE: Do NOT call DragCanvas.DeleteNode() here!
            // The node has already been removed from the canvas by the time this event is raised.
            // This handler only needs to update the application-level data model.
            
            UpdateNodeDisplay();
        }
    }

    private RgxNodeControl? FindNodeControlForRgxNode(RgxNode? rgxNode)
    {
        if (rgxNode == null)
            return null;

        foreach (var child in DragCanvasMain.Children)
        {
            if (child is RgxNodeControl nodeControl && nodeControl.RgxNode == rgxNode)
            {
                return nodeControl;
            }
        }

        return null;
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

        // Force the editor to redraw so highlights update
        SampleTextEditor.TextArea.TextView.Redraw();
    }
}