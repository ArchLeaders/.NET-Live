using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotnetLive.Helpers;

namespace DotnetLive.Models;

public partial class NuGetPackage : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _version;

    [ObservableProperty]
    private string? _source;

    [RelayCommand]
    public async Task Restore()
    {
        await NuGetHelper.TryDownloadPackage(Name, Version, Source);
    }
}
