using RegexStringLibrary;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RegexMaker.Nodes;

/// <summary>
/// A node that matches a previously captured group by name.
/// </summary>
public class BackReferenceNode : RgxNode
{
    public string GroupName { get; set; } = string.Empty;

    public BackReferenceNode() : base(RgxNodeType.BackReference)
    {
        GroupName = "group1";
    }

    public BackReferenceNode(string groupName) : base(RgxNodeType.BackReference)
    {
        GroupName = groupName;
    }

    internal override string CalculateResult()
    {
        if (string.IsNullOrEmpty(GroupName))
            return string.Empty;

        if (Int32.TryParse(GroupName, out int groupNumber))
        {
            // BackReference by number
            return Stex.BRef(groupNumber);
        }
        return Stex.BRef(GroupName);
    }

    public override string RandomMatch()
    {
        // BackReference matches whatever was captured by the named group.
        // Without context of the actual capture, we return empty.
        return string.Empty;
    }

    public override string DisplayName => $"BRef {GroupName}";

    public override IRgxNode Default()
    {
        return new BackReferenceNode();
    }

    public override string RawCode(CodeCollector cc)
    {
        return Int32.TryParse(GroupName, out int groupNumber)
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
        {
            GroupName = groupNameElement.GetString() ?? "group1";
        }
    }
}