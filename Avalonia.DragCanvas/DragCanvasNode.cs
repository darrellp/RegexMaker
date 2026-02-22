using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Avalonia.Controls;

public class DragCanvasNode : ContentControl
{
    private const double PortRadiusNormal = 5.0;
    private const double PortRadiusHover = 7.0;
    private const double PortPaddingRatio = 0.1;
    private const double PortSizeRatio = 0.2; // Port + padding = 0.2 + 0.1 = 0.3 per port
    
    private readonly List<PortInfo> _leftPorts = new();
    private readonly List<PortInfo> _rightPorts = new();
    private int? _hoveredPortIndex;
    private PortSide? _hoveredPortSide;
    private Point _lastPointerPosition;
    private Size _lastArrangedSize;

    // Styled properties for port counts
    public static readonly StyledProperty<int> PortCtLeftProperty =
        AvaloniaProperty.Register<DragCanvasNode, int>(
            nameof(PortCtLeft),
            defaultValue: 0,
            validate: value => value >= 0);

    public static readonly StyledProperty<int> PortCtRightProperty =
        AvaloniaProperty.Register<DragCanvasNode, int>(
            nameof(PortCtRight),
            defaultValue: 0,
            validate: value => value >= 0);

    public int PortCtLeft
    {
        get => GetValue(PortCtLeftProperty);
        set => SetValue(PortCtLeftProperty, value);
    }

    public int PortCtRight
    {
        get => GetValue(PortCtRightProperty);
        set => SetValue(PortCtRightProperty, value);
    }

    static DragCanvasNode()
    {
        AffectsRender<DragCanvasNode>(PortCtLeftProperty, PortCtRightProperty);
        AffectsMeasure<DragCanvasNode>(PortCtLeftProperty, PortCtRightProperty);
    }

    public DragCanvasNode()
    {
        // Don't clip - we want to see the ports even if they extend slightly beyond bounds
        ClipToBounds = false;
        
        // Make sure we can receive pointer events
        Background = Background ?? Brushes.Transparent;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PortCtLeftProperty || change.Property == PortCtRightProperty)
        {
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var contentSize = base.MeasureOverride(availableSize);
        
        // Calculate minimum height needed for ports
        var maxPorts = Math.Max(PortCtLeft, PortCtRight);
        var minHeightForPorts = CalculateMinHeightForPorts(maxPorts);
        
        // Ensure we have enough height for the ports
        var finalHeight = Math.Max(contentSize.Height, minHeightForPorts);
        var finalWidth = Math.Max(contentSize.Width, 60); // Minimum width for ports
        
        return new Size(finalWidth, finalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _lastArrangedSize = finalSize;
        return base.ArrangeOverride(finalSize);
    }

    private double CalculateMinHeightForPorts(int portCount)
    {
        if (portCount == 0) return 0;
        
        // Each port needs: padding (0.1) + port space (0.2) of total height
        // First port has top padding, last port has bottom padding
        // Formula: (portCount * 0.3 + 0.1) * some base unit
        // We want each port to be visible, so we use actual pixel values
        var portHeight = PortRadiusNormal * 2;
        var paddingHeight = portHeight * 0.5; // Padding is half the port height
        
        return portCount * (portHeight + paddingHeight) + paddingHeight;
    }

    private void UpdatePorts()
    {
        _leftPorts.Clear();
        _rightPorts.Clear();

        var width = _lastArrangedSize.Width;
        var height = _lastArrangedSize.Height;
        
        if (width <= 0 || height <= 0)
            return;
        
        // Create left ports
        CreatePorts(_leftPorts, PortCtLeft, 0, height, PortSide.Left);
        
        // Create right ports
        CreatePorts(_rightPorts, PortCtRight, width, height, PortSide.Right);
    }

    private void CreatePorts(List<PortInfo> portList, int count, double x, double height, PortSide side)
    {
        if (count == 0) return;

        var portHeight = height * PortSizeRatio;
        var paddingHeight = height * PortPaddingRatio;
        var totalSpacePerPort = portHeight + paddingHeight;
        
        // Calculate starting Y position to center the ports
        var totalHeight = count * totalSpacePerPort + paddingHeight;
        var startY = (height - totalHeight) / 2 + paddingHeight + portHeight / 2;

        for (int i = 0; i < count; i++)
        {
            var y = startY + i * totalSpacePerPort;
            portList.Add(new PortInfo
            {
                Center = new Point(x, y),
                Index = i,
                Side = side
            });
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Update ports before rendering to ensure correct positioning
        UpdatePorts();

        // Render all ports
        RenderPorts(context, _leftPorts);
        RenderPorts(context, _rightPorts);
    }

    private void RenderPorts(DrawingContext context, List<PortInfo> ports)
    {
        foreach (var port in ports)
        {
            // Check if this port is hovered by comparing index and side
            var isHovered = _hoveredPortIndex.HasValue && 
                           _hoveredPortSide.HasValue &&
                           port.Index == _hoveredPortIndex.Value && 
                           port.Side == _hoveredPortSide.Value;
            
            var radius = isHovered ? PortRadiusHover : PortRadiusNormal;
            var brush = isHovered ? Brushes.Blue : Brushes.Black;

            context.DrawEllipse(brush, null, port.Center, radius, radius);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        _lastPointerPosition = e.GetPosition(this);
        UpdateHoveredPort(_lastPointerPosition);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (_hoveredPortIndex.HasValue)
        {
            _hoveredPortIndex = null;
            _hoveredPortSide = null;
            InvalidateVisual();
        }
    }

    private void UpdateHoveredPort(Point pointerPosition)
    {
        const double hoverDistance = 20.0; // Distance within which port is considered hovered
        
        PortInfo? newHoveredPort = null;
        double minDistance = hoverDistance;

        // Ensure ports are up to date
        if (_leftPorts.Count == 0 && _rightPorts.Count == 0)
        {
            UpdatePorts();
        }

        // Check all ports
        foreach (var port in _leftPorts.Concat(_rightPorts))
        {
            var distance = Distance(pointerPosition, port.Center);
            if (distance < minDistance)
            {
                minDistance = distance;
                newHoveredPort = port;
            }
        }

        // Check if hover state changed
        bool hasChanged = false;
        
        if (newHoveredPort == null)
        {
            if (_hoveredPortIndex.HasValue)
            {
                hasChanged = true;
                _hoveredPortIndex = null;
                _hoveredPortSide = null;
            }
        }
        else
        {
            if (!_hoveredPortIndex.HasValue || 
                _hoveredPortIndex.Value != newHoveredPort.Index ||
                _hoveredPortSide != newHoveredPort.Side)
            {
                hasChanged = true;
                _hoveredPortIndex = newHoveredPort.Index;
                _hoveredPortSide = newHoveredPort.Side;
            }
        }

        if (hasChanged)
        {
            InvalidateVisual();
        }
    }

    private static double Distance(Point p1, Point p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private class PortInfo
    {
        public Point Center { get; set; }
        public int Index { get; set; }
        public PortSide Side { get; set; }
    }

    private enum PortSide
    {
        Left,
        Right
    }
}