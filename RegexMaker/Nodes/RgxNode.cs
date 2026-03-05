using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RegexMaker.Nodes;
public abstract class RgxNode : IRgxNode
{
    public static Random random = new();
    
    public string? VariableName { get; set; }

    private static int _idCounter = 0;
    public int ID { get; private set; }
    public RgxNodeType NodeType { get; }
    public IList<IRgxNode?> Parameters { get; set; }
    public IList<IRgxNode> Parents { get; set; } = [];

    // Default implementation of Name property takes name from the enum type of the NodeType.
    // Can be overridden by derived classes if needed.
    public virtual string Name => NameFromType();
    public virtual string DisplayName => Name ?? "No Node";

    private string? _cachedResult;

    public void InitializeForCode()
    {
        VariableName = null;
        foreach (var parameter in Parameters)
        {
            (parameter as RgxNode)?.InitializeForCode();
        }
    }
    
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

    internal bool CheckRename(CodeCollector cc, bool fForce = false)
    {
        // We can't have fForce being true when we've already got a Variable name
        Debug.Assert(!(fForce && VariableName is not null));
        if ((VariableName != null || Parents.Count <= 1) && !fForce)
        {
            return false;
        }
        var variableBaseName = this.GetType().Name;
        if (variableBaseName.EndsWith("Node"))
        {
            variableBaseName = variableBaseName.Remove(variableBaseName.Length - "Node".Length);
        }

        var code = Code(cc);
        VariableName = cc.NextVariable(variableBaseName);
        cc.AddCode(VariableName, code);
        return true;
    }

    public virtual string Code(CodeCollector cc)
    {
        return $"DEBUG ERROR: No Code for {DisplayName}";
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
        Parents.ToList().ForEach(p => (p as RgxNode)?.MakeDirty());
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

    /// <summary>
    /// Serializes the node's state to JSON
    /// </summary>
    public virtual string SerializeToJson()
    {
        var data = new Dictionary<string, object?>
        {
            ["NodeType"] = NodeType.ToString(),
            ["ID"] = ID
        };
        
        // Add any additional state specific to derived types
        AddSerializationData(data);
        
        return JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// Override this in derived classes to add custom serialization data
    /// </summary>
    protected virtual void AddSerializationData(Dictionary<string, object?> data)
    {
        // Base implementation does nothing - override in derived classes
    }

    /// <summary>
    /// Deserializes the node's state from JSON
    /// </summary>
    public virtual void DeserializeFromJson(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (data != null)
            {
                RestoreSerializationData(data);
            }
        }
        catch
        {
            // Handle deserialization errors gracefully
        }
    }

    /// <summary>
    /// Override this in derived classes to restore custom serialization data
    /// </summary>
    protected virtual void RestoreSerializationData(Dictionary<string, JsonElement> data)
    {
        // Base implementation does nothing - override in derived classes
    }
}