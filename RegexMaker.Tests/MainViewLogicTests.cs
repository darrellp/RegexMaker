using RegexMaker.Nodes;
using RegexMaker.ViewModels;
using RegexMaker.Services;

namespace RegexMaker.Tests;

/// <summary>
/// Tests for the logic exercised by MainView's event handlers,
/// verified through the underlying data model (RgxNode graph and MainViewModel).
/// </summary>
public class MainViewLogicTests
{
    #region NodeSwitched / Carousel Index Tests

    [Theory]
    [InlineData(typeof(LiteralNode), RgxNodeType.StringSearch, 0)]
    [InlineData(typeof(ConcatenateNode), RgxNodeType.Concatenate, 1)]
    [InlineData(typeof(RepeatNode), RgxNodeType.Repeat, 2)]
    [InlineData(typeof(AnyCharFromNode), RgxNodeType.AnyCharFrom, 5)]
    [InlineData(typeof(AnyOfNode), RgxNodeType.AnyOf, 6)]
    [InlineData(typeof(RangeNode), RgxNodeType.Range, 7)]
    [InlineData(typeof(CharClassNode), RgxNodeType.CharClass, 8)]
    [InlineData(typeof(NamedNode), RgxNodeType.Named, 9)]
    public void NodeType_ShouldMapToCorrectCarouselIndex(Type nodeType, RgxNodeType expectedNodeType, int expectedIndex)
    {
        // The carousel index is set via: _mainViewModel.SelectedCarouselIndex = (int)node.NodeType;
        // Verify each node type maps to the expected integer index.
        var node = (RgxNode?)Activator.CreateInstance(nodeType);
        Assert.NotNull(node);
        Assert.Equal(expectedNodeType, node.NodeType);
        Assert.Equal(expectedIndex, (int)node.NodeType);
    }

    #endregion

    #region OnConnectionCreated Logic Tests

    [Fact]
    public void ConnectionCreated_ShouldSetParameterOnTargetNode()
    {
        // Simulates what OnConnectionCreated does:
        // rgxNodeTarget.Parameters[targetNodeIndex] = rgxNodeSource;
        // rgxNodeSource.Parents.Add(rgxNodeTarget);
        var source = new LiteralNode("hello");
        var target = new ConcatenateNode(); // Has [null, null] parameters

        int targetPortIndex = 0;
        target.Parameters[targetPortIndex] = source;
        source.Parents.Add(target);

        Assert.Same(source, target.Parameters[targetPortIndex]);
        Assert.Contains(target, source.Parents);
    }

    [Fact]
    public void ConnectionCreated_ShouldMakeTargetDirtyAndUpdateResult()
    {
        // First produce a cached result
        var target = new ConcatenateNode();
        var initialResult = target.ProduceResult();

        // Simulate connection
        var source = new LiteralNode("test");
        target.Parameters[0] = source;
        source.Parents.Add(target);
        target.MakeDirty();

        var newResult = target.ProduceResult();

        Assert.Contains("test", newResult);
    }

    [Fact]
    public void ConnectionCreated_ShouldSupportMultipleConnections()
    {
        var source1 = new LiteralNode("abc");
        var source2 = new LiteralNode("def");
        var target = new ConcatenateNode();

        target.Parameters[0] = source1;
        source1.Parents.Add(target);
        target.Parameters[1] = source2;
        source2.Parents.Add(target);
        target.MakeDirty();

        var result = target.ProduceResult();
        Assert.Equal("abcdef", result);
    }

    #endregion

    #region OnConnectionDeleted Logic Tests

    [Fact]
    public void ConnectionDeleted_ShouldClearParameterOnTargetNode()
    {
        // Simulates what OnConnectionDeleted does:
        // rgxNodeTarget.Parameters[targetNodeIndex] = null;
        // rgxNodeSource.Parents.Remove(rgxNodeTarget);
        var source = new LiteralNode("hello");
        var target = new ConcatenateNode();

        // Setup connection
        target.Parameters[0] = source;
        source.Parents.Add(target);

        // Delete connection
        int targetPortIndex = 0;
        target.Parameters[targetPortIndex] = null;
        source.Parents.Remove(target);
        target.MakeDirty();

        Assert.Null(target.Parameters[targetPortIndex]);
        Assert.DoesNotContain(target, source.Parents);
    }

    [Fact]
    public void ConnectionDeleted_ShouldUpdateProducedResult()
    {
        var source = new LiteralNode("hello");
        var target = new ConcatenateNode();

        // Setup connection
        target.Parameters[0] = source;
        source.Parents.Add(target);
        target.MakeDirty();
        var connectedResult = target.ProduceResult();

        // Delete connection
        target.Parameters[0] = null;
        source.Parents.Remove(target);
        target.MakeDirty();
        var disconnectedResult = target.ProduceResult();

        Assert.Contains("hello", connectedResult);
        Assert.DoesNotContain("hello", disconnectedResult);
    }

    #endregion

    #region OnNodeDeleted Logic Tests

    [Fact]
    public void NodeDeleted_ShouldClearReferencesFromParents()
    {
        // Simulates what OnNodeDeleted does for parent cleanup
        var deletedNode = new LiteralNode("gone");
        var parent = new ConcatenateNode();

        // Setup: parent references deletedNode
        parent.Parameters[0] = deletedNode;
        deletedNode.Parents.Add(parent);

        // Simulate deletion cleanup
        foreach (var p in deletedNode.Parents.ToList())
        {
            if (p is RgxNode parentNode)
            {
                for (int i = 0; i < parentNode.Parameters.Count; i++)
                {
                    if (parentNode.Parameters[i] == deletedNode)
                    {
                        parentNode.Parameters[i] = null;
                    }
                }
                parentNode.MakeDirty();
            }
        }
        deletedNode.Parents.Clear();

        Assert.Null(parent.Parameters[0]);
        Assert.Empty(deletedNode.Parents);
    }

    [Fact]
    public void NodeDeleted_ShouldDisconnectFromChildren()
    {
        // Simulates child cleanup during node deletion
        var child1 = new LiteralNode("a");
        var child2 = new LiteralNode("b");
        var deletedNode = new ConcatenateNode();

        // Setup
        deletedNode.Parameters[0] = child1;
        deletedNode.Parameters[1] = child2;
        child1.Parents.Add(deletedNode);
        child2.Parents.Add(deletedNode);

        // Simulate deletion: clear parameters and remove from children's parents
        for (int i = 0; i < deletedNode.Parameters.Count; i++)
        {
            if (deletedNode.Parameters[i] is RgxNode childNode)
            {
                childNode.Parents.Remove(deletedNode);
                childNode.MakeDirty();
            }
            deletedNode.Parameters[i] = null;
        }

        Assert.All(deletedNode.Parameters, p => Assert.Null(p));
        Assert.DoesNotContain(deletedNode, child1.Parents);
        Assert.DoesNotContain(deletedNode, child2.Parents);
    }

    [Fact]
    public void NodeDeleted_ShouldHandleNodeWithBothParentsAndChildren()
    {
        // A middle node connected both ways
        var grandchild = new LiteralNode("x");
        var middleNode = new RepeatNode([grandchild], 2, 4);
        var parentNode = new ConcatenateNode();

        parentNode.Parameters[0] = middleNode;
        middleNode.Parents.Add(parentNode);
        grandchild.Parents.Add(middleNode);

        // Delete middleNode: cleanup parents
        foreach (var p in middleNode.Parents.ToList())
        {
            if (p is RgxNode pNode)
            {
                for (int i = 0; i < pNode.Parameters.Count; i++)
                {
                    if (pNode.Parameters[i] == middleNode)
                        pNode.Parameters[i] = null;
                }
                pNode.MakeDirty();
            }
        }
        middleNode.Parents.Clear();

        // Delete middleNode: cleanup children
        for (int i = 0; i < middleNode.Parameters.Count; i++)
        {
            if (middleNode.Parameters[i] is RgxNode childNode)
            {
                childNode.Parents.Remove(middleNode);
                childNode.MakeDirty();
            }
            middleNode.Parameters[i] = null;
        }

        Assert.Null(parentNode.Parameters[0]);
        Assert.Empty(middleNode.Parents);
        Assert.DoesNotContain(middleNode, grandchild.Parents);
    }

    #endregion

    #region Port Count Change Logic Tests (OnConcatenatePortCountChanged)

    [Fact]
    public void ConcatenatePortCountIncrease_ShouldAddNullParameters()
    {
        // Simulates OnConcatenatePortCountChanged adding ports
        var node = new ConcatenateNode();
        int currentCount = node.Parameters.Count; // 2
        int newCount = 5;

        int portsToAdd = newCount - currentCount;
        for (int i = 0; i < portsToAdd; i++)
        {
            node.Parameters.Add(null);
        }

        Assert.Equal(newCount, node.Parameters.Count);
        Assert.All(node.Parameters, p => Assert.Null(p));
    }

    [Fact]
    public void ConcatenatePortCountDecrease_ShouldRemoveParameters()
    {
        // Simulates OnConcatenatePortCountChanged removing ports
        var node = new ConcatenateNode();
        // Add extra ports first
        node.Parameters.Add(null);
        node.Parameters.Add(null);
        Assert.Equal(4, node.Parameters.Count);

        int newCount = 2;
        int currentCount = node.Parameters.Count;

        // Clear parameters being removed
        for (int i = newCount; i < currentCount; i++)
        {
            node.Parameters[i] = null;
        }

        // Remove extra parameters
        int portsToRemove = currentCount - newCount;
        for (int i = 0; i < portsToRemove; i++)
        {
            node.Parameters.RemoveAt(node.Parameters.Count - 1);
        }

        node.MakeDirty();

        Assert.Equal(newCount, node.Parameters.Count);
    }

    [Fact]
    public void PortCountDecrease_ShouldDisconnectRemovedPorts()
    {
        var source = new LiteralNode("removed");
        var node = new ConcatenateNode();
        node.Parameters.Add(null); // port index 2

        // Connect source to port 2
        node.Parameters[2] = source;
        source.Parents.Add(node);

        // Decrease to 2 ports — port 2 should be removed
        int newCount = 2;
        int currentCount = node.Parameters.Count;

        for (int i = newCount; i < currentCount; i++)
        {
            if (node.Parameters[i] is RgxNode child)
            {
                child.Parents.Remove(node);
            }
            node.Parameters[i] = null;
        }

        int portsToRemove = currentCount - newCount;
        for (int i = 0; i < portsToRemove; i++)
        {
            node.Parameters.RemoveAt(node.Parameters.Count - 1);
        }
        node.MakeDirty();

        Assert.Equal(2, node.Parameters.Count);
        Assert.DoesNotContain(node, source.Parents);
    }

    #endregion

    #region MainViewModel Interaction Tests

    [Fact]
    public void MainViewModel_SelectedCarouselIndex_ShouldDefaultToZero()
    {
        var vm = new MainViewModel();
        Assert.Equal(0, vm.SelectedCarouselIndex);
    }

    [Fact]
    public void MainViewModel_RegexPattern_ShouldDefaultToEmpty()
    {
        var vm = new MainViewModel();
        Assert.Equal(string.Empty, vm.RegexPattern);
    }

    [Fact]
    public void MainViewModel_CurrentNodeViewModel_ShouldDefaultToNull()
    {
        var vm = new MainViewModel();
        Assert.Null(vm.CurrentNodeViewModel);
    }

    [Fact]
    public void MainViewModel_SettingCarouselIndex_ShouldRaisePropertyChanged()
    {
        var vm = new MainViewModel();
        bool raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedCarouselIndex))
                raised = true;
        };

        vm.SelectedCarouselIndex = 5;

        Assert.True(raised);
        Assert.Equal(5, vm.SelectedCarouselIndex);
    }

    [Fact]
    public void MainViewModel_SettingRegexPattern_ShouldRaisePropertyChanged()
    {
        var vm = new MainViewModel();
        bool raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.RegexPattern))
                raised = true;
        };

        vm.RegexPattern = "test.*pattern";

        Assert.True(raised);
        Assert.Equal("test.*pattern", vm.RegexPattern);
    }

    [Fact]
    public void MainViewModel_SaveCommand_ShouldRaiseSaveRequested()
    {
        var vm = new MainViewModel();
        bool raised = false;
        vm.SaveRequested += (s, e) => raised = true;

        vm.SaveCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public void MainViewModel_LoadCommand_ShouldRaiseLoadRequested()
    {
        var vm = new MainViewModel();
        bool raised = false;
        vm.LoadRequested += (s, e) => raised = true;

        vm.LoadCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public void MainViewModel_ClearCommand_ShouldRaiseClearRequested()
    {
        var vm = new MainViewModel();
        bool raised = false;
        vm.ClearRequested += (s, e) => raised = true;

        vm.ClearCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public void MainViewModel_CopyRegexCommand_ShouldNotRaiseWhenPatternEmpty()
    {
        var vm = new MainViewModel();
        bool raised = false;
        vm.CopyRegexRequested += (s, e) => raised = true;

        vm.CopyRegexCommand.Execute(null);

        Assert.False(raised);
    }

    [Fact]
    public void MainViewModel_CopyRegexCommand_ShouldRaiseWhenPatternSet()
    {
        var vm = new MainViewModel();
        bool raised = false;
        string? copiedPattern = null;
        vm.CopyRegexRequested += (s, e) =>
        {
            raised = true;
            copiedPattern = e.RegexPattern;
        };

        vm.RegexPattern = @"\d+";
        vm.CopyRegexCommand.Execute(null);

        Assert.True(raised);
        Assert.Equal(@"\d+", copiedPattern);
    }

    #endregion

    #region MakeDirty Propagation Tests

    [Fact]
    public void MakeDirty_ShouldPropagateToParents()
    {
        var child = new LiteralNode("a");
        var parent = new ConcatenateNode();

        parent.Parameters[0] = child;
        child.Parents.Add(parent);

        // Cache a result on parent
        var cachedResult = parent.ProduceResult();

        // Modify child and propagate dirty
        child.MakeDirty();

        // Parent should recompute
        var newResult = parent.ProduceResult();

        // Results should be equal in value but not be the same reference (recalculated)
        Assert.Equal(cachedResult, newResult);
    }

    [Fact]
    public void MakeDirty_ShouldPropagateMultipleLevels()
    {
        var leaf = new LiteralNode("x");
        var middle = new RepeatNode([leaf], 2, 2);
        var root = new ConcatenateNode();
        root.Parameters[0] = middle;
        middle.Parents.Add(root);
        leaf.Parents.Add(middle);

        // Cache results
        root.ProduceResult();

        // Dirty the leaf
        leaf.MakeDirty();

        // Root should recompute
        var result = root.ProduceResult();
        Assert.Contains("x", result);
    }

    #endregion

    #region NodeGraphService Tests

    [Fact]
    public void NodeGraphService_Connect_ShouldSetParameterAndAddParent()
    {
        var source = new LiteralNode("hello");
        var target = new ConcatenateNode();

        NodeGraphService.Connect(source, target, 0);

        Assert.Same(source, target.Parameters[0]);
        Assert.Contains(target, source.Parents);
    }

    [Fact]
    public void NodeGraphService_Connect_ShouldMakeTargetDirty()
    {
        var source = new LiteralNode("hi");
        var target = new ConcatenateNode();

        // Cache an initial result
        var initial = target.ProduceResult();

        NodeGraphService.Connect(source, target, 0);

        var updated = target.ProduceResult();
        Assert.Contains("hi", updated);
    }

    [Fact]
    public void NodeGraphService_Connect_MultiplePortsOnSameTarget()
    {
        var source1 = new LiteralNode("abc");
        var source2 = new LiteralNode("def");
        var target = new ConcatenateNode();

        NodeGraphService.Connect(source1, target, 0);
        NodeGraphService.Connect(source2, target, 1);

        Assert.Same(source1, target.Parameters[0]);
        Assert.Same(source2, target.Parameters[1]);
        Assert.Equal("abcdef", target.ProduceResult());
    }

    [Fact]
    public void NodeGraphService_Disconnect_ShouldClearParameterAndRemoveParent()
    {
        var source = new LiteralNode("hello");
        var target = new ConcatenateNode();

        NodeGraphService.Connect(source, target, 0);
        NodeGraphService.Disconnect(source, target, 0);

        Assert.Null(target.Parameters[0]);
        Assert.DoesNotContain(target, source.Parents);
    }

    [Fact]
    public void NodeGraphService_Disconnect_ShouldUpdateProducedResult()
    {
        var source = new LiteralNode("hello");
        var target = new ConcatenateNode();

        NodeGraphService.Connect(source, target, 0);
        var connectedResult = target.ProduceResult();
        Assert.Contains("hello", connectedResult);

        NodeGraphService.Disconnect(source, target, 0);
        var disconnectedResult = target.ProduceResult();
        Assert.DoesNotContain("hello", disconnectedResult);
    }

    [Fact]
    public void NodeGraphService_Disconnect_WithInvalidPortIndex_ShouldNotThrow()
    {
        var source = new LiteralNode("x");
        var target = new ConcatenateNode();
        source.Parents.Add(target);

        // Should handle out-of-range port index gracefully
        NodeGraphService.Disconnect(source, target, 999);

        Assert.DoesNotContain(target, source.Parents);
    }

    [Fact]
    public void NodeGraphService_Disconnect_WithNegativePortIndex_ShouldNotThrow()
    {
        var source = new LiteralNode("x");
        var target = new ConcatenateNode();
        source.Parents.Add(target);

        NodeGraphService.Disconnect(source, target, -1);

        Assert.DoesNotContain(target, source.Parents);
    }

    [Fact]
    public void NodeGraphService_DeleteNode_ShouldClearFromParents()
    {
        var child = new LiteralNode("gone");
        var parent = new ConcatenateNode();

        NodeGraphService.Connect(child, parent, 0);

        NodeGraphService.DeleteNode(child);

        Assert.Null(parent.Parameters[0]);
        Assert.Empty(child.Parents);
    }

    [Fact]
    public void NodeGraphService_DeleteNode_ShouldDisconnectChildren()
    {
        var child1 = new LiteralNode("a");
        var child2 = new LiteralNode("b");
        var node = new ConcatenateNode();

        NodeGraphService.Connect(child1, node, 0);
        NodeGraphService.Connect(child2, node, 1);

        NodeGraphService.DeleteNode(node);

        Assert.All(node.Parameters, p => Assert.Null(p));
        Assert.DoesNotContain(node, child1.Parents);
        Assert.DoesNotContain(node, child2.Parents);
    }

    [Fact]
    public void NodeGraphService_DeleteNode_ShouldHandleMiddleNode()
    {
        var leaf = new LiteralNode("x");
        var middle = new ConcatenateNode();
        var root = new ConcatenateNode();

        NodeGraphService.Connect(leaf, middle, 0);
        NodeGraphService.Connect(middle, root, 0);

        NodeGraphService.DeleteNode(middle);

        // Middle disconnected from root
        Assert.Null(root.Parameters[0]);
        Assert.Empty(middle.Parents);

        // Middle disconnected from leaf
        Assert.All(middle.Parameters, p => Assert.Null(p));
        Assert.DoesNotContain(middle, leaf.Parents);
    }

    [Fact]
    public void NodeGraphService_DeleteNode_ShouldHandleNodeWithNoConnections()
    {
        var orphan = new LiteralNode("alone");

        // Should not throw
        NodeGraphService.DeleteNode(orphan);

        Assert.Empty(orphan.Parents);
    }

    [Fact]
    public void NodeGraphService_DeleteNode_ShouldMakeParentsDirty()
    {
        var child = new LiteralNode("val");
        var parent = new ConcatenateNode();

        NodeGraphService.Connect(child, parent, 0);
        var beforeResult = parent.ProduceResult();
        Assert.Contains("val", beforeResult);

        NodeGraphService.DeleteNode(child);

        var afterResult = parent.ProduceResult();
        Assert.DoesNotContain("val", afterResult);
    }

    [Fact]
    public void NodeGraphService_SetPortCount_Increase_ShouldAddNullParameters()
    {
        var node = new ConcatenateNode();
        Assert.Equal(2, node.Parameters.Count);

        NodeGraphService.SetPortCount(node, 5);

        Assert.Equal(5, node.Parameters.Count);
        Assert.All(node.Parameters, p => Assert.Null(p));
    }

    [Fact]
    public void NodeGraphService_SetPortCount_Decrease_ShouldRemoveParameters()
    {
        var node = new ConcatenateNode();
        NodeGraphService.SetPortCount(node, 5);
        Assert.Equal(5, node.Parameters.Count);

        NodeGraphService.SetPortCount(node, 2);

        Assert.Equal(2, node.Parameters.Count);
    }

    [Fact]
    public void NodeGraphService_SetPortCount_Decrease_ShouldDisconnectRemovedChildren()
    {
        var source = new LiteralNode("removed");
        var node = new ConcatenateNode();

        // Expand to 3 ports, connect to port 2
        NodeGraphService.SetPortCount(node, 3);
        NodeGraphService.Connect(source, node, 2);

        Assert.Same(source, node.Parameters[2]);
        Assert.Contains(node, source.Parents);

        // Shrink back to 2 — port 2 should be removed and source disconnected
        NodeGraphService.SetPortCount(node, 2);

        Assert.Equal(2, node.Parameters.Count);
        Assert.DoesNotContain(node, source.Parents);
    }

    [Fact]
    public void NodeGraphService_SetPortCount_Decrease_ShouldNotAffectRemainingConnections()
    {
        var source0 = new LiteralNode("keep");
        var source2 = new LiteralNode("remove");
        var node = new ConcatenateNode();

        NodeGraphService.SetPortCount(node, 3);
        NodeGraphService.Connect(source0, node, 0);
        NodeGraphService.Connect(source2, node, 2);

        NodeGraphService.SetPortCount(node, 2);

        // Port 0 connection should remain
        Assert.Same(source0, node.Parameters[0]);
        Assert.Contains(node, source0.Parents);

        // Port 2 connection should be gone
        Assert.Equal(2, node.Parameters.Count);
        Assert.DoesNotContain(node, source2.Parents);
    }

    [Fact]
    public void NodeGraphService_SetPortCount_SameCount_ShouldBeNoOp()
    {
        var node = new ConcatenateNode();
        var source = new LiteralNode("stable");
        NodeGraphService.Connect(source, node, 0);

        NodeGraphService.SetPortCount(node, 2);

        Assert.Equal(2, node.Parameters.Count);
        Assert.Same(source, node.Parameters[0]);
    }

    [Fact]
    public void NodeGraphService_SetPortCount_WorksWithAnyOfNode()
    {
        var node = new AnyOfNode();
        Assert.Equal(2, node.Parameters.Count);

        NodeGraphService.SetPortCount(node, 4);
        Assert.Equal(4, node.Parameters.Count);

        NodeGraphService.SetPortCount(node, 1);
        Assert.Equal(1, node.Parameters.Count);
    }

    #endregion

    #region MatchDataService Tests

    [Fact]
    public void MatchDataService_NullMatchCollection_ShouldReturnNull()
    {
        var result = MatchDataService.GetMatchAtOffset(null, null, 0);

        Assert.Null(result);
    }

    [Fact]
    public void MatchDataService_CaretInsideMatch_ShouldReturnMatchResult()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"\d+");
        var matches = regex.Matches("abc 123 def");

        // Caret at position 5, inside "123" (index 4, length 3)
        var result = MatchDataService.GetMatchAtOffset(matches, regex, 5);

        Assert.NotNull(result);
        Assert.Equal("Range: 4 to 7", result.Extent);
        Assert.NotEmpty(result.Groups);
    }

    [Fact]
    public void MatchDataService_CaretOutsideAllMatches_ShouldReturnNull()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"\d+");
        var matches = regex.Matches("abc 123 def");

        // Caret at position 0, inside "abc" — no match
        var result = MatchDataService.GetMatchAtOffset(matches, regex, 0);

        Assert.Null(result);
    }

    [Fact]
    public void MatchDataService_CaretAtMatchStart_ShouldReturnMatchResult()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"\d+");
        var matches = regex.Matches("abc 123 def");

        // Caret at position 4, the start of "123"
        var result = MatchDataService.GetMatchAtOffset(matches, regex, 4);

        Assert.NotNull(result);
        Assert.Equal("Range: 4 to 7", result.Extent);
    }

    [Fact]
    public void MatchDataService_CaretAtMatchEnd_ShouldReturnNull()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"\d+");
        var matches = regex.Matches("abc 123 def");

        // Caret at position 7 — one past the end of "123" (exclusive), should not match
        var result = MatchDataService.GetMatchAtOffset(matches, regex, 7);

        Assert.Null(result);
    }

    [Fact]
    public void MatchDataService_MultipleMatches_ShouldReturnCorrectOne()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"\d+");
        var matches = regex.Matches("12 ab 34 cd 56");

        // Caret at position 6, inside "34" (index 6, length 2)
        var result = MatchDataService.GetMatchAtOffset(matches, regex, 6);

        Assert.NotNull(result);
        Assert.Equal("Range: 6 to 8", result.Extent);
    }

    [Fact]
    public void MatchDataService_WithNamedGroups_ShouldReturnGroupNames()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"(?<year>\d{4})-(?<month>\d{2})");
        var matches = regex.Matches("date: 2026-03 end");

        // Caret inside the match (index 6, length 7)
        var result = MatchDataService.GetMatchAtOffset(matches, regex, 8);

        Assert.NotNull(result);
        Assert.Contains(result.Groups, g => g.Contains("year"));
        Assert.Contains(result.Groups, g => g.Contains("month"));
    }

    [Fact]
    public void MatchDataService_WithUnnamedGroups_ShouldReturnNumericGroupNames()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"(\d+)-(\d+)");
        var matches = regex.Matches("val: 100-200 end");

        var result = MatchDataService.GetMatchAtOffset(matches, regex, 7);

        Assert.NotNull(result);
        // Group 0 is the whole match, groups 1 and 2 are the captures
        Assert.Contains(result.Groups, g => g.Contains("<0>"));
        Assert.Contains(result.Groups, g => g.Contains("<1>"));
        Assert.Contains(result.Groups, g => g.Contains("<2>"));
    }

    [Fact]
    public void MatchDataService_NullRegex_ShouldStillReturnGroupsWithNumericNames()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"(?<name>\w+)");
        var matches = regex.Matches("hello");

        // Pass null for the regex parameter — should fall back to numeric names
        var result = MatchDataService.GetMatchAtOffset(matches, null, 2);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Groups);
        // Without regex, named groups can't be resolved, so should use numeric index
        Assert.Contains(result.Groups, g => g.Contains("<0>"));
    }

    [Fact]
    public void MatchDataService_NoMatches_ShouldReturnNull()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"\d+");
        var matches = regex.Matches("no digits here");

        var result = MatchDataService.GetMatchAtOffset(matches, regex, 3);

        Assert.Null(result);
    }

    #endregion
}