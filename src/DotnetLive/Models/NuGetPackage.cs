using CommunityToolkit.Mvvm.ComponentModel;

namespace DotnetLive.Models;

public partial class NuGetPackage : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _version;

    [ObservableProperty]
    private string? _source;
}
