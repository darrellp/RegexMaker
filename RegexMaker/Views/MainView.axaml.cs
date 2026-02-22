using Avalonia.Controls;

namespace RegexMaker.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        foreach (var RgxNode in Nodes.RgxNode.Exemplars)
        {
            var btn = new Button
            {
                Content = RgxNode.Name,
                Tag = RgxNode,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };
            SpToolBox.Children.Add(btn);
            btn.IsVisible = true;
        }

    }
}