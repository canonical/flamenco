using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Flamenco.Packaging;

namespace Flamenco.Console.Commands;

public class PackCommand : Command
{
    public PackCommand() : base(
        name: "pack", 
        description: "Packages the debian tarball(s) for specific packages and series from a provided source directory.")
    {
        var targetArguments = new Argument<string[]>(
            name: "targets", 
            description: "The packaging targets that should be produced. A packaging target is in the " +
                         "format 'PACKAGE:SERIES' (e.g. 'dotnet8:noble'). If no packaging target " +
                         "is specified all packageable targets in the source directory will be selected.")
        {
            Arity = ArgumentArity.ZeroOrMore,
        };

        AddArgument(targetArguments);
        AddOption(CommonOptions.SourceDirectoryOption);
        AddOption(CommonOptions.DestinationDirectoryOption);
        AddOption(CommonOptions.DebianTarballCompressionMethod);
        Handler = CommandHandler.Create(Run);
    }
    
    private static async Task<int> Run(
        string[] targets, 
        DirectoryInfo? sourceDirectory,
        DirectoryInfo? destinationDirectory,
        TarballCompressionMethod? debianTarballCompressionMethod,
        CancellationToken cancellationToken = default)
    {
        if (!EnvironmentVariables.TryGetSourceDirectoryInfoFromEnvironmentOrDefaultIfNull(ref sourceDirectory) ||
            !EnvironmentVariables.TryGetDestinationDirectoryInfoFromEnvironmentOrDefaultIfNull(ref destinationDirectory) ||
            !EnvironmentVariables.TryGetDebianTarballCompressionMethodFromEnvironmentOrDefaultIfNull(ref debianTarballCompressionMethod)) 
        {
            return -1;
        }
        
        Log.Debug("Source Directory: " + sourceDirectory.FullName);
        Log.Debug("Destination Directory: " + destinationDirectory.FullName);

#if SNAPCRAFT
        if (!await Program.IsPathAccessibleAsync(sourceDirectory.FullName, cancellationToken) || 
            !await Program.IsPathAccessibleAsync(destinationDirectory.FullName, cancellationToken))
        {
            Log.Fatal("Aborting the packaging process, because some paths are not accessible.");
            return -1;
        }
#endif
        var sourceDirectoryInfoResult = SourceDirectoryInfo.FromDirectory(sourceDirectory);
        Log.Annotations(sourceDirectoryInfoResult);
        if (sourceDirectoryInfoResult.IsFailure)
        {
            Log.Fatal("Aborting the packaging process, because the source directory contains errors.");
            return -1;
        }

        var sourceDirectoryInfo = sourceDirectoryInfoResult.Value;
        
        Log.Debug($"Discovered targets in source directory: {sourceDirectoryInfo.BuildableTargets}");
        
        var buildTargets = ParseBuildTargets(targets);
        if (buildTargets is null)
        {
            Log.Fatal("Aborting the packaging process, because the requested packaging targets contains errors.");
            return 1;
        }
        Log.Debug($"Requested packaging targets: {buildTargets}");

        if (buildTargets.Count == 0)
        {
            Log.Info("Packaging all packageable targets.");
            buildTargets = sourceDirectoryInfo.BuildableTargets;
        }
        else if (!BuildTargetsAreSubSetOfDefinedTargets())
        {
            Log.Fatal("Aborting the packaging process, because some packaging targets are not defined in the source directory.");
            return 1;
        }

        int failedBuilds = 0;

        if (!destinationDirectory.Exists)
        {
            destinationDirectory.Create();
        }

        var packagingTasks = new List<Task>();
        foreach (var buildTarget in buildTargets)
        {
            var debianTarballBuilder = new DebianSourcePackageBuilder()
            {
                SourceDirectory = sourceDirectoryInfo,
                DestinationDirectory = destinationDirectory,
                TarballCompressionMethod = debianTarballCompressionMethod.Value,
                BuildTarget = buildTarget,
                TarArchivingServiceProvider = new TarSystemCommand()
            };

            packagingTasks.Add(debianTarballBuilder
                .BuildDebianSourcePackageAsync(cancellationToken)
                .AsTask().ContinueWith((task) =>
                {
                    var result = task.Result;
                    
                    Log.Annotations(result);
                    if (result.IsSuccess)
                    {
                        Log.Info($"Packaging target {debianTarballBuilder.BuildTarget} succeeded.");
                    }
                    else
                    {
                        Interlocked.Increment(ref failedBuilds);
                        Log.Error($"Packaging target {debianTarballBuilder.BuildTarget} failed.");
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion));
        }

        await Task.WhenAll(packagingTasks);

        Log.Info($"Total Packages: {buildTargets.Count}; Succeeded: {buildTargets.Count - failedBuilds}; Failed: {failedBuilds}");
        
        if (failedBuilds > 0)
        {
            Log.Fatal("Packaging one or more packaging targets failed.");
            return 1;
        }
        
        return 0;

        bool BuildTargetsAreSubSetOfDefinedTargets()
        {
            bool allBuildTargetsAreDefined = true;
            
            foreach (var target in buildTargets)
            {
                if (!sourceDirectoryInfo.BuildableTargets.Contains(target))
                {
                    Log.Error($"Packaging target '{target}' is not defined in the source directory.");
                    allBuildTargetsAreDefined = false;
                }
            }

            return allBuildTargetsAreDefined;
        }
    }

    private static BuildTargetCollection? ParseBuildTargets(string[] targets)
    {
        var targetCollection = new BuildTargetCollection();

        bool errorDetected = false;
        
        foreach (var target in targets)
        {
            var targetComponents = target.Split(':');

            if (targetComponents.Length != 2)
            {
                Log.Error($"The packaging target '{target}' does not follow the format 'PACKAGE:SERIES'!");
                errorDetected = true;
                continue;
            }
            
            targetCollection.Add(new BuildTarget(PackageName: targetComponents[0], SeriesName: targetComponents[1]));
        }
        
        // we want to fail only after checking the format of all changelog files 
        if (errorDetected) return null;
        
        return targetCollection;
    }
}