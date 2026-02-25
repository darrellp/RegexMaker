using Avalonia.Controls;
using Avalonia.DragCanvas;
using RegexMaker.Nodes;
using Avalonia;
using Avalonia.Media;

namespace RegexMaker.Controls;

public class RgxNodeControl : DragCanvasNode
{
    private RgxNode? _rgxNode;
    private readonly TextBlock _textBlock;

    public static readonly StyledProperty<string?> NodeNameProperty =
        AvaloniaProperty.Register<RgxNodeControl, string?>(
            nameof(NodeName),
            defaultValue: null);

    public string? NodeName
    {
        get => GetValue(NodeNameProperty);
        set => SetValue(NodeNameProperty, value);
    }

    public RgxNode? RgxNode
    {
        get => _rgxNode;
        private set
        {
            _rgxNode = value;
            UpdateTextBlock();
            PortCtRight = 1;
            PortCtLeft = _rgxNode != null ? _rgxNode.Parameters.Count : 0;
        }
    }

    public RgxNodeControl()
    {
        _textBlock = new TextBlock
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Thickness(10)
        };

        var border = new Border
        {
            Background = new SolidColorBrush(Colors.LightGray),
            CornerRadius = new CornerRadius(5),
            Child = _textBlock
        };

        Content = border;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == NodeNameProperty)
        {
            var nodeName = change.GetNewValue<string?>();
            if (!string.IsNullOrEmpty(nodeName))
            {
                try
                {
                    RgxNode = RgxNode.NameToNode(nodeName);
                }
                catch
                {
                    RgxNode = null;
                }
            }
            else
            {
                RgxNode = null;
            }
        }
    }

    private void UpdateTextBlock()
    {
        if (_rgxNode is StringSearchNode literal)
        {
            _textBlock.Text = $"\"{literal.SearchString}\"";
        }
        else
        {
            _textBlock.Text = _rgxNode?.Name ?? "No Node";
        }
    }
}