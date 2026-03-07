using CommunityToolkit.Mvvm.ComponentModel;

namespace RegexMaker.ViewModels;

/// <summary>
///     ViewModel for managing the currently selected node's parameters.
/// </summary>
public partial class NodeParametersViewModel : ViewModelBase
{
    [ObservableProperty] private object? _currentNodeViewModel;

    [ObservableProperty] private int _selectedIndex;

    /// <summary>
    ///     Clears the current node view model.
    /// </summary>
    public void Clear()
    {
        CurrentNodeViewModel = null;
        SelectedIndex = 0;
    }
}