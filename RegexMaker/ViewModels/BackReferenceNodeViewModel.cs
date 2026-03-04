using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;
using System;

namespace RegexMaker.ViewModels;

public partial class BackReferenceNodeViewModel : ViewModelBase
{
    private readonly BackReferenceNode _node;
    private readonly Action _onChanged;

    [ObservableProperty]
    private string _groupName;

    public BackReferenceNodeViewModel(BackReferenceNode node, Action onChanged)
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