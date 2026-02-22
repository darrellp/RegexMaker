using Avalonia.Controls;
using Avalonia.Layout;

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
            SpToolBox.Children.Add(btn);
            btn.IsVisible = true;
        }

    }
}