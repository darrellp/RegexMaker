using Avalonia.Controls;
using Avalonia.Controls.Templates;
using RegexMaker.ViewModels;
using System;
using System.Diagnostics.CodeAnalysis;

namespace RegexMaker;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        // Match is not implemented because Avalonia's ContentControl will call Build for every view 
        // model and we don't want to exclude any view models from being processed. If Build returns 
        // null, the ContentControl will just display nothing, which is the desired behavior for view 
        // models that don't have corresponding views.
        throw new NotImplementedException("Match is not implemented");
    }
}