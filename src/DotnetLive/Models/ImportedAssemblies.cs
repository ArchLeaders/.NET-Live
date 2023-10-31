using System.Collections.ObjectModel;

namespace DotnetLive.Models;

public class ImportedAssemblies : ObservableCollection<string>
{
    private static readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImportedAssemblies");

    public void Load()
    {
        if (Directory.Exists(_path)) {
            foreach (var path in Directory.EnumerateFiles(_path, "*.dll")) {
                Add(Path.GetFileNameWithoutExtension(path));
            }
        }
    }

    public void Import(string path)
    {
        if (File.Exists(path)) {
            Directory.CreateDirectory(_path);
            File.Copy(path, Path.Combine(_path, Path.GetFileName(path)));
            Add(Path.GetFileNameWithoutExtension(path));
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
