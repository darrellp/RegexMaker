using Avalonia.Controls;
using Avalonia.DragCanvas;
using RegexMaker.Nodes;
using Avalonia;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace RegexMaker.Controls;

public class RgxNodeControl : DragCanvasNode, ISerializableNode
{
    private RgxNode? _rgxNode;
    private readonly TextBlock _textBlock;
    private IDisposable? _portCtLeftSubscription;

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
        
        // Subscribe to PortCtLeft changes to handle dynamic port updates
        _portCtLeftSubscription = this.GetObservable(PortCtLeftProperty).Subscribe(new PortCtLeftObserver(this));
    }

    private void OnPortCtLeftChanged(int newPortCount)
    {
        // Sync the RgxNode's Parameters collection with the port count
        if (_rgxNode != null)
        {
            int currentCount = _rgxNode.Parameters.Count;
            
            if (newPortCount > currentCount)
            {
                // Add null parameters
                for (int i = currentCount; i < newPortCount; i++)
                {
                    _rgxNode.Parameters.Add(null);
                }
            }
            else if (newPortCount < currentCount)
            {
                // Remove excess parameters
                for (int i = currentCount - 1; i >= newPortCount; i--)
                {
                    _rgxNode.Parameters.RemoveAt(i);
                }
            }
        }

        // Trigger layout update to recalculate port positions
        InvalidateArrange();
        InvalidateVisual();
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

    public void UpdateTextBlock()
    {
        Debug.Assert(_rgxNode != null);
        _textBlock.Text = _rgxNode.DisplayName;
    }

    public string SerializeApplicationData()
    {
        if (_rgxNode == null)
            return string.Empty;

        var data = new RgxNodeSerializationData
        {
            NodeName = NodeName,
            RgxNodeJson = _rgxNode.SerializeToJson()
        };

        return JsonSerializer.Serialize(data);
    }

    public void DeserializeApplicationData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return;

        try
        {
            var nodeData = JsonSerializer.Deserialize<RgxNodeSerializationData>(data);
            if (nodeData != null && !string.IsNullOrEmpty(nodeData.NodeName))
            {
                NodeName = nodeData.NodeName;
                
                // Restore the RgxNode from JSON
                if (_rgxNode != null && !string.IsNullOrEmpty(nodeData.RgxNodeJson))
                {
                    _rgxNode.DeserializeFromJson(nodeData.RgxNodeJson);
                    // Update the visual display after deserialization
                    UpdateTextBlock();
                }
            }
        }
        catch
        {
            // Handle deserialization errors gracefully
        }
    }

    public string GetNodeTypeName()
    {
        return "RgxNodeControl";
    }

    private class RgxNodeSerializationData
    {
        public string? NodeName { get; set; }
        public string? RgxNodeJson { get; set; }
    }

    private class PortCtLeftObserver : IObserver<int>
    {
        private readonly RgxNodeControl _control;

        public PortCtLeftObserver(RgxNodeControl control)
        {
            _control = control;
        }

        public void OnNext(int value)
        {
            _control.OnPortCtLeftChanged(value);
        }

        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}