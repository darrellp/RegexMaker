using Avalonia.Controls;
using Avalonia.DragCanvas;
using Avalonia.Layout;
using RegexMaker.Controls;
using RegexMaker.Nodes;
using RegexMaker.ViewModels;

namespace RegexMaker.Views;

public partial class MainView : UserControl
{
    private RgxNode? _currentlySelectedNode;
    private object? _currentViewModel;

    public MainView()
    {
        InitializeComponent();

        // Exemplars are set up in the static constructor of RgxNode using reflection to find all derived types
        // from RgxNode and create instances of them. We can just loop through those exemplars and create buttons
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

    /// <summary>
    /// Handles node selection events from the DragCanvas
    /// </summary>
    private void OnNodeSelected(object? sender, NodeSelectedEventArgs e)
    {
        if (e.SelectedNode is RgxNodeControl rgxNodeControl)
        {
            NodeSwitched(rgxNodeControl.RgxNode);
        }
    }

    /// <summary>
    /// Called when a node is selected - updates the carousel and sets up data binding
    /// </summary>
    private void NodeSwitched(RgxNode? node)
    {
        if (node == null)
        {
            _currentlySelectedNode = null;
            _currentViewModel = null;
            return;
        }

        _currentlySelectedNode = node;

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
                    _currentViewModel = new RepeatNodeViewModel(repeatNode);
                    NumRepeatLeast.DataContext = _currentViewModel;
                    NumRepeatMost.DataContext = _currentViewModel;
                    ChkRepeatIsLazy.DataContext = _currentViewModel;
                }
                break;

            case RgxNodeType.Range:
                if (node is RangeNode rangeNode)
                {
                    _currentViewModel = new RangeNodeViewModel(rangeNode);
                    TxtRangeCharStart.DataContext = _currentViewModel;
                    TxtRangeCharEnd.DataContext = _currentViewModel;
                }
                break;

            case RgxNodeType.Concatenate:
            case RgxNodeType.PatternStart:
            case RgxNodeType.PatternEnd:
            case RgxNodeType.AnyCharFrom:
                // These node types have no editable parameters
                _currentViewModel = null;
                break;
        }
    }

    /// <summary>
    /// Updates the visual display of the selected node
    /// </summary>
    private void UpdateNodeDisplay()
    {
        // Find the RgxNodeControl that contains the current node and refresh its display
        if (_currentlySelectedNode != null)
        {
            foreach (var child in DragCanvasMain.Children)
            {
                if (child is RgxNodeControl control && control.RgxNode == _currentlySelectedNode)
                {
                    // Trigger a refresh of the node's text display
                    control.UpdateTextBlock();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Shows the parameter page in the carousel corresponding to the given RgxNodeType
    /// </summary>
    internal void ShowNodeParameters(RgxNodeType nodeType)
    {
        CarParameters.SelectedIndex = (int)nodeType;
    }
}