using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class AnchorNodeViewModel : ViewModelBase
{
    private readonly AnchorNode _node;
    private readonly Action _onChanged;

    [ObservableProperty] private bool _isPositive;
    [ObservableProperty] private bool _isNegative;
    [ObservableProperty] private bool _isAhead;
    [ObservableProperty] private bool _isBehind;

    public AnchorNodeViewModel(AnchorNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;

        _isPositive = node.IsPositive;
        _isNegative = !node.IsPositive;
        _isAhead = node.IsAhead;
        _isBehind = !node.IsAhead;
    }

    partial void OnIsPositiveChanged(bool value)
    {
        if (value)
        {
            _node.IsPositive = true;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnIsNegativeChanged(bool value)
    {
        if (value)
        {
            _node.IsPositive = false;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnIsAheadChanged(bool value)
    {
        if (value)
        {
            _node.IsAhead = true;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnIsBehindChanged(bool value)
    {
        if (value)
        {
            _node.IsAhead = false;
            _node.MakeDirty();
            _onChanged();
        }
    }
}