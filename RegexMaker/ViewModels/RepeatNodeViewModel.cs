using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;
using System;

namespace RegexMaker.ViewModels;

public partial class RepeatNodeViewModel : ObservableObject
{
    private readonly RepeatNode _node;
    private readonly Action _onChanged;

    [ObservableProperty]
    private int _least;

    [ObservableProperty]
    private int _most;

    [ObservableProperty]
    private bool _isLazy;

    public int MaximumForLeast => Most == -1 ? int.MaxValue : Most;

    public RepeatNodeViewModel(RepeatNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;
        _least = node.Least;
        _most = node.Most;
        _isLazy = node.IsLazy;
    }

    partial void OnLeastChanged(int value)
    {
        _node.Least = value;
        _node.MakeDirty();
        _onChanged();
    }

    partial void OnMostChanged(int value)
    {
        _node.Most = value;
        _node.MakeDirty();
        OnPropertyChanged(nameof(MaximumForLeast));
        _onChanged();
    }

    partial void OnIsLazyChanged(bool value)
    {
        _node.IsLazy = value;
        _node.MakeDirty();
        _onChanged();
    }
}