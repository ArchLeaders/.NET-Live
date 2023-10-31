using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using DotnetLive.ViewModels;
using DotnetLive.Views;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace DotnetLive;

public partial class App : Application
{
    public static SettingsViewModel Settings { get; } = SettingsViewModel.Load();

    public static Task LoadRoslyn { get; } = Task.Run(async () => {
        await CSharpScript.EvaluateAsync("""
            using System;
            
            Console.WriteLine("[Roslyn Initialized]");
            """, ScriptOptions.Default);
    });

    public static Task RestorePackages { get; } = Task.Run(async () => {
        await Settings.RestoreNuGetPackages();
        Settings.Apply();
        await Console.Out.WriteLineAsync("[Packages Restored]");
    });

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);

        IconProvider.Current
            .Register(new FontAwesomeIconProvider());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new ShellWindow {
                DataContext = new ShellViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
            singleViewPlatform.MainView = new ShellView {
                DataContext = new ShellViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
