using RegexMaker.Nodes;
using System;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RegexMaker.ViewModels;

public partial class CharClassNodeViewModel : ViewModelBase
{
    public CharClassNode Node { get; }
    public ObservableCollection<CharClassType> CharClassTypes { get; } =
        new(Enum.GetValues<CharClassType>());

    [ObservableProperty]
    private CharClassType selectedCharClass;

    private readonly Action? _onChanged;

    public CharClassNodeViewModel(CharClassNode node, Action? onChanged)
    {
        Node = node;
        _onChanged = onChanged;
        selectedCharClass = node.CharClass;
    }

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