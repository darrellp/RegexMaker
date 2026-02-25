using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes;
using RegexMaker.Nodes;
using System.Collections.Generic;

namespace RegexMaker.Benchmarks;

[MemoryDiagnoser]
public class MainViewBenchmarks
{
    private Dictionary<RgxNode, int> _nodeToIndex = null!;
    private List<RgxNode> _nodes = null!;
    private RgxNode _targetNode = null!;
    private const int NodeCount = 100;

    [GlobalSetup]
    public void Setup()
    {
        _nodeToIndex = new Dictionary<RgxNode, int>();
        _nodes = new List<RgxNode>();
        
        // Create 100 nodes to simulate a realistic canvas
        for (int i = 0; i < NodeCount; i++)
        {
            var node = RgxNode.NameToNode("Literal");
            _nodes.Add(node);
            _nodeToIndex[node] = i;
        }

        // Set the target node to be the last one (worst case scenario)
        _targetNode = _nodes[NodeCount - 1];
    }

    [Benchmark]
    public int? FindNodeIndex_LinearSearch()
    {
        int? found = null;
        for (int i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i] == _targetNode)
            {
                found = i;
                break;
            }
        }
        return found;
    }

    [Benchmark]
    public int? FindNodeIndex_DictionaryLookup()
    {
        return _nodeToIndex.TryGetValue(_targetNode, out var index) ? index : null;
    }

    [Benchmark]
    public RgxNode? NameToNode_Lookup()
    {
        return RgxNode.NameToNode("Literal");
    }
}