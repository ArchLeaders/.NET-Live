using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotnetLive.Models;
using DotnetLive.Views;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace DotnetLive.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly SettingsViewModel _settings = SettingsViewModel.Load();
    private readonly ScriptOptions _roslynDefaultOptions = ScriptOptions.Default.AddReferences(
        typeof(ShellViewModel).Assembly.GetReferencedAssemblies().Select((x) => Assembly.Load(x.FullName)));

    public ShellViewModel()
    {
        Console.SetOut(new ShellViewWriter(this));
    }

    [ObservableProperty]
    private string _context = Directory.GetCurrentDirectory();

    [ObservableProperty]
    private ObservableCollection<Exception> _errors = new();

    [ObservableProperty]
    private int _selectedErrorIndex = -1;

    [ObservableProperty]
    private string _output = string.Empty;

    [RelayCommand]
    private async Task Execute(TextEditor editor)
    {
        await App.LoadRoslyn;
        await Console.Out.WriteLineAsync($"[Executing Script]\n");

        try {
            object? result = await CSharpScript.EvaluateAsync(editor.Text, _roslynDefaultOptions);
            string? textResult = result?.ToString();
            await Console.Out.WriteLineAsync(
                $"\n{(string.IsNullOrEmpty(textResult) ? "Execution returned void" : textResult)}");

            ClearErrors();
        }
        catch (Exception ex) {
            Errors.Add(ex);
            SelectedErrorIndex = Errors.Count - 1;
        }
    }

    [RelayCommand]
    private void ClearErrors()
    {
        Errors.Clear();
    }

    [RelayCommand]
    private void ClearOutput()
    {
        Output = string.Empty;
    }

    [RelayCommand]
    private static async Task Save(TextEditor editor)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            IStorageFile? file = await desktop.MainWindow!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
                DefaultExtension = ".cs",
                FileTypeChoices = new List<FilePickerFileType>() {
                    new FilePickerFileType("C# Source File") {
                        Patterns = new List<string>() {
                            "*.cs"
                        }
                    }
                },
                Title = "Save C# Source File"
            });

            if (file != null) {
                using Stream writer = await file.OpenWriteAsync();
                writer.Write(Encoding.UTF8.GetBytes(editor.Text));
            }
        }
    }

    [RelayCommand]
    private static async Task Import(TextEditor editor)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            IReadOnlyList<IStorageFile> files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>() {
                    new FilePickerFileType("C# Source File") {
                        Patterns = new List<string>() {
                            "*.cs"
                        }
                    }
                },
                Title = "Import C# Source File"
            });

            if (files.Count == 1) {
                using Stream stream = await files[0].OpenReadAsync();
                using StreamReader reader = new(stream);
                editor.Text = await reader.ReadToEndAsync();
            }
        }
    }

    [RelayCommand]
    private static void Info()
    {

    }

    [RelayCommand]
    private void OpenSettings(ContentControl overflow)
    {
        overflow.Content = new SettingsView {
            Close = () => overflow.Content = null,
            Apply = () => { },
            DataContext = _settings
        };
    }
}
