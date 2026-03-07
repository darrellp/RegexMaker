using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class NumericNodeViewModel : ViewModelBase
{
    private readonly NumericNode _node;
    private readonly Action _onChanged;

    [ObservableProperty] private bool _isFloat;

    [ObservableProperty] private bool _isInteger;

    [ObservableProperty] private bool _isUnsigned;

    public NumericNodeViewModel(NumericNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;

        _isInteger = node.NumericType == NumericType.Integer;
        _isUnsigned = node.NumericType == NumericType.Unsigned;
        _isFloat = node.NumericType == NumericType.Float;
    }

    partial void OnIsIntegerChanged(bool value)
    {
        if (value)
        {
            _node.NumericType = NumericType.Integer;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnIsUnsignedChanged(bool value)
    {
        if (value)
        {
            _node.NumericType = NumericType.Unsigned;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnIsFloatChanged(bool value)
    {
        if (value)
        {
            _node.NumericType = NumericType.Float;
            _node.MakeDirty();
            _onChanged();
        }
    }
}