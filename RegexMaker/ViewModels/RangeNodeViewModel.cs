using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;
using System;

namespace RegexMaker.ViewModels;

public partial class RangeNodeViewModel : ObservableObject
{
    private readonly RangeNode _node;
    private readonly Action _onChanged;

    [ObservableProperty]
    private string _charStart;

    [ObservableProperty]
    private string _charEnd;

    public RangeNodeViewModel(RangeNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;
        _charStart = node.CharStart;
        _charEnd = node.CharEnd;
    }

    partial void OnCharStartChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _node.CharStart = value;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnCharEndChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _node.CharEnd = value;
            _node.MakeDirty();
            _onChanged();
        }
    }
}