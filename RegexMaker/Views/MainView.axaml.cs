using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using RegexMaker.Nodes;

namespace RegexMaker.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        // Exemplars are set up in the static constructor of RgxNode using reflection to find all derived types
        // from RgxNode and create instances of them. We can just loop through those exemplars and create buttons
        // for each of them in the UI.
        foreach (var RgxNode in Nodes.RgxNode.Exemplars)
        {
            var btn = new Button
            {
                Content = RgxNode.Name,
                Tag = RgxNode,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
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