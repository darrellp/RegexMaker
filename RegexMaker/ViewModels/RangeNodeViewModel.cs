using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class RangeNodeViewModel : ViewModelBase
{
    private readonly RangeNode _node;
    private readonly Action _onChanged;

    [ObservableProperty] private string _charEnd;

    [ObservableProperty] private string _charStart;

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