using Microsoft.CodeAnalysis;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Versioning;

namespace DotnetLive.Helpers;

public static class NuGetHelper
{
    private const string _defaultSource = "https://api.nuget.org/v3/index.json";
    private static readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NuGet-Repository");
    private static readonly NuGetFramework _targetFramework;

    public static ConcurrentBag<string> PathCache { get; } = new();

    static NuGetHelper()
    {
        string? frameworkName = Assembly.GetExecutingAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName;
        _targetFramework = frameworkName is null
            ? NuGetFramework.AnyFramework
            : NuGetFramework.ParseFrameworkName(frameworkName, new DefaultFrameworkNameProvider());
    }

    public static void DeleteLocalRepo()
    {
        Directory.Delete(_path, true);
    }

    public static async Task<bool> TryDownloadPackage(string name, string? expectedVersion = null, string? source = null, CancellationToken? cancellationToken = null)
    {
        source ??= _defaultSource;
        CancellationToken token = cancellationToken ?? CancellationToken.None;

        SourceCacheContext cache = new();
        SourceRepository repo = Repository.Factory.GetCoreV3(source);
        FindPackageByIdResource query = await repo.GetResourceAsync<FindPackageByIdResource>();

        NuGetVersion? version;
        IEnumerable<NuGetVersion> versions = await query.GetAllVersionsAsync(name, cache, NullLogger.Instance, token);

        if (expectedVersion != null && versions.Any(x => x.ToFullString() == expectedVersion)) {
            version = new(expectedVersion);
        }
        else {
            version = versions.LastOrDefault();
        }

        if (version == null) {
            await Console.Out.WriteLineAsync($"[Restore] Package '{name}' not found on source '{repo.PackageSource.Source}'");
            return false;
        }

        string path = Path.Combine(_path, name.ToLower(), version.ToFullString());
        string nupkg = Path.Combine(path, $"{name}.nupkg");
        Directory.CreateDirectory(path);
        PackageIdentity id = new(name, version);

        IPackageDownloader downloader = await query.GetPackageDownloaderAsync(id, cache, NullLogger.Instance, token);
        await downloader.CopyNupkgFileToAsync(nupkg, token);

        IEnumerable<FrameworkSpecificGroup> groups = await downloader.ContentReader.GetLibItemsAsync(token);
        if (TryGetMostCompatibleGroup(groups, out FrameworkSpecificGroup? group)) {
            foreach (var file in group!.Items) {
                using Stream stream = await downloader.CoreReader.GetStreamAsync(file, token);
                string outputFile = Path.Combine(path, file);

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile) ?? string.Empty);
                using FileStream fs = File.Create(outputFile);
                await stream.CopyToAsync(fs);

                if (outputFile.EndsWith(".dll")) {
                    PathCache.Add(outputFile);
                }
            }

            if (downloader.ContentReader is PackageArchiveReader reader) {
                if (reader.NuspecReader.GetDependencyGroups().FirstOrDefault(x => x.TargetFramework == group.TargetFramework) is PackageDependencyGroup pkgGroup) {
                    await Parallel.ForEachAsync(pkgGroup.Packages, token, async (pkg, _) => {
                        await TryDownloadPackage(pkg.Id, pkg.VersionRange.OriginalString, source, token);
                    });
                }
            }
        }

        if (downloader.ContentReader is PackageArchiveReader packageArchiveReader) {
            packageArchiveReader.Dispose();
            File.Delete(nupkg);
        }

        return true;
    }

    private static bool TryGetMostCompatibleGroup(IEnumerable<FrameworkSpecificGroup> groups, out FrameworkSpecificGroup? group)
    {
        FrameworkReducer reducer = new();
        if (reducer.GetNearest(_targetFramework, groups.Select(i => i.TargetFramework)) is NuGetFramework framework) {
            group = groups.FirstOrDefault(i => i.TargetFramework.Equals(framework));
            return group != null;
        }

        group = null;
        return false;
    }
}
