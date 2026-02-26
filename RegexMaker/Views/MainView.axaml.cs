using Avalonia.Controls;
using Avalonia.DragCanvas;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Controls;
using RegexMaker.Nodes;
using RegexMaker.ViewModels;
using System.Diagnostics;

namespace RegexMaker.Views;

public partial class MainView : UserControl
{
    private RgxNode? _currentlySelectedNode;
    private RgxNodeControl? _currentlySelectedNodeControl;
    private object? _currentViewModel;

    public MainView()
    {
        InitializeComponent();

        // Exemplars are set up in the static constructor of RgxNode using reflection to find all derived types
        // from RgxNode and create instances of them. We can just loop through those exemplars and create TextBlocks
        // for each of them in the UI.
        foreach (var RgxNode in Nodes.RgxNode.Exemplars)
        {
            var text = new TextBlock
            {
                Text = RgxNode.Name,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            SpToolBox.Children.Add(text);
            text.IsVisible = true;
        }
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
            TxtRegex.Text = rgxNodeControl.RgxNode.ProduceResult();
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
    /// Called when a node is selected - updates the carousel and sets up data binding.
    /// </summary>
    ///
    /// <remarks>   Darrell Plank, 2/25/2026. </remarks>
    ///
    /// <param name="node"> The node. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    private void NodeSwitched(RgxNode? node)
    {
        if (node == null)
        {
            _currentlySelectedNode = null;
            UnsubscribeFromViewModel();
            _currentViewModel = null;
            return;
        }

        _currentlySelectedNode = node;

        // Unsubscribe from previous ViewModel
        UnsubscribeFromViewModel();

        // Set carousel to the appropriate page
        CarParameters.SelectedIndex = (int)node.NodeType;

        // Create ViewModel and set up bindings based on node type
        switch (node.NodeType)
        {
            case RgxNodeType.StringSearch:
                if (node is StringSearchNode stringSearchNode)
                {
                    _currentViewModel = new StringSearchNodeViewModel(stringSearchNode, UpdateNodeDisplay);
                    TxtStringSearchValue.DataContext = _currentViewModel;
                }
                break;

            case RgxNodeType.Repeat:
                if (node is RepeatNode repeatNode)
                {
                    _currentViewModel = new RepeatNodeViewModel(repeatNode, UpdateNodeDisplay);
                    NumRepeatLeast.DataContext = _currentViewModel;
                    NumRepeatMost.DataContext = _currentViewModel;
                    ChkRepeatIsLazy.DataContext = _currentViewModel;
                }
                break;

            case RgxNodeType.Range:
                if (node is RangeNode rangeNode)
                {
                    _currentViewModel = new RangeNodeViewModel(rangeNode, UpdateNodeDisplay);
                    TxtRangeCharStart.DataContext = _currentViewModel;
                    TxtRangeCharEnd.DataContext = _currentViewModel;
                }
                break;

            case RgxNodeType.Concatenate:
            case RgxNodeType.PatternStart:
            case RgxNodeType.PatternEnd:
            case RgxNodeType.AnyCharFrom:
                _currentViewModel = null;
                break;
        }

        // Subscribe to new ViewModel's property changes
        if (_currentViewModel is ObservableObject observableObject)
        {
            observableObject.PropertyChanged += OnViewModelPropertyChanged;
        }
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
            TxtRegex.Text = _currentlySelectedNode.ProduceResult();
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
        CarParameters.SelectedIndex = (int)nodeType;
    }
}