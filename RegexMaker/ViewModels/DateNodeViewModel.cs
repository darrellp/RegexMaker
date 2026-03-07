using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class DateNodeViewModel : ViewModelBase
{
    private readonly DateNode _node;
    private readonly Action _onChanged;

    [ObservableProperty] private bool _isAmerican;

    public DateNodeViewModel(DateNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;
        _isAmerican = node.IsAmerican;
    }

    partial void OnIsAmericanChanged(bool value)
    {
        _node.IsAmerican = value;
        _node.MakeDirty();
        _onChanged();
    }
}