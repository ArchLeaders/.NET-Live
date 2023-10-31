using Avalonia.Controls;

namespace DotnetLive.Views;
public partial class SettingsView : UserControl
{
    public required Action Close { get; set; }
    public required Action Apply { get; set; }

    public SettingsView()
    {
        InitializeComponent();
    }
}
