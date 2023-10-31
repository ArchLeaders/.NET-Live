using Avalonia.Controls;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace DotnetLive.Views;
public partial class ShellView : UserControl
{
    public ShellView()
    {
        InitializeComponent();

        RegistryOptions registryOptions = new(ThemeName.DarkPlus);
        TextMate.Installation textMateInstallation = CodeEditor.InstallTextMate(registryOptions);
        textMateInstallation.SetGrammar("source.cs");

        CodeEditor.Text = """
            using System;

            Console.WriteLine("Hello World!");
            """;
    }
}
