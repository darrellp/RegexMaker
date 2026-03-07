using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class NamedNodeViewModel : ViewModelBase
{
    private readonly NamedNode _node;
    private readonly Action _onChanged;

    [ObservableProperty] private string _groupName;

    public NamedNodeViewModel(NamedNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;
        _groupName = node.GroupName;
    }

    partial void OnGroupNameChanged(string value)
    {
        _node.GroupName = value;
        _node.MakeDirty();
        _onChanged();
    }
}