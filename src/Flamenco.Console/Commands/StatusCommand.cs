using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Flamenco.Distro.ReleaseInfo;
using Flamenco.Distro.Services.Abstractions;
using Flamenco.Distro.Services.Launchpad.ReleaseStateProviders;
using Flamenco.Distro.Services.Madison;
using Flamenco.Packaging;
using Flamenco.Packaging.Dpkg;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Flamenco.Console.Commands;

public class StatusCommand: Command
{
    public StatusCommand() : base(name: "status", description: "")
    {
        AddOption(CommonOptions.SourceDirectoryOption);
        Handler = CommandHandler.Create(Run);
    }
    
    private static async Task<int> Run(
        DirectoryInfo? sourceDirectory,
        CancellationToken cancellationToken = default)
    {
        if (!EnvironmentVariables.TryGetSourceDirectoryInfoFromEnvironmentOrDefaultIfNull(ref sourceDirectory))
        {
            return -1;
        }

#if SNAPCRAFT
        if (!await Program.IsPathAccessibleAsync(sourceDirectory.FullName, cancellationToken))
        {
            Log.Fatal("Aborting the status process, because some paths are not accessible.");
            return -1;
        }
#endif

        Result<SourceDirectoryInfo> sourceDirectoryInfoResult = Result.Success;
        Result<IImmutableList<DpkgPackageReleaseState>> queryReleaseStatesResult = Result.Success;
        var changelogEntries = new Dictionary<BuildTarget, Result<ChangelogEntry>>();

        await AnsiConsole.Status().StartAsync("Analyzing source directory...", async statusContext =>
        {
            sourceDirectoryInfoResult = SourceDirectoryInfo.FromDirectory(sourceDirectory);

            if (sourceDirectoryInfoResult.IsSuccess)
            {
                foreach (var buildableTarget in sourceDirectoryInfoResult.Value.BuildableTargets)
                {
                    var readFirstChangelogEntryResult = await sourceDirectoryInfoResult.Value.ReadFirstChangelogEntryAsync(buildableTarget, CancellationToken.None);
                    changelogEntries.Add(buildableTarget, readFirstChangelogEntryResult);
                }
            }
        });

        if (sourceDirectoryInfoResult.IsFailure)
        {
            Log.Fatal("Aborting the packaging process, because the source directory contains errors.");
            return -1;
        }

        var sourceDirectoryInfo = sourceDirectoryInfoResult.Value;
        IReadOnlyList<DpkgPackageReleaseState> releaseStates = ImmutableList<DpkgPackageReleaseState>.Empty;

        await AnsiConsole.Status().StartAsync("Querying release states...", async statusContext =>
        {
            var queryOptions = new DpkgReleaseStateQueryOptions
            {
                PackageNames = sourceDirectoryInfo.BuildableTargets
                    .PackageNames
                    .Select(packageName => DpkgName.Parse(packageName))
                    .ToImmutableArray(),
                Architectures = ImmutableList.Create(DpkgArchitecture.Source),
                IncludeBinaryPackagesOfSourcePackages = false,
            };
            
            IHttpClientFactory httpClientFactory = new HttpClientFactory();

            var releaseStateProviderCollection = new DpkgReleaseStateProviderCollection
            {
                DpkgReleaseStateProviders = [
                    new UbuntuMadisonDpkgReleaseStateProvider(httpClientFactory),
                    new DebianMadisonDpkgReleaseStateProvider(httpClientFactory),
                    new LaunchpadPpaDpkgReleaseStateProvider(httpClientFactory)
                    {
                        PpaOwner = "dotnet",
                        PpaName = "backports",
                    },
                    new LaunchpadPpaDpkgReleaseStateProvider(httpClientFactory)
                    {
                        PpaOwner = "dotnet",
                        PpaName = "previews",
                    },
                    new UbuntuQueueReleaseStateProvider(httpClientFactory),
                ]
            };

            queryReleaseStatesResult = await releaseStateProviderCollection
                .QueryAsync(queryOptions, cancellationToken);

            if (queryReleaseStatesResult.HasValue)
            {
                releaseStates = queryReleaseStatesResult.Value;
            }
        });
        
        AnsiConsole.Write(
            new Rows(
                new Padder(
                    new Rows(
                        new Markup("[b]Source Directory[/]"),
                        new TextPath(sourceDirectoryInfo.DirectoryInfo.FullName)
                    )
                ).PadTop(0).PadLeft(1).PadBottom(1), 
                new Padder(
                    new Rows(
                        new Markup("[b]Build Targets[/]"),
                        RenderBuildTargets()
                    )
                ).PadTop(0).PadLeft(1).PadBottom(1), 
                new Padder(
                    new Rows(
                        new Markup("[b]Release State[/]"),
                        RenderReleaseState()
                    )
                ).PadTop(0).PadLeft(1).PadBottom(1)
            )
        );

        Log.Annotations(sourceDirectoryInfoResult);
        Log.Annotations(queryReleaseStatesResult);

        IRenderable RenderBuildTargets()
        {
            var table = new Table()
                .AddColumn("Build Target Id")
                .AddColumn("Source Package")
                .AddColumn("Series")
                .AddColumn("Version")
                .AddColumn("Note");

            bool hadPrevious = false;

            foreach (var packageTargetGroup in sourceDirectoryInfo.BuildableTargets.GroupBy(bt => bt.PackageName))
            {
                if (hadPrevious)
                {
                    table.AddEmptyRow();
                    hadPrevious = false;
                }

                foreach (var buildableTarget in packageTargetGroup.OrderByDescending(bt => bt.SeriesName))
                {
                    string versionMarkup = "";
                    string noteMarkup = "";
                    var changelogEntryResult = changelogEntries[buildableTarget];

                    if (changelogEntryResult.IsSuccess)
                    {
                        versionMarkup = Markup.Escape(changelogEntryResult.Value.Version.ToString());
                    }
                    else
                    {
                        noteMarkup = "Error";
                    }

                    table.AddRow(
                        Markup.Escape(buildableTarget.ToString()),
                        buildableTarget.PackageName,
                        buildableTarget.SeriesName,
                        versionMarkup,
                        noteMarkup);
                }

                hadPrevious = true;
            }

            return table;
        }
    
        IRenderable RenderReleaseState()
        {
            var releaseStateTree = new Tree("");
            
            foreach (var packageReleaseStates in releaseStates.GroupBy(s => s.Package.Identifier))
            {
                var table = new Table()
                    .AddColumn("Archive")
                    .AddColumn("Series")
                    .AddColumn("Pocket")
                    .AddColumn("Component")
                    .AddColumn("Version")
                    .AddColumn("Note");

                bool hadPrevious = false;

                foreach (var packageSeriesReleaseStates
                         in packageReleaseStates
                         .GroupBy(releaseState => releaseState.ArchiveSection.Suite.Series.Identifier)
                         .OrderByDescending(group => group.Key))
                {
                    if (hadPrevious)
                    {
                        table.AddEmptyRow();
                        hadPrevious = false;
                    }

                    var highestVersionInSeries = packageSeriesReleaseStates
                        .Where(releaseState => !releaseState.ArchiveSection.Suite.Pocket.IsProposed())
                        .Max(releaseState => releaseState.Version);

                    foreach (var releaseState 
                             in packageSeriesReleaseStates
                             .OrderByDescending(releaseState => releaseState.Version)
                             .ThenByDescending(releaseState => releaseState.ArchiveSection.Suite.Pocket.Identifier)
                             .ThenByDescending(releaseState => releaseState.ArchiveSection.Component.Identifier))
                    {
                        var pocket = releaseState.ArchiveSection.Suite.Pocket;
                        string pocketMarkup = Markup.Escape(pocket.Name);

                        if (releaseState.IsPendingOrProposed)
                        {
                            pocketMarkup = $"[dim]{pocketMarkup}[/]";
                        }

                        var component = releaseState.ArchiveSection.Component;
                        var componentMarkup = Markup.Escape(component.Identifier);

                        if ((component != UbuntuComponents.Main &&
                             component != UbuntuComponents.Restricted &&
                             component != DebianComponents.Main)
                            || releaseState.IsPendingOrProposed)
                        {
                            componentMarkup = $"[dim]{componentMarkup}[/]";
                        }

                        var versionMarkup = Markup.Escape(releaseState.Version.ToString());

                        if (releaseState.Version != highestVersionInSeries || releaseState.IsPendingOrProposed)
                        {
                            versionMarkup = $"[dim]{versionMarkup}[/]";
                        }

                        var archiveMarkup = Markup.Escape(releaseState.ArchiveSection.Name);
                        var seriesMarkup = Markup.Escape(releaseState.ArchiveSection.Suite.Series.Identifier);

                        if (releaseState.IsPendingOrProposed)
                        {
                            archiveMarkup = $"[dim]{archiveMarkup}[/]";
                            seriesMarkup = $"[dim]{seriesMarkup}[/]";
                        }

                        var noteMarkup = "";

                        var buildTarget = changelogEntries.Keys.FirstOrDefault(bt =>
                            bt.PackageName == releaseState.Package.Identifier &&
                            bt.SeriesName == releaseState.ArchiveSection.Suite.Series.Identifier);

                        if (buildTarget is not null && changelogEntries[buildTarget].IsSuccess)
                        {
                            var changelogEntry = changelogEntries[buildTarget].Value;

                            if (changelogEntry.Version < releaseState.Version)
                            {
                                noteMarkup = "[red]" + Markup.Escape("Local version (") + "[bold]" + Markup.Escape(changelogEntry.Version.ToString()) + "[/]" + Markup.Escape(") is behind!") + "[/]";
                            }
                            else if  (changelogEntry.Version > releaseState.Version)
                            {
                                noteMarkup = "[yellow]" + Markup.Escape("Local version (") + "[bold]" + Markup.Escape(changelogEntry.Version.ToString()) + "[/]" + Markup.Escape(") is ahead!") + "[/]";
                            }
                            else
                            {
                                noteMarkup = "[dim]" + Markup.Escape("Local version is up to date.") + "[/]";
                            }
                        }

                        table.AddRow(
                            archiveMarkup,
                            seriesMarkup,
                            pocketMarkup,
                            componentMarkup,
                            versionMarkup,
                            noteMarkup);
                        hadPrevious = true;
                    }
                }
                
                releaseStateTree.AddNode(
                    new Padder(
                        new Rows(new Markup($"[dim]src:[/]{Markup.Escape(packageReleaseStates.Key)}"), table)
                    ).PadTop(0).PadBottom(0).PadLeft(0).PadRight(0)
                );
            }
            
            return releaseStateTree;
        }
        
        return 0;
    }
    
    private class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
