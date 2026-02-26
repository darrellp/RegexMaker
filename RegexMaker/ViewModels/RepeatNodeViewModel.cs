using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;
using System;

namespace RegexMaker.ViewModels;

public partial class RepeatNodeViewModel : ViewModelBase
{
    private readonly RepeatNode _node;
    private readonly Action _onChanged;
    private int _mostBeforeInfinity;

    [ObservableProperty]
    private int _least;

    [ObservableProperty]
    private int _most;

    [ObservableProperty]
    private bool _isInfinity;

    [ObservableProperty]
    private bool _isLazy;

    public int MaximumForLeast => IsInfinity ? int.MaxValue : Most;
    
    public bool IsMostEnabled => !IsInfinity;

    public RepeatNodeViewModel(RepeatNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;
        _least = node.Least;
        _most = node.Most;
        _isLazy = node.IsLazy;
        _isInfinity = node.Most == -1;
        _mostBeforeInfinity = _isInfinity ? 1 : _most;
    }

    partial void OnLeastChanged(int value)
    {
        _node.Least = value;
        _node.MakeDirty();
        _onChanged();
        
        // Only coerce Most if infinity is not checked
        if (!IsInfinity && value > Most)
        {
            Most = value;
        }
    }

    partial void OnMostChanged(int value)
    {
        // Save the value for when infinity is unchecked
        if (!IsInfinity && value > 0)
        {
            _mostBeforeInfinity = value;
        }
        
        _node.Most = value;
        _node.MakeDirty();
        OnPropertyChanged(nameof(MaximumForLeast));
        _onChanged();
    }

    partial void OnIsInfinityChanged(bool value)
    {
        if (value)
        {
            // Save current Most value before setting to infinity
            if (Most > 0)
            {
                _mostBeforeInfinity = Most;
            }
            // Set node to infinity
            _node.Most = -1;
            // Update backing field directly to avoid triggering OnMostChanged
            _most = -1;
            OnPropertyChanged(nameof(Most));
        }
        else
        {
            // Restore previous Most value, ensuring it's at least equal to Least
            int restoredMost = Math.Max(_mostBeforeInfinity, Least);
            Most = restoredMost;
        }
        
        _node.MakeDirty();
        OnPropertyChanged(nameof(MaximumForLeast));
        OnPropertyChanged(nameof(IsMostEnabled));
        _onChanged();
    }

    partial void OnIsLazyChanged(bool value)
    {
        _node.IsLazy = value;
        _node.MakeDirty();
        _onChanged();
    }
}