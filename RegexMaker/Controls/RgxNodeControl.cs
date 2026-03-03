using Avalonia.Controls;
using Avalonia.DragCanvas;
using RegexMaker.Nodes;
using Avalonia;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;

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
            Foreground = new SolidColorBrush(Colors.Black),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Thickness(10)
        };

        var border = new Border
        {
            Background = new SolidColorBrush(Colors.AliceBlue),
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
        if (_rgxNode != null)
        {
            _textBlock.Text = _rgxNode.DisplayName;
        }
    }

    public string SerializeApplicationData()
    {
        if (_rgxNode == null)
            return string.Empty;

        var data = new RgxNodeSerializationData
        {
            NodeName = NodeName,
            RgxNodeData = GetRgxNodeDataAsDictionary()
        };

        return JsonSerializer.Serialize(data);
    }

    private Dictionary<string, object?>? GetRgxNodeDataAsDictionary()
    {
        if (_rgxNode == null)
            return null;

        // Use the existing serialization logic
        var jsonString = _rgxNode.SerializeToJson();
        
        // Parse it into a dictionary so it's properly nested in the final JSON
        var jsonDoc = JsonDocument.Parse(jsonString);
        var dict = new Dictionary<string, object?>();
        
        foreach (var property in jsonDoc.RootElement.EnumerateObject())
        {
            dict[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetInt32(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => property.Value.Clone()
            };
        }
        
        return dict;
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
                
                // Restore the RgxNode from the data dictionary
                if (_rgxNode != null && nodeData.RgxNodeData != null)
                {
                    // Convert back to JSON string for the existing DeserializeFromJson method
                    var jsonString = JsonSerializer.Serialize(nodeData.RgxNodeData);
                    _rgxNode.DeserializeFromJson(jsonString);
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
        public Dictionary<string, object?>? RgxNodeData { get; set; }
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