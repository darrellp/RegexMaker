using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;
using System;

namespace RegexMaker.ViewModels;

public partial class AnyCharFromNodeViewModel : ViewModelBase
{
    private readonly AnyCharFromNode _node;
    private readonly Action _onChanged;

    [ObservableProperty]
    private string _chars;

    [ObservableProperty]
    private bool _notIn; // Add this property

    public AnyCharFromNodeViewModel(AnyCharFromNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;
        _chars = node.Chars;
        _notIn = node.NotIn; // Initialize from node
    }

    partial void OnCharsChanged(string value)
    {
        _node.Chars = value;
        _node.MakeDirty();
        _onChanged();
    }

    partial void OnNotInChanged(bool value)
    {
        _node.NotIn = value;
        _node.MakeDirty();
        _onChanged();
    }
}