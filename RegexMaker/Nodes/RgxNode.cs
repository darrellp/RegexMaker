using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RegexMaker.Nodes;
public abstract class RgxNode : IRgxNode
{
    public static Random random = new();

    private static int _idCounter = 0;
    public int ID { get; private set; }
    public RgxNodeType NodeType { get; }
    public IList<IRgxNode?> Parameters { get; set; }

    // Default implementation of Name property takes name from the enum type of the NodeType.
    // Can be overridden by derived classes if needed.
    public virtual string Name => NameFromType();

    private string? _cachedResult;

    internal static List<RgxNode> Exemplars { get; private set; }

    static RgxNode()
    {
        _idCounter = 0;

        // Get all non-abstract classes that derive from RgxNode in the current assembly - i.e., our node types.
        var derivedTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(RgxNode)))
            .ToList();

        Exemplars = [];

        // Create instances (assumes parameterless constructor or handle constructor parameters)
        foreach (var type in derivedTypes)
        {
            // Check for parameterless constructor
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                Debug.WriteLine($"<<<<<<<<<<<< {type.Name} does not have a parameterless constructor. Skipping exemplar creation. >>>>>>>>>>>>>");
                continue;
            }

            try
            {
                RgxNode? node = (RgxNode?)Activator.CreateInstance(type);
                Debug.Assert(node is not null, $"Failed to create exemplar for {type.Name}");
                Exemplars.Add(node);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not create exemplar for {type.Name}: {ex.Message}");
            }
        }
    }

    private string NameFromType()
    {
        return NodeType.ToString();
    }

    public RgxNode(RgxNodeType rgxType, params IRgxNode[] parameters)
    {
        ID = _idCounter++;
        NodeType = rgxType;
        Parameters = parameters.ToList();
        _cachedResult = null;
    }

    internal abstract string CalculateResult();

    public string ProduceResult()
    {
        _cachedResult ??= CalculateResult();
        return _cachedResult;
    }

    public void MakeDirty()
    {
        _cachedResult = null;
    }

    public bool Matches(string input)
    {
        // Produce the regex pattern from the node structure.
        string pattern = ProduceResult();
        // Use Regex.IsMatch to check if the input matches the produced pattern.
        return Regex.IsMatch(input, $"^{pattern}$");
    }

    public virtual string RandomMatch()
    {
        throw new System.NotImplementedException();
    }

    public static RgxNode NameToNode(string name)
    {
        // Find the exemplar with the matching name.
        var exemplar = Exemplars.FirstOrDefault(e => e.Name == name);
        var ret = exemplar?.Default() as RgxNode;
        if (ret is null)
        {
            throw new ArgumentException($"No node type found with name: {name}");
        }
        return ret;
    }

    public abstract IRgxNode Default();
}