using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotnetLive.Views;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Collections.ObjectModel;
using System.Text;

namespace DotnetLive.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private string _context = Directory.GetCurrentDirectory();

    [ObservableProperty]
    private ObservableCollection<Exception> _errors = new();

    [ObservableProperty]
    private int _selectedErrorIndex = -1;

    [RelayCommand]
    private async Task Execute(TextEditor editor)
    {
        await App.LoadRoslyn;
        await App.RestorePackages;
        await Console.Out.WriteLineAsync($"[Executing Script]\n");

        try {
            object? result = await CSharpScript.EvaluateAsync(editor.Text, App.Settings.ScriptOptions);
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
    private static void ClearOutput()
    {
        Console.Clear();
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
    private static void OpenSettings(ContentControl overflow)
    {
        overflow.Content = new SettingsView {
            Close = () => overflow.Content = null,
            Apply = () => { },
            DataContext = App.Settings
        };
    }
}
