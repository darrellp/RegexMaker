using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RegexMaker.Nodes;

namespace RegexMaker.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private RgxNode? _currentlySelectedNode;

    [ObservableProperty] private object? _currentNodeViewModel;

    private object? _currentViewModelBacking;

    [ObservableProperty] private ObservableCollection<string> _matches = new();

    [ObservableProperty] private string? _matchExtent;

    [ObservableProperty] private string _regexPattern = string.Empty;

    [ObservableProperty] private string _replacePattern = string.Empty;

    [ObservableProperty] private string _replacementText = string.Empty;

    [ObservableProperty] private int _selectedCarouselIndex;

    [ObservableProperty] private string? _selectedVariableName;

    [ObservableProperty] private bool _showReplace;

    [ObservableProperty] private bool _showWhitespace;

    [ObservableProperty] private bool _useCrLf;

    public event EventHandler<SaveRequestedEventArgs>? SaveRequested;
    public event EventHandler<LoadRequestedEventArgs>? LoadRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler<CopyRegexRequestedEventArgs>? CopyRegexRequested;
    public event EventHandler<ShowCodeRequestedEventArgs>? ShowCodeRequested;

    /// <summary>
    ///     Raised when the node display needs to be refreshed (e.g., after a property change).
    ///     The View subscribes to this to update visual elements like RgxNodeControl.
    /// </summary>
    public event Action? NodeDisplayUpdateRequested;

    /// <summary>
    ///     Raised when the variable name is changed by the user.
    ///     The View subscribes to sync this back to the selected RgxNodeControl.
    /// </summary>
    public event Action<string?>? VariableNameChanged;

    /// <summary>
    ///     Raised when the line ending mode is toggled. The View subscribes to convert
    ///     the editor text between \r\n and \n.
    /// </summary>
    public event Action<bool>? LineEndingToggled;

    /// <summary>
    ///     Raised when the show whitespace toggle changes. The View subscribes to update
    ///     the AvaloniaEdit editor options.
    /// </summary>
    public event Action<bool>? ShowWhitespaceToggled;

    /// <summary>
    ///     Raised when the show replace toggle changes. The View subscribes to show/hide
    ///     the replacement panel.
    /// </summary>
    public event Action<bool>? ShowReplaceToggled;

    /// <summary>
    ///     Raised when the replace pattern changes so the view can update replacement text.
    /// </summary>
    public event Action? ReplacePatternChanged;

    partial void OnUseCrLfChanged(bool oldValue, bool newValue)
    {
        LineEndingToggled?.Invoke(newValue);
    }

    partial void OnShowWhitespaceChanged(bool oldValue, bool newValue)
    {
        ShowWhitespaceToggled?.Invoke(newValue);
    }

    partial void OnShowReplaceChanged(bool oldValue, bool newValue)
    {
        ShowReplaceToggled?.Invoke(newValue);
    }

    partial void OnReplacePatternChanged(string? oldValue, string newValue)
    {
        ReplacePatternChanged?.Invoke();
    }

    partial void OnSelectedVariableNameChanged(string? oldValue, string? newValue)
    {
        VariableNameChanged?.Invoke(newValue);
    }

    /// <summary>
    ///     Performs the regex replacement and updates <see cref="ReplacementText"/>.
    /// </summary>
    /// <param name="sampleText">The current sample text from the editor.</param>
    public void UpdateReplacementText(string sampleText)
    {
        if (!ShowReplace || string.IsNullOrEmpty(RegexPattern))
        {
            ReplacementText = string.Empty;
            return;
        }

        try
        {
            var regex = new Regex(RegexPattern);
            ReplacementText = regex.Replace(sampleText, ReplacePattern ?? string.Empty);
        }
        catch
        {
            ReplacementText = "<!-- invalid regex or replace pattern -->";
        }
    }

    /// <summary>
    ///     Switches the active node selection — creates the appropriate ViewModel
    ///     and updates the carousel index. Pure logic, no UI dependencies.
    /// </summary>
    /// <param name="node">The newly selected node, or null to clear selection.</param>
    /// <param name="portCountChangedCallback">
    ///     Callback for port-count changes that require canvas manipulation (UI-side concern).
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
            SelectedVariableName = null;
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
            RgxNodeType.Anchor when node is AnchorNode anc
                => new AnchorNodeViewModel(anc, RequestNodeDisplayUpdate),
            _ => null
        };

        CurrentNodeViewModel = _currentViewModelBacking;

        if (_currentViewModelBacking is ObservableObject obs)
            obs.PropertyChanged += (_, _) => RequestNodeDisplayUpdate();
    }

    public void RequestNodeDisplayUpdate()
    {
        if (_currentlySelectedNode != null) RegexPattern = _currentlySelectedNode.ProduceResult();
        NodeDisplayUpdateRequested?.Invoke();
    }

    private void UnsubscribeFromCurrentViewModel()
    {
        // ObservableObject subscriptions are replaced each time SelectNode is called
        _currentViewModelBacking = null;
    }

    [RelayCommand]
    private void Save()
    {
        SaveRequested?.Invoke(this, new SaveRequestedEventArgs());
    }

    [RelayCommand]
    private void Load()
    {
        LoadRequested?.Invoke(this, new LoadRequestedEventArgs());
    }

    [RelayCommand]
    private void Clear()
    {
        ClearRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CopyRegex()
    {
        if (string.IsNullOrEmpty(RegexPattern))
            return;
        CopyRegexRequested?.Invoke(this, new CopyRegexRequestedEventArgs(RegexPattern));
    }

    [RelayCommand]
    private void ShowCode()
    {
        if (_currentlySelectedNode == null)
            return;

        var cc = new CodeCollector(_currentlySelectedNode);
        cc.GatherCode();
        ShowCodeRequested?.Invoke(this, new ShowCodeRequestedEventArgs(cc.Result));
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
    public CopyRegexRequestedEventArgs(string regexPattern)
    {
        RegexPattern = regexPattern;
    }

    public string RegexPattern { get; }
}

public class ShowCodeRequestedEventArgs : EventArgs
{
    public ShowCodeRequestedEventArgs(string code)
    {
        Code = code;
    }

    public string Code { get; }
}