using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RegexMaker.Nodes;
internal abstract class RgxNode : IRgxNode
{
    public static System.Random random = new Random();

    private static int _idCounter = 0;
    public int ID { get; private set; }
    public RgxNodeType NodeType { get; }
    public IList<IRgxNode> Parameters { get; private set; }

    public virtual string Name => NameFromType();

    private string? _cachedResult;

    internal static List<RgxNode> Exemplars { get; private set; }

    static RgxNode()
    {
        _idCounter = 0;
        var derivedTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(RgxNode)))
            .ToList();

        Exemplars = new List<RgxNode>();

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
                Exemplars.Add((RgxNode)Activator.CreateInstance(type));
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
}