using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class RepeatNodeViewModel : ObservableObject
{
    private readonly RepeatNode _node;

    [ObservableProperty]
    private int _least;

    [ObservableProperty]
    private int _most;

    [ObservableProperty]
    private bool _isLazy;

    public RepeatNodeViewModel(RepeatNode node)
    {
        _node = node;
        _least = node.Least;
        _most = node.Most;
        _isLazy = node.IsLazy;
    }

    partial void OnLeastChanged(int value)
    {
        _node.Least = value;
        _node.MakeDirty();
    }

    partial void OnMostChanged(int value)
    {
        _node.Most = value;
        _node.MakeDirty();
    }

    partial void OnIsLazyChanged(bool value)
    {
        _node.IsLazy = value;
        _node.MakeDirty();
    }
}