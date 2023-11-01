using System.Collections.ObjectModel;
using System.Reflection;

namespace DotnetLive.Models;

public class ImportedAssemblies : ObservableCollection<string>
{
    private static readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imported-Assemblies");

    public List<Assembly> Cache { get; } = new();

    public void Load()
    {
        if (Directory.Exists(_path)) {
            foreach (var path in Directory.EnumerateFiles(_path, "*.dll", SearchOption.AllDirectories)) {
                Add(Path.GetFileNameWithoutExtension(path));
                Cache.Add(Assembly.LoadFrom(path));
            }
        }
    }

    public void Import(string file)
    {
        if (!File.Exists(file)) {
            return;
        }

        string path = Path.Combine(_path, Path.GetFileNameWithoutExtension(file).ToLower());
        Directory.CreateDirectory(path);

        string output = Path.Combine(path, Path.GetFileName(file));
        File.Copy(file, output, true);

        try {
            Assembly root = Assembly.LoadFrom(output);
            foreach (var assembly in root.GetReferencedAssemblies()) {
                Import(Path.Combine(Path.GetDirectoryName(file)!, $"{assembly.Name}.dll"));
            }

            Cache.Add(root);
            Add(Path.GetFileNameWithoutExtension(file));
        }
        catch (Exception ex) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Import Error]");
            Console.WriteLine(ex);
            Console.ResetColor();
        }
    }

    public void Delete(string name)
    {
        string path = Path.Combine(_path, name + ".dll");
        if (File.Exists(path)) {
            File.Delete(path);
            Remove(name);
        }
    }
}
