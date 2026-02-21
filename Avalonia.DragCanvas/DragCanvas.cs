using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Controls;

public class DragCanvas : Canvas
{
    private Control? elementBeingDragged;
    private Point origCursorLocation;
    private double origHorizOffset, origVertOffset;
    private bool modifyLeftOffset, modifyTopOffset;

    public bool IsDragInProgress { get; private set; }

    // Attached property for CanBeDragged
    public static readonly AttachedProperty<bool> CanBeDraggedProperty =
        AvaloniaProperty.RegisterAttached<DragCanvas, Control, bool>(
            "CanBeDragged", 
            defaultValue: true);

    public static bool GetCanBeDragged(Control element) => 
        element.GetValue(CanBeDraggedProperty);

    public static void SetCanBeDragged(Control element, bool value) => 
        element.SetValue(CanBeDraggedProperty, value);

    // StyledProperty for AllowDragging
    public static readonly StyledProperty<bool> AllowDraggingProperty =
        AvaloniaProperty.Register<DragCanvas, bool>(
            nameof(AllowDragging), 
            defaultValue: true);

    public bool AllowDragging
    {
        get => GetValue(AllowDraggingProperty);
        set => SetValue(AllowDraggingProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (!AllowDragging) return;

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed) return;

        origCursorLocation = point.Position;
        
        // Find canvas child
        if (e.Source is Visual visual)
        {
            elementBeingDragged = FindCanvasChild(visual);
            if (elementBeingDragged == null) return;

            double left = GetLeft(elementBeingDragged);
            double right = GetRight(elementBeingDragged);
            double top = GetTop(elementBeingDragged);
            double bottom = GetBottom(elementBeingDragged);

            origHorizOffset = ResolveOffset(left, right, out modifyLeftOffset);
            origVertOffset = ResolveOffset(top, bottom, out modifyTopOffset);

            IsDragInProgress = true;
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        if (elementBeingDragged == null || !IsDragInProgress) return;

        Point cursorLocation = e.GetPosition(this);

        double newHorizontalOffset = modifyLeftOffset
            ? origHorizOffset + (cursorLocation.X - origCursorLocation.X)
            : origHorizOffset - (cursorLocation.X - origCursorLocation.X);

        double newVerticalOffset = modifyTopOffset
            ? origVertOffset + (cursorLocation.Y - origCursorLocation.Y)
            : origVertOffset - (cursorLocation.Y - origCursorLocation.Y);

        if (modifyLeftOffset)
            SetLeft(elementBeingDragged, newHorizontalOffset);
        else
            SetRight(elementBeingDragged, newHorizontalOffset);

        if (modifyTopOffset)
            SetTop(elementBeingDragged, newVerticalOffset);
        else
            SetBottom(elementBeingDragged, newVerticalOffset);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        elementBeingDragged = null;
        IsDragInProgress = false;
    }

    private Control? FindCanvasChild(Visual? visual)
    {
        while (visual != null)
        {
            if (visual is Control control && Children.Contains(control))
                return control;
            
            visual = visual.GetVisualParent();
        }
        return null;
    }

    private static double ResolveOffset(double side1, double side2, out bool useSide1)
    {
        useSide1 = true;
        if (double.IsNaN(side1))
        {
            if (double.IsNaN(side2))
                return 0;
            else
            {
                useSide1 = false;
                return side2;
            }
        }
        return side1;
    }
}