using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RegexMaker.Nodes;
using System;
using System.Collections.ObjectModel;

namespace RegexMaker.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public event EventHandler<SaveRequestedEventArgs>? SaveRequested;
    public event EventHandler<LoadRequestedEventArgs>? LoadRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler<CopyRegexRequestedEventArgs>? CopyRegexRequested;

    /// <summary>
    /// Raised when the node display needs to be refreshed (e.g., after a property change).
    /// The View subscribes to this to update visual elements like RgxNodeControl.
    /// </summary>
    public event Action? NodeDisplayUpdateRequested;

    [ObservableProperty]
    private string _regexPattern = string.Empty;

    [ObservableProperty]
    private object? _currentNodeViewModel;

    [ObservableProperty]
    private int _selectedCarouselIndex = 0;

    [ObservableProperty]
    private string? _matchExtent;

    [ObservableProperty]
    private ObservableCollection<string> _matches = new();

    private RgxNode? _currentlySelectedNode;
    private object? _currentViewModelBacking;

    /// <summary>
    /// Switches the active node selection — creates the appropriate ViewModel
    /// and updates the carousel index. Pure logic, no UI dependencies.
    /// </summary>
    /// <param name="node">The newly selected node, or null to clear selection.</param>
    /// <param name="portCountChangedCallback">
    /// Callback for port-count changes that require canvas manipulation (UI-side concern).
    /// </param>
    public void SelectNode(RgxNode? node, Func<RgxNode, int, Action>? portCountChangedCallback = null)
    {
        if (node == null)
        {
            _currentlySelectedNode = null;
            UnsubscribeFromCurrentViewModel();
            _currentViewModelBacking = null;
            CurrentNodeViewModel = null;
            SelectedCarouselIndex = 0;
            return;
        }

        _currentlySelectedNode = node;
        UnsubscribeFromCurrentViewModel();

        SelectedCarouselIndex = (int)node.NodeType;

        _currentViewModelBacking = node.NodeType switch
        {
            RgxNodeType.StringSearch when node is LiteralNode lit
                => new StringSearchNodeViewModel(lit, RequestNodeDisplayUpdate),
            RgxNodeType.Repeat when node is RepeatNode rep
                => new RepeatNodeViewModel(rep, RequestNodeDisplayUpdate),
            RgxNodeType.Range when node is RangeNode rng
                => new RangeNodeViewModel(rng, RequestNodeDisplayUpdate),
            RgxNodeType.AnyCharFrom when node is AnyCharFromNode acf
                => new AnyCharFromNodeViewModel(acf, RequestNodeDisplayUpdate),
            RgxNodeType.Concatenate when node is ConcatenateNode cat
                => new ConcatenateNodeViewModel(cat, newCount => portCountChangedCallback?.Invoke(cat, newCount)),
            RgxNodeType.AnyOf when node is AnyOfNode ao
                => new AnyOfViewModel(ao, newCount => portCountChangedCallback?.Invoke(ao, newCount)),
            RgxNodeType.CharClass when node is CharClassNode cc
                => new CharClassNodeViewModel(cc, RequestNodeDisplayUpdate),
            RgxNodeType.Named when node is NamedNode named
                => new NamedNodeViewModel(named, RequestNodeDisplayUpdate),
            RgxNodeType.AnyWordFrom when node is AnyWordFromNode awf
                => new AnyWordFromNodeViewModel(awf, RequestNodeDisplayUpdate),
            RgxNodeType.BackReference when node is BackReferenceNode br
                => new BackReferenceNodeViewModel(br, RequestNodeDisplayUpdate),
            RgxNodeType.Numeric when node is NumericNode num
                => new NumericNodeViewModel(num, RequestNodeDisplayUpdate),
            RgxNodeType.Options when node is OptionsNode opt
                => new OptionsNodeViewModel(opt, RequestNodeDisplayUpdate),
            _ => null
        };

        CurrentNodeViewModel = _currentViewModelBacking;

        if (_currentViewModelBacking is ObservableObject obs)
        {
            obs.PropertyChanged += (_, _) => RequestNodeDisplayUpdate();
        }
    }

    public void RequestNodeDisplayUpdate()
    {
        if (_currentlySelectedNode != null)
        {
            RegexPattern = _currentlySelectedNode.ProduceResult();
        }
        NodeDisplayUpdateRequested?.Invoke();
    }

    private void UnsubscribeFromCurrentViewModel()
    {
        // ObservableObject subscriptions are replaced each time SelectNode is called
        _currentViewModelBacking = null;
    }

    [RelayCommand]
    private void Save() => SaveRequested?.Invoke(this, new SaveRequestedEventArgs());

    [RelayCommand]
    private void Load() => LoadRequested?.Invoke(this, new LoadRequestedEventArgs());

    [RelayCommand]
    private void Clear() => ClearRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void CopyRegex()
    {
        if (string.IsNullOrEmpty(RegexPattern))
            return;
        CopyRegexRequested?.Invoke(this, new CopyRegexRequestedEventArgs(RegexPattern));
    }
}

public class SaveRequestedEventArgs : EventArgs
{
}

public class LoadRequestedEventArgs : EventArgs
{
}

public class CopyRegexRequestedEventArgs : EventArgs
{
    public string RegexPattern { get; }

    public CopyRegexRequestedEventArgs(string regexPattern)
    {
        RegexPattern = regexPattern;
    }
}



