using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class CharClassNodeViewModel : ViewModelBase
{
    private readonly Action? _onChanged;

    [ObservableProperty] private CharClassType selectedCharClass;

    public CharClassNodeViewModel(CharClassNode node, Action? onChanged)
    {
        Node = node;
        _onChanged = onChanged;
        selectedCharClass = node.CharClass;
    }

    public CharClassNode Node { get; }

    public ObservableCollection<CharClassType> CharClassTypes { get; } =
        new(Enum.GetValues<CharClassType>());

    partial void OnSelectedCharClassChanged(CharClassType value)
    {
        if (Node.CharClass != value)
        {
            Node.CharClass = value;
            Node.MakeDirty();
            _onChanged?.Invoke();
        }
    }
}