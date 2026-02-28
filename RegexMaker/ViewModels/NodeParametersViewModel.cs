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
    /// Updates the view model for the specified node.
    /// </summary>
    public void UpdateForNode(RgxNode? node, Action onChanged)
    {
        if (node == null)
        {
            CurrentNodeViewModel = null;
            SelectedIndex = 0;
            return;
        }

        SelectedIndex = (int)node.NodeType;

        CurrentNodeViewModel = node.NodeType switch
        {
            RgxNodeType.StringSearch when node is LiteralNode literalNode =>
                new StringSearchNodeViewModel(literalNode, onChanged),
            
            RgxNodeType.Repeat when node is RepeatNode repeatNode =>
                new RepeatNodeViewModel(repeatNode, onChanged),
            
            RgxNodeType.Range when node is RangeNode rangeNode =>
                new RangeNodeViewModel(rangeNode, onChanged),
            
            RgxNodeType.AnyCharFrom when node is AnyCharFromNode anyCharFromNode =>
                new AnyCharFromNodeViewModel(anyCharFromNode, onChanged),
            
            RgxNodeType.Concatenate when node is ConcatenateNode concatenateNode =>
                new ConcatenateNodeViewModel(concatenateNode, newCount => { /* handled in MainView for now */ }),
            
            RgxNodeType.AnyOf when node is AnyOfNode anyOfNode =>
                new AnyOfViewModel(anyOfNode, newCount => { /* handled in MainView for now */ }),
            
            _ => null
        };
    }

    /// <summary>
    /// Clears the current node view model.
    /// </summary>
    public void Clear()
    {
        CurrentNodeViewModel = null;
        SelectedIndex = 0;
    }
}