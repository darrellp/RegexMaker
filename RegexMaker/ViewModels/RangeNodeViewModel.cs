using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class RangeNodeViewModel : ObservableObject
{
    private readonly RangeNode _node;

    [ObservableProperty]
    private string _charStart;

    [ObservableProperty]
    private string _charEnd;

    public RangeNodeViewModel(RangeNode node)
    {
        _node = node;
        _charStart = node.CharStart;
        _charEnd = node.CharEnd;
    }

    partial void OnCharStartChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _node.CharStart = value;
            _node.MakeDirty();
        }
    }

    partial void OnCharEndChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _node.CharEnd = value;
            _node.MakeDirty();
        }
    }
}