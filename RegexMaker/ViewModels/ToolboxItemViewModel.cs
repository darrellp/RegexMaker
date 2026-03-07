using CommunityToolkit.Mvvm.ComponentModel;

namespace RegexMaker.ViewModels;

/// <summary>
///     ViewModel for a toolbox item representing a draggable node type.
/// </summary>
public partial class ToolboxItemViewModel : ViewModelBase
{
    [ObservableProperty] private string _displayText = string.Empty;

    [ObservableProperty] private string _nodeName = string.Empty;

    public ToolboxItemViewModel(string nodeName, string displayText)
    {
        _nodeName = nodeName;
        _displayText = displayText;
    }
}