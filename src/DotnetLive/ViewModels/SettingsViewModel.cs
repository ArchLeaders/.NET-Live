using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotnetLive.Models;
using DotnetLive.Views;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DotnetLive.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private static readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".net-live", "Config.json");
    
    public static SettingsViewModel Load()
    {
        if (!File.Exists(_path)) {
            return new();
        }

        using FileStream fs = File.OpenRead(_path);
        SettingsViewModel result = JsonSerializer.Deserialize<SettingsViewModel>(fs)
            ?? throw new InvalidOperationException("Could not deserialize FileStream");

        result.Assemblies.Load();
        return result;
    }

    [ObservableProperty]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private ImportedAssemblies _assemblies = new();

    [ObservableProperty]
    private ObservableCollection<NuGetPackage> _packages = new();

    [RelayCommand]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private async Task ImportAssembly()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            IReadOnlyList<IStorageFile> files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
                AllowMultiple = true,
                FileTypeFilter = new List<FilePickerFileType>() {
                    new FilePickerFileType("Application Extension") {
                        Patterns = new List<string>() {
                            "*.dll"
                        }
                    }
                },
                Title = "Import .NET Assembly"
            });

            foreach (var file in files.Select(x => x.TryGetLocalPath() ?? string.Empty).Where(File.Exists)) {
                Assemblies.Import(file);
            }
        }
    }

    [RelayCommand]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private void DeleteAssembly(string name)
    {
        Assemblies.Delete(name);
    }

    [RelayCommand]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private void AddPackage(string name)
    {
        if (!string.IsNullOrEmpty(name)) {
            Packages.Add(new NuGetPackage {
                Name = name,
            });
        }
    }

    [RelayCommand]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private void RemovePackage(NuGetPackage package)
    {
        Packages.Remove(package);
    }

    [RelayCommand]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private static void Restore()
    {
        // TODO: Restore NuGet packages and dependencies
    }

    [RelayCommand]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private static void Cancel(SettingsView view)
    {
        view.Close();
    }

    [RelayCommand]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private async Task Save(SettingsView view)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? string.Empty);

        using FileStream fs = File.Create(_path);
        await JsonSerializer.SerializeAsync(fs, this);

        view.Apply();
        view.Close();
    }
}
