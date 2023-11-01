using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotnetLive.Helpers;
using DotnetLive.Models;
using DotnetLive.Views;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public ScriptOptions ScriptOptions { get; private set; } = ScriptOptions.Default
        .AddReferences(typeof(ShellViewModel).Assembly
            .GetReferencedAssemblies()
            .Select((x) => Assembly.Load(x.FullName))
    );

    [ObservableProperty]
    [property: JsonIgnore]
    private ImportedAssemblies _assemblies = new();

    [ObservableProperty]
    private ObservableCollection<NuGetPackage> _packages = new();

    [RelayCommand]
    [property: JsonIgnore]
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
    [property: JsonIgnore]
    private void DeleteAssembly(string name)
    {
        Assemblies.Delete(name);
    }

    [RelayCommand]
    [property: JsonIgnore]
    private async Task AddPackage(string name)
    {
        if (!string.IsNullOrEmpty(name)) {
            NuGetPackage pkg = new() {
                Name = name
            };

            Packages.Add(pkg);
            await pkg.Restore();
        }
    }

    [RelayCommand]
    [property: JsonIgnore]
    private void RemovePackage(NuGetPackage package)
    {
        Packages.Remove(package);
    }

    [RelayCommand]
    [property: JsonIgnore]
    private static void Cancel(SettingsView view)
    {
        view.Close();
    }

    [RelayCommand]
    [property: JsonIgnore]
    private async Task Save(SettingsView view)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? string.Empty);

        using FileStream fs = File.Create(_path);
        await JsonSerializer.SerializeAsync(fs, this);

        Apply();
        view.Close();
    }

    public async Task RestoreNuGetPackages()
    {
        NuGetHelper.DeleteLocalRepo();

        await Parallel.ForEachAsync(Packages.DistinctBy(x => x.Name), async (package, token) => {
            await package.Restore();
        });
    }

    public void Apply()
    {
        ScriptOptions = ScriptOptions
            .AddReferences(NuGetHelper.PathCache.Select(Assembly.LoadFrom));

        NuGetHelper.PathCache.Clear();
    }
}
