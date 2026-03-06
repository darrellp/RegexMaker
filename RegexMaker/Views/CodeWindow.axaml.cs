using Avalonia.Controls;

namespace RegexMaker.Views;

public partial class CodeWindow : Window
{
    public CodeWindow()
    {
        InitializeComponent();
    }

    public CodeWindow(string code) : this()
    {
        CodeEditor.Text = code;
    }
}