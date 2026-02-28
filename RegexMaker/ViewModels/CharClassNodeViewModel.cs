using System;
using System.Collections.ObjectModel;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public class CharClassNodeViewModel : ViewModelBase
{
    public CharClassNode Node { get; }
    public ObservableCollection<CharClassType> CharClassTypes { get; } =
        new(Enum.GetValues<CharClassType>());

    public CharClassType SelectedCharClass
    {
        get => Node.CharClass;
        set
        {
            if (Node.CharClass != value)
            {
                Node.CharClass = value;
                OnPropertyChanged(nameof(SelectedCharClass));
                _onChanged?.Invoke();
            }
        }
    }

    private readonly Action? _onChanged;

    public CharClassNodeViewModel(CharClassNode node, Action? onChanged)
    {
        Node = node;
        _onChanged = onChanged;
    }
}