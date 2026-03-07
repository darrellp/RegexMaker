using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class StringSearchNodeViewModel : ViewModelBase
{
    private readonly LiteralNode _node;
    private readonly Action _onChanged;

    [ObservableProperty] private bool _autoEscape;

    [ObservableProperty] private string _searchString;

    public StringSearchNodeViewModel(LiteralNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;
        _searchString = node.SearchString;
        _autoEscape = node.AutoEscape;
    }

    partial void OnAutoEscapeChanged(bool value)
    {
        _node.AutoEscape = value;
        _node.MakeDirty();
        _onChanged();
    }


    partial void OnSearchStringChanged(string value)
    {
        _node.SearchString = value;
        _node.MakeDirty();
        _onChanged();
    }
}