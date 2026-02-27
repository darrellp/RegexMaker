using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;
using System;

namespace RegexMaker.ViewModels;

public partial class ConcatenateNodeViewModel : ViewModelBase
{
    private readonly ConcatenateNode _node;
    private readonly Action<int> _onPortCountChanged;

    [ObservableProperty]
    private int _portCount;

    public ConcatenateNodeViewModel(ConcatenateNode node, Action<int> onPortCountChanged)
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