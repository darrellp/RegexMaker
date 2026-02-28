using CommunityToolkit.Mvvm.ComponentModel;
using RegexMaker.Nodes;
using System;

namespace RegexMaker.ViewModels;

/// <summary>
/// ViewModel for managing the currently selected node's parameters.
/// </summary>
public partial class NodeParametersViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _selectedIndex = 0;

    [ObservableProperty]
    private object? _currentNodeViewModel;

    /// <summary>
    /// Clears the current node view model.
    /// </summary>
    public void Clear()
    {
        CurrentNodeViewModel = null;
        SelectedIndex = 0;
    }
}