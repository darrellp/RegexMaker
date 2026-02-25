using Avalonia;
using Avalonia.Controls;
using Avalonia.DragCanvas;
using Avalonia.Interactivity;
using Avalonia.Layout;
using RegexMaker.Controls;
using RegexMaker.Nodes;

namespace RegexMaker.Views;

public partial class MainView : UserControl
{
    private RgxNode? _currentlySelectedNode;
    private bool _isUpdatingFromNode;

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
            return;
        }

        _currentlySelectedNode = node;
        _isUpdatingFromNode = true;

        // Set carousel to the appropriate page
        CarParameters.SelectedIndex = (int)node.NodeType;

        // Load current values from the node into the UI
        switch (node.NodeType)
        {
            case RgxNodeType.StringSearch:
                if (node is StringSearchNode stringSearchNode)
                {
                    TxtStringSearchValue.Text = stringSearchNode.SearchString;
                }
                break;

            case RgxNodeType.Repeat:
                if (node is RepeatNode repeatNode)
                {
                    NumRepeatLeast.Value = repeatNode.Least;
                    NumRepeatMost.Value = repeatNode.Most;
                    ChkRepeatIsLazy.IsChecked = repeatNode.IsLazy;
                }
                break;

            case RgxNodeType.Range:
                if (node is RangeNode rangeNode)
                {
                    TxtRangeCharStart.Text = rangeNode.CharStart.ToString();
                    TxtRangeCharEnd.Text = rangeNode.CharEnd.ToString();
                }
                break;

            case RgxNodeType.Concatenate:
            case RgxNodeType.PatternStart:
            case RgxNodeType.PatternEnd:
            case RgxNodeType.AnyCharFrom:
                // These node types have no editable parameters
                break;
        }

        _isUpdatingFromNode = false;
    }

    /// <summary>
    /// Handles changes to the StringSearch value
    /// </summary>
    private void OnStringSearchValueChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_isUpdatingFromNode || _currentlySelectedNode == null)
            return;

        if (e.Property.Name == nameof(TextBox.Text) && _currentlySelectedNode is StringSearchNode stringSearchNode)
        {
            stringSearchNode.SearchString = TxtStringSearchValue.Text ?? string.Empty;
            _currentlySelectedNode.MakeDirty();
            UpdateNodeDisplay();
        }
    }

    /// <summary>
    /// Handles changes to the Repeat NumericUpDown values
    /// </summary>
    private void OnRepeatNumericValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        UpdateRepeatNode();
    }

    /// <summary>
    /// Handles changes to the Repeat CheckBox value
    /// </summary>
    private void OnRepeatCheckBoxChanged(object? sender, RoutedEventArgs e)
    {
        UpdateRepeatNode();
    }

    /// <summary>
    /// Updates the RepeatNode with current UI values
    /// </summary>
    private void UpdateRepeatNode()
    {
        if (_isUpdatingFromNode || _currentlySelectedNode == null)
            return;

        if (_currentlySelectedNode is RepeatNode repeatNode)
        {
            repeatNode.Least = (int)(NumRepeatLeast.Value ?? 0);
            repeatNode.Most = (int)(NumRepeatMost.Value ?? -1);
            repeatNode.IsLazy = ChkRepeatIsLazy.IsChecked ?? false;
            _currentlySelectedNode.MakeDirty();
        }
    }

    /// <summary>
    /// Handles changes to the Range node values
    /// </summary>
    private void OnRangeValueChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_isUpdatingFromNode || _currentlySelectedNode == null)
            return;

        if (e.Property.Name == nameof(TextBox.Text) && _currentlySelectedNode is RangeNode rangeNode)
        {
            var startText = TxtRangeCharStart.Text;
            var endText = TxtRangeCharEnd.Text;

            if (!string.IsNullOrEmpty(startText))
                rangeNode.CharStart = startText;

            if (!string.IsNullOrEmpty(endText))
                rangeNode.CharEnd = endText;

            _currentlySelectedNode.MakeDirty();
        }
    }

    /// <summary>
    /// Updates the visual display of the selected node (if it's a StringSearchNode)
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

    /// <summary>
    /// Gets parameter values from the carousel for StringSearch node
    /// </summary>
    public string GetStringSearchValue()
    {
        return TxtStringSearchValue.Text ?? string.Empty;
    }

    /// <summary>
    /// Sets parameter values in the carousel for StringSearch node
    /// </summary>
    public void SetStringSearchValue(string value)
    {
        TxtStringSearchValue.Text = value;
    }

    /// <summary>
    /// Gets parameter values from the carousel for Repeat node
    /// </summary>
    public (int Least, int Most, bool IsLazy) GetRepeatValues()
    {
        return (
            (int)(NumRepeatLeast.Value ?? 0),
            (int)(NumRepeatMost.Value ?? -1),
            ChkRepeatIsLazy.IsChecked ?? false
        );
    }

    /// <summary>
    /// Sets parameter values in the carousel for Repeat node
    /// </summary>
    public void SetRepeatValues(int least, int most, bool isLazy)
    {
        NumRepeatLeast.Value = least;
        NumRepeatMost.Value = most;
        ChkRepeatIsLazy.IsChecked = isLazy;
    }

    /// <summary>
    /// Gets parameter values from the carousel for Range node
    /// </summary>
    public (string CharStart, string CharEnd) GetRangeValues()
    {
        return (
            TxtRangeCharStart.Text ?? "a",
            TxtRangeCharEnd.Text ?? "z"
        );
    }

    /// <summary>
    /// Sets parameter values in the carousel for Range node
    /// </summary>
    public void SetRangeValues(string charStart, string charEnd)
    {
        TxtRangeCharStart.Text = charStart;
        TxtRangeCharEnd.Text = charEnd;
    }
}