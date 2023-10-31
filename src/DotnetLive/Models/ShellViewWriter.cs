using DotnetLive.ViewModels;
using System.Text;

namespace DotnetLive.Models;

public class ShellViewWriter : TextWriter
{
    private readonly ShellViewModel _shellViewModel;

    public override Encoding Encoding { get; } = Encoding.UTF8;

    public ShellViewWriter(ShellViewModel shellViewModel)
    {
        _shellViewModel = shellViewModel;
    }

    public override void Write(string? value)
    {
        _shellViewModel.Output += value;
    }
}
