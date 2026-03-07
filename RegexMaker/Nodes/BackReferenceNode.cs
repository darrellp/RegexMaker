using System.Collections.Generic;
using System.Text.Json;
using RegexStringLibrary;

namespace RegexMaker.Nodes;

/// <summary>
///     A node that matches a previously captured group by name.
/// </summary>
public class BackReferenceNode : RgxNode
{
    public BackReferenceNode() : base(RgxNodeType.BackReference)
    {
        GroupName = "group1";
    }

    public BackReferenceNode(string groupName) : base(RgxNodeType.BackReference)
    {
        GroupName = groupName;
    }

    public string GroupName { get; set; } = string.Empty;

    public override string DisplayName => $"BRef {GroupName}";

    internal override string CalculateResult()
    {
        if (string.IsNullOrEmpty(GroupName))
            return string.Empty;

        if (int.TryParse(GroupName, out var groupNumber))
            // BackReference by number
            return Stex.BRef(groupNumber);
        return Stex.BRef(GroupName);
    }

    public override string RandomMatch()
    {
        // BackReference matches whatever was captured by the named group.
        // Without context of the actual capture, we return empty.
        return string.Empty;
    }

    public override IRgxNode Default()
    {
        return new BackReferenceNode();
    }

    public override string RawCode(CodeCollector cc)
    {
        return int.TryParse(GroupName, out var groupNumber)
            ? $"Stex.BRef({groupNumber})"
            : $@"Stex.BRef(""{GroupName}"")";
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["GroupName"] = GroupName;
    }

    protected override void RestoreSerializationData(Dictionary<string, JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("GroupName", out var groupNameElement))
            GroupName = groupNameElement.GetString() ?? "group1";
    }
}