using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;
using System;

namespace RegexMaker.ViewModels;

public partial class StringSearchNodeViewModel : ViewModelBase
{
    private readonly LiteralNode _node;
    private readonly Action _onChanged;

    [ObservableProperty]
    private string _searchString;

    public StringSearchNodeViewModel(LiteralNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;
        _searchString = node.SearchString;
    }

    partial void OnSearchStringChanged(string value)
    {
        _node.SearchString = value;
        _node.MakeDirty();
        _onChanged();
    }
}