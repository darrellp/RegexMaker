using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class OptionsNodeViewModel : ViewModelBase
{
    private readonly OptionsNode _node;
    private readonly Action _onChanged;

    [ObservableProperty] private bool _caseSensitiveDflt;

    [ObservableProperty] private bool _caseSensitiveOff;

    [ObservableProperty] private bool _caseSensitiveOn;

    [ObservableProperty] private bool _multilineDflt;

    [ObservableProperty] private bool _multilineOff;

    [ObservableProperty] private bool _multilineOn;

    public OptionsNodeViewModel(OptionsNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;

        _caseSensitiveOn = node.CaseSensitiveState == TriState.On;
        _caseSensitiveOff = node.CaseSensitiveState == TriState.Off;
        _caseSensitiveDflt = node.CaseSensitiveState == TriState.Dflt;

        _multilineOn = node.MultilineState == TriState.On;
        _multilineOff = node.MultilineState == TriState.Off;
        _multilineDflt = node.MultilineState == TriState.Dflt;
    }

    partial void OnCaseSensitiveOnChanged(bool value)
    {
        if (value)
        {
            _node.CaseSensitiveState = TriState.On;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnCaseSensitiveOffChanged(bool value)
    {
        if (value)
        {
            _node.CaseSensitiveState = TriState.Off;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnCaseSensitiveDfltChanged(bool value)
    {
        if (value)
        {
            _node.CaseSensitiveState = TriState.Dflt;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnMultilineOnChanged(bool value)
    {
        if (value)
        {
            _node.MultilineState = TriState.On;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnMultilineOffChanged(bool value)
    {
        if (value)
        {
            _node.MultilineState = TriState.Off;
            _node.MakeDirty();
            _onChanged();
        }
    }

    partial void OnMultilineDfltChanged(bool value)
    {
        if (value)
        {
            _node.MultilineState = TriState.Dflt;
            _node.MakeDirty();
            _onChanged();
        }
    }
}