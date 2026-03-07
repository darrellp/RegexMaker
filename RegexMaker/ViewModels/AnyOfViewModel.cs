using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class AnyOfViewModel : ViewModelBase
{
    private readonly AnyOfNode _node;
    private readonly Action<int> _onPortCountChanged;

    [ObservableProperty] private int _portCount;

    public AnyOfViewModel(AnyOfNode node, Action<int> onPortCountChanged)
    {
        _node = node;
        _onPortCountChanged = onPortCountChanged;
        _portCount = Math.Max(2, node.Parameters.Count);
    }

    partial void OnPortCountChanged(int value)
    {
        // Coerce to minimum of 2
        if (value < 2)
        {
            PortCount = 2;
            return;
        }

        _onPortCountChanged(value);
    }
}