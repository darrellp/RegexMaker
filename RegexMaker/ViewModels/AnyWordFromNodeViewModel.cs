using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class AnyWordFromNodeViewModel : ViewModelBase
{
    private readonly AnyWordFromNode _node;
    private readonly Action _onChanged;

    [ObservableProperty] private string _newWord = string.Empty;

    [ObservableProperty] private string? _selectedWord;

    public AnyWordFromNodeViewModel(AnyWordFromNode node, Action onChanged)
    {
        _node = node;
        _onChanged = onChanged;
        Words = new ObservableCollection<string>(node.Words);
    }

    public ObservableCollection<string> Words { get; }

    [RelayCommand]
    private void AddWord()
    {
        if (string.IsNullOrWhiteSpace(NewWord))
            return;

        Words.Add(NewWord);
        _node.Words.Add(NewWord);
        NewWord = string.Empty;
        _node.MakeDirty();
        _onChanged();
    }

    [RelayCommand]
    private void RemoveWord()
    {
        if (SelectedWord == null)
            return;

        _node.Words.Remove(SelectedWord);
        Words.Remove(SelectedWord);
        SelectedWord = null;
        _node.MakeDirty();
        _onChanged();
    }
}