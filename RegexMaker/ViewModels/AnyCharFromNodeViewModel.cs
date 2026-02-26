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

    public AnyCharFromNodeViewModel(AnyCharFromNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;
        _chars = node.Chars;
    }

    partial void OnCharsChanged(string value)
    {
        _node.Chars = value;
        _node.MakeDirty();
        _onChanged();
    }
}